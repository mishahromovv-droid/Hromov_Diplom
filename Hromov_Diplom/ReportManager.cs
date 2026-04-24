#nullable disable
using Hromov_Diplom.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hromov_Diplom.Reports
{
    public static class StringExtensions
    {
        public static string Repeat(this string str, int count) => string.Concat(Enumerable.Repeat(str, count));
    }

    public class InfluenceResult
    {
        public string StationName { get; set; } = string.Empty;
        public int NumberOfPoints { get; set; }
        public int NumberOfIntervals { get; set; }
        public List<(int Index, double FactorFrom, double FactorTo, double DeltaQ, double DeltaPred, double Ki)> Data { get; set; } = new();
        public double AverageGroupCoefficient => Data.Count > 0 ? Data.Average(d => d.Ki) : 0;
    }

    public class ReportManager
    {
        private readonly List<IterationReport> _reports = new();
        private readonly string _csvPath;
        private bool _headerWritten = false;

        public ReportManager(string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath)) throw new ArgumentException("Пустой путь CSV", nameof(csvPath));
            _csvPath = csvPath;

            // При создании менеджера сразу создаем заголовок файла, чтобы он существовал
            if (!File.Exists(_csvPath))
            {
                using var initWriter = new StreamWriter(_csvPath, false, Encoding.UTF8);
                initWriter.WriteLine("Станция;Коэффициент;Предельный Переток;АДП;МДП;МДП_с_ПА;Статус");
            }
        }

        public void AddResult(IterationReport report)
        {
            _reports.Add(report);
            AppendRow(report);
        }

        // ✅ НОВЫЙ МЕТОД: Принудительное сохранение всех данных на диск
        public void SaveCurrentResults()
        {
            try
            {
                // Перезаписываем файл полностью, чтобы гарантировать целостность
                using var writer = new StreamWriter(_csvPath, false, Encoding.UTF8);
                writer.WriteLine("Станция;Коэффициент;Предельный Переток;АДП;МДП;МДП_с_ПА;Статус");

                foreach (var report in _reports)
                {
                    string station = report.StationName?.Replace(";", ",") ?? "";
                    string line = $"{station};{report.Factor.ToString("F2", CultureInfo.InvariantCulture)};" +
                                 $"{FormatVal(report.Result.Pred)};{FormatVal(report.Result.PadpReal)};" +
                                 $"{FormatVal(report.Result.ItogMdp)};{FormatVal(report.Result.ItogMdpPa)};{report.Status}";
                    writer.WriteLine(line);
                }
                // Console.WriteLine($"[AUTO-SAVE] Сохранено строк: {_reports.Count}"); // Можно раскомментировать для отладки
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Ошибка автосохранения: {ex.Message}");
            }
        }

        private void AppendRow(IterationReport report)
        {
            // Этот метод оставляем для быстрого добавления, но основной упор теперь на SaveCurrentResults
            try
            {
                using var writer = new StreamWriter(_csvPath, true, Encoding.UTF8);
                string station = report.StationName?.Replace(";", ",") ?? "";
                string line = $"{station};{report.Factor.ToString("F2", CultureInfo.InvariantCulture)};" +
                             $"{FormatVal(report.Result.Pred)};{FormatVal(report.Result.PadpReal)};" +
                             $"{FormatVal(report.Result.ItogMdp)};{FormatVal(report.Result.ItogMdpPa)};{report.Status}";
                writer.WriteLine(line);
            }
            catch (Exception ex) { Console.WriteLine($"[WARN] Ошибка доп. сохранения: {ex.Message}"); }
        }

        public int GetReportCount() => _reports.Count;

        public List<InfluenceResult> CalculateInfluenceCoefficients()
        {
            var results = new List<InfluenceResult>();
            if (_reports == null || _reports.Count == 0) return results;

            foreach (var group in _reports.GroupBy(r => r.StationName))
            {
                var working = group.OrderBy(r => r.Factor).Where(r => r.Status != "NO_PATH" && r.Status != "NO_RESULTS").ToList();
                if (working.Count < 2) continue;

                var result = new InfluenceResult
                {
                    StationName = group.Key,
                    NumberOfPoints = working.Count,
                    Data = new List<(int, double, double, double, double, double)>()
                };

                int idx = 1;
                for (int i = 1; i < working.Count; i++)
                {
                    var prev = working[i - 1];
                    var curr = working[i];

                    double? deltaPred = prev.Result.Pred.HasValue && curr.Result.Pred.HasValue
                        ? curr.Result.Pred.Value - prev.Result.Pred.Value : null;
                    double deltaQ = CalculateDeltaQ(prev, curr);

                    if (deltaQ > 1e-6 && deltaPred.HasValue && Math.Abs(deltaPred.Value) > 1e-6)
                    {
                        double ki = deltaPred.Value / deltaQ;
                        if (ki > 0.02)
                            result.Data.Add((idx++, prev.Factor, curr.Factor, deltaQ, deltaPred.Value, ki));
                    }
                }

                result.NumberOfIntervals = result.Data.Count;
                if (result.Data.Count > 0) results.Add(result);
            }
            return results;
        }

        private double CalculateDeltaQ(IterationReport prev, IterationReport curr)
        {
            double sumCurr = curr.GeneratorModifications?.Sum(m => m.QmaxNew) ?? 0;
            double sumPrev = prev.GeneratorModifications?.Sum(m => m.QmaxNew) ?? 0;
            return Math.Abs(sumCurr - sumPrev);
        }

        public void ExportInfluenceCoefficientsToCsv(List<InfluenceResult> results, string outputFilePath)
        {
            if (string.IsNullOrEmpty(outputFilePath)) return;
            try
            {
                using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8);
                writer.WriteLine("=== КОЭФФИЦИЕНТЫ ВЛИЯНИЯ РЕАКТИВНОЙ МОЩНОСТИ ===");
                writer.WriteLine("Станция;Переход;k_от;k_до;ΔQ_Мвар;ΔPred_МВт;k_v_коэффициент");
                writer.WriteLine();

                foreach (var res in results)
                {
                    writer.WriteLine($"*** СТАНЦИЯ: {res.StationName} ***");
                    writer.WriteLine($"ГРУППОВОЙ;—;-;-;-;{res.AverageGroupCoefficient:F9}");
                    writer.WriteLine();
                    foreach (var d in res.Data)
                        writer.WriteLine($"{d.Index};{res.StationName};{d.Item2:F2}-{d.Item3:F2};{d.Item2:F2};{d.Item3:F2};{d.Item4:F2};{d.Item5:F3};{d.Item6:F9}");
                    writer.WriteLine("--- --- --- --- --- --- --- ---\n");
                }
                Console.WriteLine($"✅ Коэффициенты сохранены: {outputFilePath}");
            }
            catch (Exception ex) { Console.WriteLine($"⚠ Ошибка экспорта: {ex.Message}"); }
        }

        public static void ExportCleanDataWithSums(List<GeneratorReportData> allGenerators, string filePath)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("Станция;Номер генератора;Наименование;Активная мощность P (МВт);Реактивная мощность Q (Мвар);Qmin (Мвар);Qmax (Мвар);sta");

                foreach (var stationGroup in allGenerators.GroupBy(g => g.StationName).OrderBy(g => g.Key))
                {
                    var generators = stationGroup.ToList();
                    writer.WriteLine("");
                    writer.WriteLine($">>> СТАНЦИЯ: {stationGroup.Key}");
                    writer.WriteLine($"Всего генераторов: {generators.Count} шт.");

                    double sumP = 0, sumQ = 0, sumQmax = 0;
                    foreach (var gen in generators.Where(g => g.StatusFlag == 0))
                    {
                        sumP += gen.ActivePower;
                        sumQ += gen.ReactivePower;
                        sumQmax += gen.Qmax;
                    }

                    foreach (var gen in generators.OrderBy(g => g.GeneratorNum))
                    {
                        string pStr = gen.ActivePower != 0 ? gen.ActivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string qStr = gen.ReactivePower != 0 ? gen.ReactivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        writer.WriteLine($"{stationGroup.Key};{gen.GeneratorNum:D3};\"{EscapeCsv(gen.Name)}\";{pStr};{qStr};{gen.Qmin:F0};{gen.Qmax:F0};{gen.StatusFlag}");
                    }

                    writer.WriteLine("--------------------------------------------");
                    double diff = sumQmax - sumQ;
                    writer.WriteLine($"СУММА;{stationGroup.Key};;{sumP:F3};{sumQ:F3};;;{sumQmax:F3};{diff:F3}\n");
                }

                double grandQmax = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.Qmax);
                double grandP = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.ActivePower);
                double grandQ = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.ReactivePower);
                writer.WriteLine("================================================================================");
                writer.WriteLine($"Общее количество обработанных генераторов: {allGenerators.Count} шт.");
                writer.WriteLine($"Суммарная активная мощность (P, ста=0): {grandP:F3} МВт");
                writer.WriteLine($"Суммарная реактивная мощность (Q, ста=0): {grandQ:F3} Мвар");
                writer.WriteLine($"Суммарный предел Qmax (ста=0): {grandQmax:F3} Мвар");
                writer.WriteLine("=== КОНЕЦ ФАЙЛА ===");

                // Не выводим лишний раз в консоль, чтобы не засорять
                // Console.WriteLine($"✅ Данные сохранены: {filePath}");
            }
            catch (Exception ex) { Console.WriteLine($"❌ Ошибка: {ex.Message}"); }
        }

        public static void ExportStationQSummary(List<GeneratorReportData> allGenerators, string outputCsvPath)
        {
            try
            {
                using var writer = new StreamWriter(outputCsvPath, false, Encoding.UTF8);
                writer.WriteLine("Станция;sumQ (Мвар);sumQmax (Мвар);diff (Мвар)");

                foreach (var stationGroup in allGenerators.GroupBy(g => g.StationName).OrderBy(g => g.Key))
                {
                    double sumQ = 0, sumQmax = 0;
                    foreach (var gen in stationGroup.Where(g => g.StatusFlag == 0))
                    {
                        sumQ += gen.ReactivePower;
                        sumQmax += gen.Qmax;
                    }
                    double diff = sumQmax - sumQ;
                    writer.WriteLine($"{stationGroup.Key};{sumQ:F3};{sumQmax:F3};{diff:F3}");
                }
                // Console.WriteLine($"✅ Сводная таблица сохранена: {outputCsvPath}");
            }
            catch (Exception ex) { Console.WriteLine($"❌ Ошибка: {ex.Message}"); }
        }

        private static string EscapeCsv(string text) => '"' + text.Replace("\"", "\"\"") + '"';
        private string FormatVal(double? val) => val.HasValue ? val.Value.ToString("F3", CultureInfo.InvariantCulture) : "-";
    }
}