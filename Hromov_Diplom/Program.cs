using System;
using System.Globalization;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main()
        {
            // ✅ УСТАНОВЛЕНА РУССКАЯ ЛОКАЛЬ ДЛЯ ФОРМАТИРОВАНИЯ ЧИСЕЛ С ЗАПЯТОЙ
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("==========================================");
            Console.WriteLine("    ПАКЕТНЫЙ РАСЧЁТ МДП (RastrWin3)");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Станции: {string.Join(" → ", Array.ConvertAll(CalculationSettings.Stations, s => s.Name))}");
            Console.WriteLine($"Коэффициенты: {string.Join("  ", CalculationSettings.ScaleFactors)}");
            Console.WriteLine($"ut_vir: Act=0 кроме Num={VirSettings.KeepNum}");
            Console.WriteLine($"Сечение для Pраб: Num={AppSettings.PrabSectionNum}");
            Console.WriteLine($"Требуемый запас устойчивости ζтр: {AppSettings.RequiredReservePct:F1}%");
            Console.WriteLine("==========================================\n");

            var reportManager = new ReportManager(AppSettings.CsvPath);

            foreach (var station in CalculationSettings.Stations)
            {
                Console.WriteLine(new string('=', 70));
                Console.WriteLine($"⚙ СТАНЦИЯ: {station.Name}");
                Console.WriteLine($"   Генераторы: {string.Join(", ", station.GeneratorNums)}");
                Console.WriteLine($"   Количество: {station.GeneratorNums.Length} шт.");
                Console.WriteLine(new string('=', 70));

                foreach (var factor in CalculationSettings.ScaleFactors)
                {
                    Console.WriteLine($"\n# КОЭФФИЦИЕНТ: {factor:F1} | {station.Name}");

                    var rep = RunOneIteration(station.Name, station.GeneratorNums, factor);
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

            var kbResults = InfluenceCoefficientCalculator.CalculateKBForAllStations(reportManager.GetReports());

            if (kbResults.Count > 0)
            {
                Console.WriteLine("✅ Данные k_B получены. Запись в файл...");

                try
                {
                    InfluenceCoefficientCalculator.WriteKBToCsv(finalCsvPath, kbResults);
                    Console.WriteLine($"✅ Коэффициенты k_B успешно записаны в файл: {finalCsvPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка записи k_B в файл: {ex.Message}");
                    Console.WriteLine("Попробуйте запустить программу снова после закрытия Excel.");
                }
            }
            else
            {
                Console.WriteLine("⚠️ Нет данных для расчёта k_B (мало результатов).");
            }

            Console.WriteLine("\nГотово. Нажмите Enter для выхода...");
            Console.ReadKey();
        }

        private static IterationReport RunOneIteration(string stationName, int[] genNums, double factor)
        {
            var report = new IterationReport
            {
                StationName = stationName,
                Factor = factor,
                Status = "UNSET"
            };

            try
            {
                using var engine = new RastrEngine();
                engine.Initialize();
                Console.WriteLine("  [INIT] Инициализация RastrWin3... OK");

                Console.WriteLine("  [LOAD] Загрузка режима... ");
                engine.LoadModeFiles(AppSettings.RegimeDirectory);
                Console.WriteLine("  [LOAD] OK");

                Console.WriteLine("  [VIR] Модификация ut_vir...");
                engine.ZeroVirTable(VirSettings.TableName, VirSettings.ColumnName, VirSettings.KeepNum);

                var processor = new GeneratorProcessor(genNums);

                Console.WriteLine("  [MOD] Модификация генераторов...");
                processor.ApplyLimits(engine.RawObject, factor);
                Console.WriteLine("  [MOD] OK");

                Console.WriteLine("  [VAL] Валидация диапазонов...");
                if (!processor.ValidateRanges(engine.RawObject))
                {
                    report.Status = "FAIL";
                    report.ErrorDetails = "Qmax < Qmin у одного из генераторов";
                    Console.WriteLine("  [VAL] ❌ FAIL: Qmax < Qmin");
                    return report;
                }
                Console.WriteLine("  [VAL] ✅ OK");

                Console.WriteLine("  [BASE] Расчёт базового режима...");
                engine.SolveBaseMode();
                Console.WriteLine("  [BASE] OK");

                double prab = engine.GetSectionFlow(AppSettings.PrabSectionNum);
                report.Result.PrabSech17 = prab;
                Console.WriteLine($"  [SECH] Pраб в сечении {AppSettings.PrabSectionNum}: {(prab > 0 ? prab.ToString("F3") : "НЕ НАЙДЕНО")}");

                QTotals qt = processor.GetQTotals(engine.RawObject);
                report.Result.QminTotal = qt.QminTotal;
                report.Result.QmaxTotal = qt.QmaxTotal;
                report.Result.QrangeTotal = qt.QrangeTotal;

                Console.WriteLine("  [CALC] Расчёт МДП...");
                engine.CalculateEmergencies();

                if (!engine.HasResults())
                {
                    report.Status = "NO_RESULTS";
                    report.ErrorDetails = "ut_os_result пуста";
                    Console.WriteLine("  [GET] ❌ ut_os_result пуста");
                    return report;
                }

                var res = engine.ReadResults();

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