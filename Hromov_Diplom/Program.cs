using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("==========================================");
            Console.WriteLine("    ПАКЕТНЫЙ РАСЧЁТ МДП (RastrWin3)");
            Console.WriteLine("==========================================\n");

            var reports = new List<IterationReport>();
            var reportManager = new ReportManager(AppSettings.CsvPath);

            using var engine = new RastrEngine();

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

            foreach (var station in CalculationSettings.Stations)
            {
                Console.WriteLine(new string('=', 70));
                Console.WriteLine($"⚙ СТАНЦИЯ: {station.Name} ({station.Code})");
                Console.WriteLine($"   Генераторы: {string.Join(", ", station.GeneratorNums)}");
                Console.WriteLine(new string('=', 70));

                foreach (var factor in CalculationSettings.ScaleFactors)
                {
                    Console.WriteLine($"\n# КОЭФФИЦИЕНТ: {factor:F1} | {station.Name}");

                    var rep = RunOneIteration(station.Name, station.GeneratorNums, factor, engine);
                    reports.Add(rep);
                    reportManager.AddResult(rep);
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n>>> Сохранение основных результатов в CSV...\n");
            reportManager.PrintSummaryTable(AppSettings.RequiredReservePct);

            string finalCsvPath = reportManager.GetFinalCsvPath();
            reportManager.WriteToFile(AppSettings.RequiredReservePct);

            Console.WriteLine("\n\n>>> Расчёт коэффициента влияния k_B...\n");
            Console.WriteLine("(Убедитесь, что Excel закрыт перед запуском, иначе данные не запишутся!)");

            var individualKBResults = InfluenceCoefficientCalculator.CalculateKBForAllStations(reports);
            var groupKBResults = InfluenceCoefficientCalculator.CalculateGroupKBForAllStations(reports);

            if (individualKBResults.Count > 0)
            {
                try
                {
                    InfluenceCoefficientCalculator.WriteKBToCsv(finalCsvPath, individualKBResults);
                    Console.WriteLine($"✅ Индивидуальные коэффициенты записаны в: {finalCsvPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка записи индивидуальных k_B: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("⚠️ Нет данных для расчёта индивидуальных k_B.");
            }

            if (groupKBResults.Count > 0)
            {
                Console.WriteLine("\n" + new string('-', 90));
                Console.WriteLine("              ТАБЛИЦА ГРУППОВЫХ КОЭФФИЦИЕНТОВ ВЛИЯНИЯ (K_B)");
                Console.WriteLine(new string('-', 90));
                Console.WriteLine($"| {"Станция",-40} | {"M",3} | {"Интервалы",9} | {"K_B (Среднее)",15} |");
                Console.WriteLine(new string('-', 90));

                foreach (var item in groupKBResults.OrderBy(x => x.StationName))
                {
                    Console.WriteLine($"| {item.StationName,-40} | {item.M,3} | {item.IntervalsCount,9} | {item.GroupKB,15:F6} |");
                }
                Console.WriteLine(new string('-', 90) + "\n");

                InfluenceCoefficientCalculator.WriteGroupKBToCsv(finalCsvPath, groupKBResults);
            }
            else
            {
                Console.WriteLine("⚠️ Нет данных для расчёта групповых коэффициентов.");
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

                // ✅ ЧТЕНИЕ Q-ПАРАМЕТРОВ ИЗ ТАБЛИЦЫ GENERATOR
                QTotals qt = processor.GetQTotals(engineInstance.RawObject);
                report.Result.QminTotal = qt.QminTotal;
                report.Result.QmaxTotal = qt.QmaxTotal;

                Console.WriteLine($"  [Q] Qmin={qt.QminTotal:F3}, Qmax={qt.QmaxTotal:F3}, Qrange={qt.QrangeTotal:F3}");

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