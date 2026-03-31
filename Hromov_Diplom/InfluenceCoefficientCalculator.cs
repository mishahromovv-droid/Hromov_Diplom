using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Hromov_Diplom.Calculators
{
    /// <summary>
    /// Калькулятор коэффициентов влияния на основе результатов пакетных расчётов МДП
    /// </summary>
    public class InfluenceCoefficientCalculator
    {
        /// <summary>
        /// Структура данных для хранения отчёта по одной итерации (должна быть public!)
        /// </summary>
        public class CsvReportRow
        {
            public string Station { get; set; } = string.Empty;
            public double Factor { get; set; }
            public double Pred { get; set; }
            public double PadpReal { get; set; }
            public double ItogMdp { get; set; }
        }

        /// <summary>
        /// Класс для хранения одного индивидуального коэффициента влияния
        /// </summary>
        public class InfluenceRecord
        {
            public string StationName { get; set; } = string.Empty;
            public double FactorFrom { get; set; }
            public double FactorTo { get; set; }
            public double DeltaQ_MVar { get; set; }
            public double DeltaPred_MW { get; set; }
            public double IndividualKB { get; set; }
        }

        /// <summary>
        /// Структура результата по станции
        /// </summary>
        public class StationInfluenceResult
        {
            public string StationName { get; set; } = string.Empty;
            public int NumberOfPoints { get; set; }
            public int NumberOfIntervals { get; set; }
            public List<double> IndividualCoefficients { get; set; } = new();
            public double AverageGroupCoefficient { get; set; }
        }

        /// <summary>
        /// Расчёт коэффициентов влияния по CSV-файлу с результатами МДП
        /// </summary>
        /// <param name="csvFilePath">Путь к CSV-файлу с результатами</param>
        /// <returns>Список результатов по станциям</returns>
        public static List<StationInfluenceResult> CalculateFromCsvFile(string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"Файл не найден: {csvFilePath}");
            }

            var lines = File.ReadAllLines(csvFilePath);
            var reportsByStation = new Dictionary<string, List<CsvReportRow>>();
            var stationResults = new List<StationInfluenceResult>();

            // Парсинг CSV (пропуск заголовка)
            foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var parts = line.Split(';');
                if (parts.Length >= 5)
                {
                    var stationName = parts[0].Trim().Replace("\"", "");
                    var factor = Convert.ToDouble(parts[1], CultureInfo.InvariantCulture);
                    var pred = Convert.ToDouble(parts[2], CultureInfo.InvariantCulture);

                    if (!reportsByStation.ContainsKey(stationName))
                        reportsByStation[stationName] = new List<CsvReportRow>();

                    reportsByStation[stationName].Add(new CsvReportRow
                    {
                        Station = stationName,
                        Factor = factor,
                        Pred = pred,
                        PadpReal = parts.Length >= 4 ? Convert.ToDouble(parts[3], CultureInfo.InvariantCulture) : 0,
                        ItogMdp = parts.Length >= 5 ? Convert.ToDouble(parts[4], CultureInfo.InvariantCulture) : 0
                    });
                }
            }

            // Обработка каждой станции
            foreach (var kvp in reportsByStation)
            {
                var rowList = kvp.Value.OrderBy(r => r.Factor).ToList();

                if (rowList.Count < 2)
                    continue;

                var result = new StationInfluenceResult
                {
                    StationName = kvp.Key,
                    NumberOfPoints = rowList.Count,
                    NumberOfIntervals = rowList.Count - 1,
                    IndividualCoefficients = new List<double>()
                };

                double sumCoefficients = 0;

                // Расчёт по интервалам
                for (int i = 1; i < rowList.Count; i++)
                {
                    var prev = rowList[i - 1];
                    var curr = rowList[i];

                    // ΔP_pred
                    double deltaPred = curr.Pred - prev.Pred;

                    // ΔQ оценка (упрощённая)
                    double estimatedDeltaQ = 750 * (curr.Factor - prev.Factor); // Типичное значение

                    if (Math.Abs(estimatedDeltaQ) > 1e-6 && Math.Abs(deltaPred) > 1e-6)
                    {
                        double individualKB = deltaPred / estimatedDeltaQ;
                        result.IndividualCoefficients.Add(individualKB);
                        sumCoefficients += individualKB;
                    }
                }

                // Среднее значение
                if (result.IndividualCoefficients.Count > 0)
                {
                    result.AverageGroupCoefficient = sumCoefficients / result.IndividualCoefficients.Count;
                }

                stationResults.Add(result);
            }

            return stationResults;
        }

        /// <summary>
        /// Экспорт результатов в CSV
        /// </summary>
        public static void ExportToCsv(List<StationInfluenceResult> results, string outputFilePath)
        {
            using var writer = new StreamWriter(outputFilePath, false, System.Text.Encoding.UTF8);

            // Заголовок таблицы
            writer.WriteLine("Станция;Количество точек(M);Количество интервалов(M-1);Групповой коэффициент влияния(K_B)");
            writer.WriteLine("");
            writer.WriteLine("─".Repeat(100));
            writer.WriteLine("                   ИНДИВИДУАЛЬНЫЕ КОЭФФИЦИЕНТЫ ВЛИЯНИЯ");
            writer.WriteLine("─".Repeat(100));
            writer.WriteLine("Станция;Интервал от;Интервал до;ΔQ_Мвар;ΔP_МВт;K_B_инд");
            writer.WriteLine();

            foreach (var res in results)
            {
                writer.WriteLine($"┌──────────────────────────────────────────────────────────────┐");
                writer.WriteLine($"│ СТАНЦИЯ: {res.StationName,-36} │");
                writer.WriteLine($"│ Точек расчёта: {res.NumberOfPoints,4} │");
                writer.WriteLine($"│ Интервалы: {res.NumberOfIntervals,4} │");
                writer.WriteLine($"│ Группа K_B: {res.AverageGroupCoefficient:+#.####;-#.####;0.######} МВт/Мвар │");
                writer.WriteLine($"└──────────────────────────────────────────────────────────────┘");
                writer.WriteLine();

                // Детализация
                for (int i = 0; i < res.IndividualCoefficients.Count; i++)
                {
                    writer.WriteLine($"  Переход {i + 1}: {res.IndividualCoefficients[i]:+#.####;-#.####;0.######} МВт/Мвар");
                }

                writer.WriteLine();
            }

            Console.WriteLine($"Результаты экспортированы: {outputFilePath}");
        }
    }

    #region Вспомогательный класс расширения для строк
    public static class StringExtensions
    {
        public static string Repeat(this string str, int count)
        {
            return string.Concat(Enumerable.Repeat(str, count));
        }
    }
    #endregion
}