using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hromov_Diplom.Models;

namespace Hromov_Diplom.Reports
{
    #region Вспомогательные классы
    public static class StringExtensions
    {
        public static string Repeat(this string str, int count)
        {
            return string.Concat(Enumerable.Repeat(str, count));
        }
    }

    /// <summary>
    /// Результат расчёта коэффициентов влияния для одной станции
    /// </summary>
    public class InfluenceResult
    {
        public string StationName { get; set; } = string.Empty;
        public int NumberOfPoints { get; set; }
        public int NumberOfIntervals { get; set; }

        // Хранит только значимые интервалы (Ki > 0.02)
        public List<(int Index, double FactorFrom, double FactorTo, double DeltaQ, double DeltaPred, double Ki)> Data { get; set; } = new();

        // Групповой коэффициент считается только по отфильтрованным интервалам
        public double AverageGroupCoefficient
        {
            get
            {
                if (Data.Count == 0) return 0;
                return Data.Average(d => d.Ki);
            }
        }
    }
    #endregion

    /// <summary>
    /// Менеджер отчётов с поддержкой пошагового сохранения
    /// </summary>
    public class ReportManager
    {
        private readonly List<IterationReport> _reports = new();
        private readonly string _csvPath;
        private bool _headerWritten = false;

        public ReportManager(string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
                throw new ArgumentException("Путь к CSV-файлу не может быть пустым", nameof(csvPath));

            _csvPath = csvPath;

            var directoryPath = Path.GetDirectoryName(_csvPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void AddResult(IterationReport report)
        {
            _reports.Add(report);
            AppendRow(report);
        }

        private void AppendRow(IterationReport report)
        {
            try
            {
                using var writer = new StreamWriter(_csvPath, true, Encoding.UTF8);

                if (!_headerWritten)
                {
                    writer.WriteLine("Станция;Коэффициент;Предельный Переток;АДП;МДП;МДП_с_ПА;Статус");
                    _headerWritten = true;
                }

                string station = report.StationName?.Replace(";", ",") ?? "";
                string line = $"{station};" +
                             $"{report.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +
                             $"{FormatVal(report.Result.Pred)};" +
                             $"{FormatVal(report.Result.PadpReal)};" +
                             $"{FormatVal(report.Result.ItogMdp)};" +
                             $"{FormatVal(report.Result.ItogMdpPa)};" +
                             $"{report.Status}";

                writer.WriteLine(line);
                writer.Flush();

                Console.WriteLine($"[SAVE] Строка сохранена: {station} | F={report.Factor:F1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Не удалось сохранить строку: {ex.Message}");
            }
        }

        public bool WriteMainResults()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Станция;Коэффициент;Предельный Переток;АДП;МДП;МДП_с_ПА;Статус");

                foreach (var report in _reports)
                {
                    string station = report.StationName?.Replace(";", ",") ?? "";
                    string line = $"{station};" +
                                 $"{report.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +
                                 $"{FormatVal(report.Result.Pred)};" +
                                 $"{FormatVal(report.Result.PadpReal)};" +
                                 $"{FormatVal(report.Result.ItogMdp)};" +
                                 $"{FormatVal(report.Result.ItogMdpPa)};" +
                                 $"{report.Status}";

                    sb.AppendLine(line);
                }

                string directoryPath = Path.GetDirectoryName(_csvPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(_csvPath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка записи основного отчета: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Расчет всех коэффициентов влияния с фильтрацией по порогу 0.02
        /// </summary>
        public List<InfluenceResult> CalculateInfluenceCoefficients()
        {
            var results = new List<InfluenceResult>();

            if (_reports == null || _reports.Count == 0) return results;

            var stations = _reports.GroupBy(r => r.StationName).ToList();

            foreach (var group in stations)
            {
                // Сортируем по коэффициенту и исключаем неудачные запуски
                var sorted = group.OrderBy(r => r.Factor).ToList();
                var workingReports = sorted.Where(r => r.Status != "NO_PATH" && r.Status != "NO_RESULTS").ToList();

                if (workingReports.Count < 2) continue;

                var result = new InfluenceResult
                {
                    StationName = group.Key,
                    NumberOfPoints = workingReports.Count,
                    NumberOfIntervals = 0, // Установится после фильтрации
                    Data = new List<(int, double, double, double, double, double)>()
                };

                int validIntervalIndex = 1;
                for (int i = 1; i < workingReports.Count; i++)
                {
                    var prev = workingReports[i - 1];
                    var curr = workingReports[i];

                    double? deltaPred = GetSafeDeltaPred(prev, curr);
                    double deltaQ = CalculateDeltaQ(prev, curr);

                    if (deltaQ > 1e-6)
                    {
                        double ki = (deltaPred.HasValue && Math.Abs(deltaPred.Value) > 1e-6)
                            ? deltaPred.Value / deltaQ
                            : 0.0;

                        // ✅ ФИЛЬТР: берем только интервалы, где Ki > 0.02
                        if (ki > 0.02)
                        {
                            result.Data.Add((validIntervalIndex, prev.Factor, curr.Factor, deltaQ, deltaPred ?? 0, ki));
                            validIntervalIndex++;
                        }
                    }
                }

                // ✅ Количество интервалов = только значимые переходы
                result.NumberOfIntervals = result.Data.Count;

                if (result.Data.Count > 0)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        private static double? GetSafeDeltaPred(IterationReport prev, IterationReport curr)
        {
            if (prev.Result.Pred.HasValue && curr.Result.Pred.HasValue)
            {
                return curr.Result.Pred.Value - prev.Result.Pred.Value;
            }
            return null;
        }

        /// <summary>
        /// Расчет ΔQ на основе реальной разницы суммарных Qmax генераторов
        /// </summary>
        private double CalculateDeltaQ(IterationReport prev, IterationReport curr)
        {
            double sumQMaxCurr = curr.GeneratorModifications?.Sum(m => m.QmaxNew) ?? 0;
            double sumQMaxPrev = prev.GeneratorModifications?.Sum(m => m.QmaxNew) ?? 0;
            return Math.Abs(sumQMaxCurr - sumQMaxPrev);
        }

        /// <summary>
        /// Экспорт результатов в CSV
        /// </summary>
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

                    double groupKb = res.AverageGroupCoefficient;
                    writer.WriteLine($"ГРУППОВОЙ;—;-;-;-;{groupKb:F9}");
                    writer.WriteLine();

                    foreach (var d in res.Data)
                    {
                        writer.WriteLine($"{d.Index};{res.StationName};{d.FactorFrom:F1}-{d.FactorTo:F1};{d.FactorFrom:F1};{d.FactorTo:F1};{d.DeltaQ:F2};{d.DeltaPred:F3};{d.Ki:F9}");
                    }

                    writer.WriteLine("--- --- --- --- --- --- --- ---");
                    writer.WriteLine();
                }

                Console.WriteLine($"✅ Коэффициенты влияния сохранены: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Ошибка экспорта коэффициентов влияния: {ex.Message}");
            }
        }

        private string FormatVal(double? val)
        {
            if (val.HasValue) return val.Value.ToString("F3", CultureInfo.InvariantCulture);
            return "-";
        }
    }
}