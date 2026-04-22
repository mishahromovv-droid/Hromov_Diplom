using Hromov_Diplom.ComService;
using Hromov_Diplom.Config;
using Hromov_Diplom.GeneratorProcessing;
using Hromov_Diplom.Models;
using Hromov_Diplom.Reports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (!string.IsNullOrEmpty(AppSettings.CsvPath) && File.Exists(AppSettings.CsvPath))
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
            var reportManager = !string.IsNullOrEmpty(AppSettings.CsvPath)
                ? new ReportManager(AppSettings.CsvPath)
                : throw new InvalidOperationException("Путь к файлу не указан!");

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
                    Console.WriteLine($"\n✗ ОШИБКА СТАНЦИИ {station.Name}: {ex.Message}");
                }
            }

            if (reportManager.WriteMainResults())
            {
                Console.WriteLine($"\n✅ Основные результаты сохранены: {AppSettings.CsvPath}");
            }
            else
            {
                Console.WriteLine("\n❌ Ошибка сохранения основных результатов.");
            }

            CalculateAndExportInfluenceCoefficients(reportManager);
        }

        static void CalculateAndExportInfluenceCoefficients(ReportManager reportManager)
        {
            Console.WriteLine("\n==========================================");
            Console.WriteLine("🔧 РАСЧЁТ КОЭФФИЦИЕНТОВ ВЛИЯНИЯ...");
            Console.WriteLine("==========================================\n");

            try
            {
                var results = reportManager.CalculateInfluenceCoefficients();

                if (results.Any())
                {
                    string csvPathWithK = AppSettings.CsvPath.Replace(".csv", "_influence.csv");
                    reportManager.ExportInfluenceCoefficientsToCsv(results, csvPathWithK);

                    Console.WriteLine($"✅ Коэффициенты влияния успешно рассчитаны и сохранены!");
                    Console.WriteLine($"📂 Файл результатов: {csvPathWithK}\n");
                }
                else
                {
                    Console.WriteLine("⚠ Нет данных для расчёта коэффициентов влияния.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при расчёте коэффициентов влияния: {ex.Message}\n");
            }
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

        static void ProcessIteration(RastrEngine engine, GeneratorProcessor processor, double factor, ReportManager reporter, string stationName)
        {
            IterationReport currentReport = new IterationReport { Factor = factor, StationName = stationName };

            try
            {
                if (!string.IsNullOrEmpty(AppSettings.RegimeDirectory))
                {
                    engine.LoadModeFiles(AppSettings.RegimeDirectory);
                    engine.ZeroVirTable(VirSettings.TableName, VirSettings.ColumnName, VirSettings.KeepNum);

                    var modifications = processor.ApplyLimits(engine.RawObject, factor);
                    currentReport.GeneratorModifications = ConvertToModifications(modifications);

                    if (!processor.ValidateRanges(engine.RawObject))
                    {
                        currentReport.Status = "INVALID_DATA";
                        reporter.AddResult(currentReport);
                        return;
                    }

                    // ✅ ПОДАВЛЕНИЕ ВЫВОДА RASTRWIN3
                    var originalOut = Console.Out;
                    Console.SetOut(System.IO.StreamWriter.Null);
                    try
                    {
                        engine.CalculateEmergencies();
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                    }

                    if (!engine.HasResults())
                    {
                        currentReport.Status = "NO_RESULTS";
                        reporter.AddResult(currentReport);
                        return;
                    }

                    var result = engine.GetResults();
                    currentReport.Result = result;
                    currentReport.Status = result.IsSuccess ? "OK" : "FAILED_CALCULATION";
                }
                else
                {
                    currentReport.Status = "NO_PATH";
                    reporter.AddResult(currentReport);
                }
            }
            catch (Exception ex)
            {
                currentReport.Status = $"ERROR: {ex.Message.Split(':')[0]}";
                currentReport.ErrorDetails = ex.Message;
                Console.WriteLine($"\n✗ ОШИБКА (F={factor}): {ex.Message}");
            }

            reporter.AddResult(currentReport);
        }

        static List<GeneratorModification> ConvertToModifications(
            List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)> mods)
        {
            return mods.ConvertAll(m => new GeneratorModification
            {
                Num = m.num,
                Name = m.name,
                Qmin = m.qmin,
                QmaxOld = m.qmaxOld,
                QmaxNew = m.qmaxNew
            });
        }
    }
}