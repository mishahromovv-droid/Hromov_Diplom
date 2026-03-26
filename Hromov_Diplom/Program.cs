using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Hromov_Diplom.Config;
using Hromov_Diplom.Models;
using Hromov_Diplom.Reports;
using Hromov_Diplom.ComService;
using Hromov_Diplom.GeneratorProcessing;

namespace Hromov_Diplom
{
    /// <summary>
    /// Главная программа для пакетного расчёта МДП
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // ✅ Удаляем старый файл перед запуском
            if (File.Exists(AppSettings.CsvPath))
            {
                try
                {
                    File.Delete(AppSettings.CsvPath);
                    Console.WriteLine("[CLEAN] Старый файл результатов удалён.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Не удалось удалить файл: {ex.Message}");
                }
            }

            PrintHeader();

            using var rastrEngine = new RastrEngine();
            var reportManager = new ReportManager(AppSettings.CsvPath);

            foreach (var station in CalculationSettings.Stations)
            {
                Console.WriteLine($"\n{'=',60}");
                Console.WriteLine($"⚙ СТАНЦИЯ: {station.Name}");
                Console.WriteLine($"   Генераторы: {string.Join(", ", station.GeneratorNums)}");
                Console.WriteLine($"   Количество: {station.GeneratorNums.Length} шт.");
                Console.WriteLine($"{'=',60}");

                var generatorProcessor = new GeneratorProcessor(station.GeneratorNums, station.Name);

                try
                {
                    Console.Write("[INIT] Инициализация RastrWin3... ");
                    rastrEngine.Initialize();
                    Console.WriteLine("OK");

                    foreach (double factor in CalculationSettings.ScaleFactors)
                    {
                        ProcessIteration(rastrEngine, generatorProcessor, factor, reportManager, station.Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n✗ ОШИБКА: {ex.Message}");
                }
            }

            reportManager.WriteToFile(append: false);
            reportManager.PrintSummaryTable();

            Console.WriteLine($"\n[INFO] Результаты сохранены: {AppSettings.CsvPath}");
        }

        static void PrintHeader()
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("    ПАКЕТНЫЙ РАСЧЁТ МДП (RastrWin3)");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Станции: {string.Join(" → ", CalculationSettings.Stations.Select(s => s.Name))}");
            Console.WriteLine($"Коэффициенты: {string.Join("  ", CalculationSettings.ScaleFactors.Select(f => f.ToString("0.0")))}");
            Console.WriteLine($"Генераторы: {string.Join(", ", GetAllGeneratorNums())}");
            Console.WriteLine($"ut_vir: Act=0 кроме Num={VirSettings.KeepNum}");
            Console.WriteLine("==========================================\n");
        }

        static string GetAllGeneratorNums()
        {
            return string.Join(", ", CalculationSettings.Stations.SelectMany(s => s.GeneratorNums));
        }

        static void ProcessIteration(
            RastrEngine engine,
            GeneratorProcessor processor,
            double factor,
            ReportManager reporter,
            string stationName)
        {
            Console.WriteLine($"\n{'#',50}");
            Console.WriteLine($"# КОЭФФИЦИЕНТ: {factor,32} | {stationName,-15}");
            Console.WriteLine($"{'#',50}");

            IterationReport currentReport = new IterationReport
            {
                Factor = factor,
                StationName = stationName
            };

            try
            {
                Console.Write("  [LOAD] Загрузка режима... ");
                engine.LoadModeFiles(AppSettings.RegimeDirectory);
                Console.WriteLine("OK");

                Console.Write("  [VIR] Модификация ut_vir... ");
                engine.ZeroVirTable(VirSettings.TableName, VirSettings.ColumnName, VirSettings.KeepNum);

                Console.Write("  [MOD] Модификация генераторов... ");
                var modifications = processor.ApplyLimits(engine.RawObject, factor);

                PrintModifications(modifications, stationName);

                Console.WriteLine($"OK ({modifications.Count} шт.)");

                Console.Write("  [VAL] Валидация диапазонов... ");
                if (!processor.ValidateRanges(engine.RawObject))
                {
                    Console.WriteLine("❌ Неверные границы Qmin/Qmax");
                    currentReport.Status = "INVALID_DATA";
                    reporter.AddResult(currentReport);
                    return;
                }
                Console.WriteLine("✅ OK");

                Console.Write("  [CALC] Расчёт МДП... ");
                engine.CalculateEmergencies();

                Console.Write("  [GET] Получение результатов... ");
                if (!engine.HasResults())
                {
                    Console.WriteLine("❌ Таблица ut_os_result пуста");
                    currentReport.Status = "NO_RESULTS";
                    reporter.AddResult(currentReport);
                    return;
                }
                Console.WriteLine("✅ OK");

                var result = engine.GetResults();
                PrintResultConsole(result);

                currentReport.Result = result;
                currentReport.Status = result.IsSuccess ? "OK" : "FAILED_CALCULATION";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ ОШИБКА: {ex.Message}");
                currentReport.Status = $"ERROR: {ex.Message.Split(':')[0]}";
                currentReport.ErrorDetails = ex.Message;
            }

            reporter.AddResult(currentReport);
        }

        static void PrintResultConsole(CalculationResult result)
        {
            Console.WriteLine("  +--------------------------------------------------------------+");
            Console.WriteLine($"  |  PRED (P_пр):              {FormatVal(result.Pred),15}               |");
            Console.WriteLine($"  |  PADP_REAL (АДП):          {FormatVal(result.PadpReal),15}               |");
            Console.WriteLine($"  |  ITOG_MDP (P_МДП):         {FormatVal(result.ItogMdp),15}               |");
            Console.WriteLine($"  |  ITOG_MDP_PA (P_МДП_с_ПА): {FormatVal(result.ItogMdpPa),15}               |");
            Console.WriteLine("  +--------------------------------------------------------------+");
        }

        static void PrintModifications(List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)> mods, string stationName)
        {
            if (mods.Count == 0) return;

            Console.WriteLine();
            Console.WriteLine($"┌─────────────────────────────────────────────────────────────────────────┐");
            Console.WriteLine($"│            ИЗМЕНЕНИЯ ГЕНЕРАТОРОВ ({stationName}, NumPQ=0):            │");
            Console.WriteLine($"├────────┬─────────────────────────────────┬──────────┬───────────────────┤");
            Console.WriteLine($"│   Num  │              Name               │   Qmin   │        Qmax       │");
            Console.WriteLine($"├────────┼─────────────────────────────────┼──────────┼───────────────────┤");

            foreach (var m in mods)
            {
                string namePad = m.name.PadRight(29);
                string qminFormatted = $"{m.qmin,8:F2}";
                string qmaxFormatted = $"{m.qmaxOld,7:F2}";
                string qmaxNewFormatted = $"{m.qmaxNew,7:F2}";

                Console.WriteLine($"│  {m.num,4}  │  {namePad}  │ {qminFormatted} │ {qmaxFormatted} → {qmaxNewFormatted} │");
            }

            Console.WriteLine($"└─────────────────────────────────────────────────────────────────────────┘");
        }

        static string FormatVal(double? val)
            => val.HasValue ? val.Value.ToString("F3", CultureInfo.InvariantCulture) : "-";
    }
}