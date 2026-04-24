using Hromov_Diplom.ComService;
using Hromov_Diplom.Config;
using Hromov_Diplom.GeneratorProcessing;
using Hromov_Diplom.Models;
using Hromov_Diplom.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                Console.WriteLine("🚀 СТАРТ ПРОГРАММЫ (РЕЖИМ АВТОСОХРАНЕНИЯ)");
                Console.WriteLine($"📂 Базовая папка: {AppSettings.BaseRoot}");
                Console.WriteLine("--------------------------------------------------------");

                if (!Directory.Exists(AppSettings.OutputRoot))
                    Directory.CreateDirectory(AppSettings.OutputRoot);

                // 🔹 ШАГ 1: Коэффициенты влияния
                RunInfluenceCalculation(AppSettings.RegimeFolders[0]);

                Console.WriteLine("\n⏸️  Нажмите Enter для продолжения...");
                Console.ReadLine();

                // 🔹 ШАГИ 2-6: Выгрузка Q/Qmax
                foreach (var regime in AppSettings.RegimeFolders)
                {
                    RunQExport(regime);
                    Console.WriteLine("\n⏸️  Нажмите Enter для следующего режима...");
                    Console.ReadLine();
                }

                OrganizeResults();
                Console.WriteLine("\n✅ ВСЕ ЗАДАЧИ ВЫПОЛНЕНЫ!");
                Console.WriteLine($"📁 Результаты в: {AppSettings.OutputRoot}");
                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}\n{ex.StackTrace}");
                Console.ReadKey();
            }
        }

        static void RunInfluenceCalculation(string regimeName)
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("   ШАГ 1: РАСЧЁТ КОЭФФИЦИЕНТОВ ВЛИЯНИЯ");
            Console.WriteLine($"   Режим: {regimeName}");
            Console.WriteLine("==========================================");

            var regimePath = Path.Combine(AppSettings.BaseRoot, regimeName);
            if (!Directory.Exists(regimePath) && !File.Exists(regimePath))
            {
                Console.WriteLine($"❌ Не найден режим: {regimePath}");
                return;
            }

            using var engine = new RastrEngine();
            var reportManager = new ReportManager(Path.Combine(AppSettings.BaseRoot, $"results_{regimeName}.csv"));

            try
            {
                Console.WriteLine("[1] Инициализация RastrWin3...");
                engine.Initialize();
                Console.WriteLine("    ✅ OK");

                Console.WriteLine("[2] Загрузка режима...");
                engine.LoadModeFiles(regimePath);
                Console.WriteLine("    ✅ OK");

                int totalStations = CalculationSettings.Stations.Length;
                int currentStationIndex = 0;

                foreach (var station in CalculationSettings.Stations)
                {
                    currentStationIndex++;
                    Console.WriteLine($"\n⚙ [{currentStationIndex}/{totalStations}] {station.Name}");
                    var processor = new GeneratorProcessor(station.GeneratorNums, station.Name);

                    foreach (double factor in CalculationSettings.ScaleFactors)
                    {
                        string status = ProcessIteration(engine, processor, factor, reportManager, station.Name);
                        Console.WriteLine($"   🔹 F={factor:0.00} -> {status}");

                        // ✅ АВТОСОХРАНЕНИЕ ПОСЛЕ КАЖДОЙ ИТЕРАЦИИ
                        reportManager.SaveCurrentResults();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ошибка в цикле расчёта: {ex.Message}");
            }

            Console.WriteLine($"\n[DEBUG] Отчётов собрано: {reportManager.GetReportCount()}");

            var results = reportManager.CalculateInfluenceCoefficients();

            if (results.Any())
            {
                var folder = Path.Combine(AppSettings.OutputRoot, "Коэффициенты влияния");
                Directory.CreateDirectory(folder);
                var outPath = Path.Combine(folder, $"{regimeName}_influence.csv");
                reportManager.ExportInfluenceCoefficientsToCsv(results, outPath);
                Console.WriteLine($"✅ Коэффициенты сохранены: {outPath}");
            }
            else
            {
                Console.WriteLine("⚠ Коэффициенты не рассчитаны. Проверьте статусы выше (должно быть много OK).");
            }
        }

        static string ProcessIteration(RastrEngine engine, GeneratorProcessor processor, double factor,
            ReportManager reporter, string stationName)
        {
            var report = new IterationReport { Factor = factor, StationName = stationName };
            try
            {
                engine.ZeroVirTable(VirSettings.TableName, VirSettings.ColumnName, VirSettings.KeepNum);

                var modsList = (System.Collections.Generic.List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)>)
                               processor.ApplyLimits(engine.RawObject, factor);

                report.GeneratorModifications = new List<GeneratorModification>();

                foreach (var item in modsList)
                {
                    report.GeneratorModifications.Add(new GeneratorModification
                    {
                        Num = item.num,
                        Name = item.name,
                        Qmin = item.qmin,
                        QmaxOld = item.qmaxOld,
                        QmaxNew = item.qmaxNew
                    });
                }

                if (!processor.ValidateRanges(engine.RawObject))
                {
                    report.Status = "INVALID_DATA";
                    reporter.AddResult(report);
                    return report.Status;
                }

                var originalOut = Console.Out;
                Console.SetOut(TextWriter.Null);
                try { engine.CalculateEmergencies(); }
                finally { Console.SetOut(originalOut); }

                if (!engine.HasResults())
                {
                    report.Status = "NO_RESULTS";
                    reporter.AddResult(report);
                    return report.Status;
                }

                var res = engine.GetResults();
                report.Result = res;
                report.Status = res.IsSuccess ? "OK" : "FAILED_PRED";
            }
            catch (Exception ex)
            {
                report.Status = $"ERROR: {ex.Message}";
            }
            reporter.AddResult(report);
            return report.Status;
        }

        static void RunQExport(string regimeName)
        {
            Console.WriteLine($"\n==========================================");
            Console.WriteLine($"   ВЫГРУЗКА ДАННЫХ (Q / Qmax)");
            Console.WriteLine($"   Режим: {regimeName}");
            Console.WriteLine("==========================================");

            var regimePath = Path.Combine(AppSettings.BaseRoot, regimeName);
            if (!Directory.Exists(regimePath) && !File.Exists(regimePath))
            {
                Console.WriteLine($"❌ Не найден режим: {regimePath}");
                return;
            }

            using var engine = new RastrEngine();
            try
            {
                Console.WriteLine("[1] Инициализация..."); engine.Initialize(); Console.WriteLine("    ✅");
                Console.WriteLine("[2] Загрузка..."); engine.LoadModeFiles(regimePath); Console.WriteLine("    ✅");
                Console.WriteLine("[3] Экспорт генераторов...");
                var gens = engine.ExportGeneratorDataToList();
                Console.WriteLine($"    📊 Найдено: {gens.Count}");

                string folderName = regimeName switch
                {
                    "mdp_debag_1" or "mdp_debug_1" => "Коэффициенты влияния",
                    "mdp_debag_1_Утяжеленный" or "mdp_debug_1_Утяжеленный" => "Утяжеленный",
                    "mdp_debag_1_Утяжеленный_до_МДП" or "mdp_debug_1_Утяжеленный_до_МДП" => "Утяжеленный_до_МДП",
                    "mdp_debag_1_Утяжеленный_с_Vzd" or "mdp_debug_1_Утяжеленный_с_Vzd" => "Утяжеленный_с_Vzd(Станции)",
                    "mdp_debag_1_Утяжеленный_с_Vzd_до_МДП" or "mdp_debug_1_Утяжеленный_с_Vzd_до_МДП" => "Утяжеленный_с_Vzd(Станции)_до_МДП",
                    _ => regimeName
                };

                var target = Path.Combine(AppSettings.OutputRoot, folderName);
                Directory.CreateDirectory(target);

                ReportManager.ExportCleanDataWithSums(gens, Path.Combine(target, "Raschyot.csv"));
                ReportManager.ExportStationQSummary(gens, Path.Combine(target, "Ytog.csv"));
                Console.WriteLine($"✅ Сохранено в: {target}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
            }
        }

        static void OrganizeResults()
        {
            Console.WriteLine("\n==========================================");
            Console.WriteLine("   ФИНАЛЬНАЯ ПРОВЕРКА");
            Console.WriteLine("==========================================");
            var folders = new[] { "Коэффициенты влияния", "Утяжеленный", "Утяжеленный_до_МДП", "Утяжеленный_с_Vzd(Станции)", "Утяжеленный_с_Vzd(Станции)_до_МДП" };
            foreach (var f in folders)
            {
                var p = Path.Combine(AppSettings.OutputRoot, f);
                Console.WriteLine(Directory.Exists(p) ? $"✅ {f}/ — готово" : $"⚠ {f}/ — не создана");
            }
        }
    }
}