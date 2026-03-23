using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("==========================================");
            Console.WriteLine("    ПАКЕТНЫЙ РАСЧЁТ МДП (RastrWin3)");
            Console.WriteLine("==========================================\n");

            var engine = new RastrEngine();

            try
            {
                engine.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL ERROR]: {ex.Message}\n");
                Console.WriteLine("Проверьте, что программа RASTRA запущена!");
                Console.ReadKey();
                return;
            }

            Console.WriteLine(">>> Инициализация завершена успешно!\n");

            string path = AppSettings.RegimeDirectory;
            try
            {
                engine.LoadModeFiles(path);
                Console.WriteLine(">>> Режим загружен успешно.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOAD ERROR]: {ex.Message}");
                Console.ReadKey();
                return;
            }

            var reportManager = new ReportManager(AppSettings.CsvPath);
            List<GenNodeConnection> allConnections = new List<GenNodeConnection>();

            foreach (var station in CalculationSettings.Stations)
            {
                Console.WriteLine(new string('=', 70));
                Console.WriteLine($"⚙ СТАНЦИЯ: {station.Name} ({station.Code})");
                Console.WriteLine($"   Генераторы: {string.Join(", ", station.GeneratorNums)}");
                Console.WriteLine(new string('=', 70));

                // Чтение связей
                var connections = VoltageAnalyzer.GetConnections(engine.RawObject, station.GeneratorNums, station.Name, station.Code);
                allConnections.AddRange(connections);

                if (connections.Count > 0)
                {
                    Console.WriteLine("\n🔗 СВЯЗИ ГЕНЕРАТОРОВ:");
                    foreach (var conn in connections)
                    {
                        string connectionText = $" | Г-{conn.GenNum,-3} → Узел {conn.NodeNum,-4} | {conn.GenName,-30} | {conn.StationName}, {conn.StationCode} |";
                        Console.WriteLine(connectionText);
                    }
                    Console.WriteLine();
                }

                foreach (var factor in CalculationSettings.ScaleFactors)
                {
                    Console.WriteLine($"\n# КОЭФФИЦИЕНТ: {factor:F1} | {station.Name}");

                    var rep = RunOneIteration(station.Name, station.GeneratorNums, factor, engine);
                    reportManager.AddResult(rep);
                }

                Console.WriteLine();
            }

            // Основные результаты
            Console.WriteLine("\n>>> Сохранение основных результатов в CSV...\n");
            reportManager.PrintSummaryTable(AppSettings.RequiredReservePct);

            string finalCsvPath = reportManager.GetFinalCsvPath();
            reportManager.WriteToFile(AppSettings.RequiredReservePct);

            // СОХРАНЕНИЕ СВЯЗЕЙ ПОСЛЕ ОСНОВНЫХ ДАННЫХ
            Console.WriteLine(">>> Сохранение связей в конец CSV файла...");
            VoltageAnalyzer.SaveConnectionsToCsv(allConnections, finalCsvPath);

            Console.WriteLine("\n\n>>> Расчёт коэффициента влияния k_B...\n");
            Console.WriteLine("(Убедитесь, что Excel закрыт перед запуском, иначе данные не запишутся!)");

            var kbResults = InfluenceCoefficientCalculator.CalculateKBForAllStations(reportManager.GetReports());

            if (kbResults.Count > 0)
            {
                try
                {
                    InfluenceCoefficientCalculator.WriteKBToCsv(finalCsvPath, kbResults);
                    Console.WriteLine($"✅ Коэффициенты k_B записаны в: {finalCsvPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка записи k_B: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("⚠️ Нет данных для расчёта k_B.");
            }

            Console.WriteLine("\nГотово. Нажмите Enter для выхода...");
            Console.ReadKey();

            engine.Dispose();
        }

        private static IterationReport RunOneIteration(string stationName, int[] genNums, double factor, RastrEngine engineInstance)
        {
            var report = new IterationReport
            {
                StationName = stationName,
                Factor = factor,
                Status = "UNSET"
            };

            try
            {
                Console.WriteLine("  [INIT] Инициализация RastrWin3... OK");
                Console.WriteLine("  [LOAD] Загрузка режима... OK");

                Console.WriteLine("  [VIR] Модификация ut_vir...");
                engineInstance.ZeroVirTable(VirSettings.TableName, VirSettings.ColumnName, VirSettings.KeepNum);

                var processor = new GeneratorProcessor(genNums);

                Console.WriteLine("  [MOD] Модификация генераторов...");
                processor.ApplyLimits(engineInstance.RawObject, factor);
                Console.WriteLine("  [MOD] OK");

                Console.WriteLine("  [VAL] Валидация диапазонов...");
                if (!processor.ValidateRanges(engineInstance.RawObject))
                {
                    report.Status = "FAIL";
                    report.ErrorDetails = "Qmax < Qmin";
                    Console.WriteLine("  [VAL] ❌ FAIL: Qmax < Qmin");
                    return report;
                }
                Console.WriteLine("  [VAL] ✅ OK");

                Console.WriteLine("  [BASE] Расчёт базового режима...");
                engineInstance.SolveBaseMode();
                Console.WriteLine("  [BASE] OK");

                double prab = engineInstance.GetSectionFlow(AppSettings.PrabSectionNum);
                report.Result.PrabSech17 = prab;
                Console.WriteLine($"  [SECH] Pраб в сечении {AppSettings.PrabSectionNum}: {(prab > 0 ? prab.ToString("F3") : "НЕ НАЙДЕНО")}");

                QTotals qt = processor.GetQTotals(engineInstance.RawObject);
                report.Result.QminTotal = qt.QminTotal;
                report.Result.QmaxTotal = qt.QmaxTotal;
                report.Result.QrangeTotal = qt.QrangeTotal;

                Console.WriteLine("  [CALC] Расчёт МДП...");
                engineInstance.CalculateEmergencies();

                if (!engineInstance.HasResults())
                {
                    report.Status = "NO_RESULTS";
                    report.ErrorDetails = "ut_os_result пуста";
                    Console.WriteLine("  [GET] ❌ ut_os_result пуста");
                    return report;
                }

                var res = engineInstance.ReadResults();

                report.Result.Pred = res.Pred;
                report.Result.PadpReal = res.PadpReal;
                report.Result.ItogMdp = res.ItogMdp;
                report.Result.ItogMdpPa = res.ItogMdpPa;
                 
                if (report.Result.ItogMdp > 0)
                {
                    report.Status = "OK";
                    Console.WriteLine("  [GET] ✅ OK");
                }
                else
                {
                    report.Status = "NO_RESULTS";
                    report.ErrorDetails = "ItogMdp не прочитан";
                    Console.WriteLine("  [GET] ⚠ ItogMdp не прочитан");
                }

                if (!double.IsNaN(prab) && !double.IsInfinity(prab) && prab > 0)
                {
                    report.Result.HasCore = true;
                }

                return report;
            }
            catch (Exception ex)
            {
                report.Status = "FAIL";
                report.ErrorDetails = ex.Message;
                Console.WriteLine("  ✗ ОШИБКА: " + ex.Message);
                return report;
            }
        }
    }
}