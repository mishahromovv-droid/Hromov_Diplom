using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hromov_Diplom.Models;

namespace Hromov_Diplom.Reports
{
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

        // Хранит все успешные расчёты Ki
        public List<(int Index, double FactorFrom, double FactorTo, double DeltaQ, double DeltaPred, double Ki)> Data { get; set; } = new();

        // Вычисляемое свойство: средняя величина K_B только по реально работающим интервалам
        public double AverageGroupCoefficient
        {
            get
            {
                if (Data.Count == 0) return 0;
                return Data.Average(d => d.Ki);
            }
        }
    }

    /// <summary>
    /// Менеджер отчётов с поддержкой пошагового сохранения
    /// </summary>
    public class ReportManager
    {
        private readonly List<IterationReport> _reports = new();
        private readonly string _csvPath;
        private bool _headerWritten = false;
        private StreamWriter? _fileWriter;

        public ReportManager(string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
                throw new ArgumentException("Путь к CSV-файлу не может быть пустым", nameof(csvPath));

            _csvPath = csvPath;

            // Открываем файл для записи (заголовок будет написан на первой добавке)
            var directoryPath = Path.GetDirectoryName(_csvPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void AddResult(IterationReport report)
        {
            _reports.Add(report);
            // ✅ САМОСТОЯТЕЛЬНОЕ СОХРАНЕНИЕ после добавления
            AppendRow(report);
        }

        /// <summary>
        /// Добавляет одну строку в CSV-файл без перезаписи всего файла
        /// </summary>
        private void AppendRow(IterationReport report)
        {
            try
            {
                using var writer = new StreamWriter(_csvPath, true, Encoding.UTF8);

                // Пишем заголовок только один раз
                if (!_headerWritten)
                {
                    writer.WriteLine("Станция;Коэффициент;Предельный Переток;АДП;МДП;МДП_с_ПА;Статус");
                    _headerWritten = true;
                }

                // Записываем текущую строку
                string station = report.StationName?.Replace(";", ",") ?? "";
                string line = $"{station};" +
                             $"{report.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +
                             $"{FormatVal(report.Result.Pred)};" +
                             $"{FormatVal(report.Result.PadpReal)};" +
                             $"{FormatVal(report.Result.ItogMdp)};" +
                             $"{FormatVal(report.Result.ItogMdpPa)};" +
                             $"{report.Status}";

                writer.WriteLine(line);
                writer.Flush(); // Принудительно сохраняем в буфер

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
                // Если данные уже писались пошагово, этот метод можно опустить или использовать для финальной проверки
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
        /// Расчет всех коэффициентов влияния (ВСЕХ интервалов)
        /// </summary>
        public List<InfluenceResult> CalculateInfluenceCoefficients()
        {
            var results = new List<InfluenceResult>();

            if (_reports == null || _reports.Count == 0) return results;

            var stations = _reports.GroupBy(r => r.StationName).ToList();

            foreach (var group in stations)
            {
                var sorted = group.OrderBy(r => r.Factor).ToList();
                var workingReports = sorted.Where(r => r.Status != "NO_PATH").ToList();

                if (workingReports.Count < 2) continue;

                var result = new InfluenceResult
                {
                    StationName = group.Key,
                    NumberOfPoints = workingReports.Count,
                    NumberOfIntervals = workingReports.Count - 1,
                    Data = new List<(int, double, double, double, double, double)>()
                };

                for (int i = 1; i < workingReports.Count; i++)
                {
                    var prev = workingReports[i - 1];
                    var curr = workingReports[i];

                    // Получаем предельный переток
                    double? deltaPred = GetSafeDeltaPred(prev, curr);

                    // ✅ Автоматически вычисляем ΔQ из данных отчетов о модификации генераторов
                    double deltaQ = CalculateDeltaQ(prev, curr);

                    // Если ΔQ > 0, то считаем Ki (даже при малом ΔPred)
                    if (deltaQ > 1e-6)
                    {
                        double ki = (deltaPred.HasValue && Math.Abs(deltaPred.Value) > 1e-6)
                            ? deltaPred.Value / deltaQ
                            : 0.0;

                        result.Data.Add((i, prev.Factor, curr.Factor, deltaQ, deltaPred ?? 0, ki));
                    }
                }

                if (result.Data.Count > 0) results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Безопасное получение разницы в P_pred
        /// </summary>
        private static double? GetSafeDeltaPred(IterationReport prev, IterationReport curr)
        {
            if (prev.Result.Pred.HasValue && curr.Result.Pred.HasValue)
            {
                return curr.Result.Pred.Value - prev.Result.Pred.Value;
            }
            return null;
        }

        /// <summary>
        /// ✅ Автоматический расчет ΔQ на основе измененных диапазонов генераторов
        /// </summary>
        private double CalculateDeltaQ(IterationReport prev, IterationReport curr)
        {
            // Суммарный объем Q по всем генераторам для предыдущей и текущей точки
            // Используем Prev.Factor и Curr.Factor (коэффициенты масштабирования), а не m.Factor
            double sumQ_prev = CalculateQVolumeFromMods(prev);
            double sumQ_curr = CalculateQVolumeFromMods(curr);

            return Math.Abs(sumQ_curr - sumQ_prev);
        }

        /// <summary>
        /// Расчет суммарного объема реактивной мощности из модификаций генераторов
        /// </summary>
        private double CalculateQVolumeFromMods(IterationReport report)
        {
            if (report.GeneratorModifications == null || report.GeneratorModifications.Count == 0)
            {
                // Если данных нет — используем приближенную оценку
                return 750.0 * report.Factor;
            }

            // Суммируем: Qmin + |Qmax_old| × k для каждого генератора
            double totalQ = report.GeneratorModifications.Sum(m =>
                Math.Abs(m.Qmin) + Math.Abs(m.QmaxOld) * report.Factor);

            return totalQ;
        }

        /// <summary>
        /// Экспорт с чёткой подписью каждой станции
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