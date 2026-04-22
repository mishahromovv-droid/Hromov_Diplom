using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hromov_Diplom.Models;

namespace Hromov_Diplom.Reports
{
    public class InfluenceResult
    {
        public string StationName { get; set; } = string.Empty;
        public int NumberOfPoints { get; set; }
        public int NumberOfIntervals { get; set; }
        public List<(int Index, double FactorFrom, double FactorTo, double DeltaQ, double DeltaPred, double Ki)> Data { get; set; } = new();

        public double AverageGroupCoefficient
        {
            get
            {
                if (Data.Count == 0) return 0;
                return Data.Average(d => d.Ki);
            }
        }
    }

    public class ReportManager
    {
        private readonly List<IterationReport> _reports = new();
        private readonly string _csvPath;
        private bool _headerWritten = false;

        public ReportManager(string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath)) throw new ArgumentException("Путь к CSV пуст", nameof(csvPath));
            _csvPath = csvPath;
            var dir = Path.GetDirectoryName(_csvPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
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
                string line = $"{station};{report.Factor.ToString("F2", CultureInfo.InvariantCulture)};" +
                             $"{FormatVal(report.Result.Pred)};{FormatVal(report.Result.PadpReal)};" +
                             $"{FormatVal(report.Result.ItogMdp)};{FormatVal(report.Result.ItogMdpPa)};{report.Status}";
                writer.WriteLine(line);
                writer.Flush();
            }
            catch (Exception ex) { Console.WriteLine($"[WARN] Ошибка сохранения строки: {ex.Message}"); }
        }

        public bool WriteMainResults() => true;

        public List<InfluenceResult> CalculateInfluenceCoefficients()
        {
            var results = new List<InfluenceResult>();
            if (_reports == null || _reports.Count == 0) return results;

            var stations = _reports.GroupBy(r => r.StationName).ToList();

            foreach (var group in stations)
            {
                var sorted = group.OrderBy(r => r.Factor).ToList();
                var workingReports = sorted.Where(r => r.Status != "NO_PATH" && r.Status != "NO_RESULTS").ToList();

                if (workingReports.Count < 2) continue;

                var result = new InfluenceResult
                {
                    StationName = group.Key,
                    NumberOfPoints = workingReports.Count,
                    NumberOfIntervals = 0,
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

                        // ✅ ФИЛЬТР: Ki > 0.02
                        if (ki > 0.02)
                        {
                            result.Data.Add((validIntervalIndex, prev.Factor, curr.Factor, deltaQ, deltaPred ?? 0, ki));
                            validIntervalIndex++;
                        }
                    }
                }

                result.NumberOfIntervals = result.Data.Count;
                if (result.Data.Count > 0) results.Add(result);
            }
            return results;
        }

        private static double? GetSafeDeltaPred(IterationReport prev, IterationReport curr)
        {
            if (prev.Result.Pred.HasValue && curr.Result.Pred.HasValue)
                return curr.Result.Pred.Value - prev.Result.Pred.Value;
            return null;
        }

        private double CalculateDeltaQ(IterationReport prev, IterationReport curr)
        {
            double sumQMaxCurr = curr.GeneratorModifications?.Sum(m => m.QmaxNew) ?? 0;
            double sumQMaxPrev = prev.GeneratorModifications?.Sum(m => m.QmaxNew) ?? 0;
            return Math.Abs(sumQMaxCurr - sumQMaxPrev);
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
                    {
                        writer.WriteLine($"{d.Index};{res.StationName};{d.FactorFrom:F2}-{d.FactorTo:F2};{d.FactorFrom:F2};{d.FactorTo:F2};{d.DeltaQ:F2};{d.DeltaPred:F3};{d.Ki:F9}");
                    }
                    writer.WriteLine("--- --- --- --- --- --- --- ---");
                    writer.WriteLine();
                }
                Console.WriteLine($"✅ Коэффициенты влияния сохранены: {outputFilePath}");
            }
            catch (Exception ex) { Console.WriteLine($"⚠ Ошибка экспорта: {ex.Message}"); }
        }

        private string FormatVal(double? val)
        {
            if (val.HasValue) return val.Value.ToString("F3", CultureInfo.InvariantCulture);
            return "-";
        }
    }
}