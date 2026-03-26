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

    public class ReportManager
    {
        private readonly List<IterationReport> _reports = new();
        private readonly string _csvPath;

        public ReportManager(string csvPath) => _csvPath = csvPath;

        public void AddResult(IterationReport report) => _reports.Add(report);

        public void WriteToFile(bool append = false)
        {
            try
            {
                var sb = new StringBuilder();

                if (!append)
                {
                    sb.AppendLine("Station;Factor;Pred;PadpReal;ItogMdp;ItogMdpPa;Status");
                }

                // Сначала пишем основные результаты
                foreach (var report in _reports)
                {
                    string station = report.StationName.Replace(";", ",");

                    string line = $"{station};" +
                                 $"{report.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +
                                 $"{FormatNull(report.Result.Pred)};" +
                                 $"{FormatNull(report.Result.PadpReal)};" +
                                 $"{FormatNull(report.Result.ItogMdp)};" +
                                 $"{FormatNull(report.Result.ItogMdpPa)};" +
                                 $"{report.Status}";

                    sb.AppendLine(line);
                }

                // Добавляем пустые строки и таблицу параметров
                sb.AppendLine();
                sb.AppendLine("════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════");
                sb.AppendLine("                    ПАРАМЕТРЫ ГЕНЕРАТОРОВ ПО ИТЕРАЦИЯМ");
                sb.AppendLine("════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════");

                string currentStation = null;

                foreach (var report in _reports)
                {
                    // Заголовок станции
                    if (currentStation != report.StationName)
                    {
                        currentStation = report.StationName;
                        sb.AppendLine();
                        sb.AppendLine("─".Repeat(100));
                        sb.AppendLine($"  СТАНЦИЯ: {currentStation}");
                        sb.AppendLine("─".Repeat(100));
                    }

                    // Заголовок коэффициента и таблица
                    sb.AppendLine();
                    sb.AppendLine($"  Коэффициент: {report.Factor:F1}");
                    sb.AppendLine("  +--------+----------------------------------+----------+----------+----------+");
                    sb.AppendLine("  |  Num   |              Name                |   Qmin   |  QmaxOld |  QmaxNew |");
                    sb.AppendLine("  +--------+----------------------------------+----------+----------+----------+");

                    if (report.GeneratorModifications.Count > 0)
                    {
                        foreach (var gen in report.GeneratorModifications)
                        {
                            sb.AppendLine($"  |  {gen.Num,4}  |  {gen.Name,-32}  | {gen.Qmin,8:F2} | {gen.QmaxOld,8:F2} | {gen.QmaxNew,8:F2} |");
                        }
                    }
                    else
                    {
                        sb.AppendLine("  |                                Нет данных                                |");
                    }

                    sb.AppendLine("  +--------+----------------------------------+----------+----------+----------+");
                }

                sb.AppendLine();
                sb.AppendLine("════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════");

                File.WriteAllText(_csvPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Запись CSV не удалась: {ex.Message}");
            }
        }

        /// <summary>
        /// Печать таблицы с параметрами генераторов (Qmin, Qmax) для каждой итерации
        /// </summary>
        public void PrintGeneratorParametersTable()
        {
            if (_reports.Count == 0)
            {
                Console.WriteLine("[WARN] Нет данных для вывода");
                return;
            }

            Console.WriteLine($"\n{'=',100}");
            Console.WriteLine("                    ПАРАМЕТРЫ ГЕНЕРАТОРОВ ПО ИТЕРАЦИЯМ");
            Console.WriteLine($"{'=',100}");

            string currentStation = null;

            foreach (var report in _reports)
            {
                // Заголовок станции
                if (currentStation != report.StationName)
                {
                    currentStation = report.StationName;
                    Console.WriteLine($"\n{'─',100}");
                    Console.WriteLine($"  СТАНЦИЯ: {currentStation}");
                    Console.WriteLine($"{'─',100}");
                }

                // Заголовок коэффициента
                Console.WriteLine($"\n  Коэффициент: {report.Factor:F1}");
                Console.WriteLine("  +--------+----------------------------------+----------+----------+----------+");
                Console.WriteLine("  |  Num   │              Name                │   Qmin   │  QmaxOld │  QmaxNew │");
                Console.WriteLine("  +--------+----------------------------------+----------+----------+----------+");

                if (report.GeneratorModifications.Count > 0)
                {
                    foreach (var gen in report.GeneratorModifications)
                    {
                        Console.WriteLine($"  |  {gen.Num,4}  │  {gen.Name,-32}  │ {gen.Qmin,8:F2} │ {gen.QmaxOld,8:F2} │ {gen.QmaxNew,8:F2} │");
                    }
                }
                else
                {
                    Console.WriteLine("  |                                Нет данных                                |");
                }

                Console.WriteLine("  +--------+----------------------------------+----------+----------+----------+");
            }

            Console.WriteLine($"\n{'=',100}");
        }

        /// <summary>
        /// Расчёт и печать коэффициентов влияния
        /// </summary>
        public void PrintInfluenceCoefficients()
        {
            if (_reports.Count == 0)
            {
                Console.WriteLine("[WARN] Нет данных для расчёта");
                return;
            }

            Console.WriteLine($"\n{'=',100}");
            Console.WriteLine("                    РАСЧЁТ КОЭФФИЦИЕНТОВ ВЛИЯНИЯ");
            Console.WriteLine($"{'=',100}");

            // Группируем отчёты по станциям
            var stations = _reports.GroupBy(r => r.StationName).ToList();

            foreach (var stationGroup in stations)
            {
                var stationName = stationGroup.Key;
                var sortedReports = stationGroup.OrderBy(r => r.Factor).ToList();

                Console.WriteLine($"\n{'─',100}");
                Console.WriteLine($"  СТАНЦИЯ: {stationName}");
                Console.WriteLine($"{'─',100}");

                // Пример расчёта для первого перехода 0.1 → 0.2
                bool exampleShown = false;

                for (int i = 1; i < sortedReports.Count; i++)
                {
                    var prev = sortedReports[i - 1];
                    var curr = sortedReports[i];

                    // Пропускаем если нет результатов
                    if (!prev.Result.Pred.HasValue || !curr.Result.Pred.HasValue)
                        continue;

                    // Σ(|Qmin| + |QmaxOld|) для предыдущего коэффициента
                    double sumQ_prev = prev.GeneratorModifications.Sum(g => Math.Abs(g.Qmin) + Math.Abs(g.QmaxOld));

                    // Σ(|Qmin| + |QmaxOld|) для текущего коэффициента
                    double sumQ_curr = curr.GeneratorModifications.Sum(g => Math.Abs(g.Qmin) + Math.Abs(g.QmaxOld));

                    // ΔQ_суммарное = (ΣQ × factor_curr) - (ΣQ × factor_prev)
                    double deltaQ = (sumQ_curr * curr.Factor) - (sumQ_prev * prev.Factor);

                    // ΔPred
                    double deltaPred = curr.Result.Pred.Value - prev.Result.Pred.Value;

                    // Коэффициент влияния
                    double influenceCoeff = deltaQ != 0 ? deltaPred / deltaQ : 0;

                    // Подробный расчёт для первого перехода
                    if (!exampleShown)
                    {
                        string deltaQStr = deltaQ >= 0 ? $"+{deltaQ:F2}" : $"{deltaQ:F2}";
                        string deltaPredStr = deltaPred >= 0 ? $"+{deltaPred:F3}" : $"{deltaPred:F3}";
                        string coeffStr = influenceCoeff >= 0 ? $"+{influenceCoeff:F6}" : $"{influenceCoeff:F6}";

                        Console.WriteLine($"\n  ╔═══════════════════════════════════════════════════════════════════════════════╗");
                        Console.WriteLine($"  ║  ПРИМЕР РАСЧЁТА для перехода {prev.Factor:F1} → {curr.Factor:F1}");
                        Console.WriteLine($"  ╠═══════════════════════════════════════════════════════════════════════════════╣");
                        Console.WriteLine($"  ║  1) Σ(|Qmin| + |QmaxOld|) при k={prev.Factor:F1}:");
                        Console.WriteLine($"  ║     ΣQ = {sumQ_prev:F2} МВар");
                        Console.WriteLine($"  ║  2) Σ(|Qmin| + |QmaxOld|) при k={curr.Factor:F1}:");
                        Console.WriteLine($"  ║     ΣQ = {sumQ_curr:F2} МВар");
                        Console.WriteLine($"  ║  3) ΔQ_суммарное = (ΣQ × {curr.Factor:F1}) - (ΣQ × {prev.Factor:F1})");
                        Console.WriteLine($"  ║            = ({sumQ_curr:F2} × {curr.Factor:F1}) - ({sumQ_prev:F2} × {prev.Factor:F1})");
                        Console.WriteLine($"  ║            = {sumQ_curr * curr.Factor:F2} - {sumQ_prev * prev.Factor:F2}");
                        Console.WriteLine($"  ║            = {deltaQStr} МВар");
                        Console.WriteLine($"  ║  4) ΔPred = Pred({curr.Factor:F1}) - Pred({prev.Factor:F1})");
                        Console.WriteLine($"  ║            = {curr.Result.Pred.Value:F3} - {prev.Result.Pred.Value:F3}");
                        Console.WriteLine($"  ║            = {deltaPredStr} МВт");
                        Console.WriteLine($"  ║  5) Коэф. влияния = ΔPred / ΔQ");
                        Console.WriteLine($"  ║                    = {deltaPredStr} / {deltaQStr}");
                        Console.WriteLine($"  ║                    = {coeffStr} МВт/МВар");
                        Console.WriteLine($"  ╚═══════════════════════════════════════════════════════════════════════════════╝");
                        exampleShown = true;
                    }
                    else
                    {
                        Console.WriteLine($"\n  Переход: {prev.Factor:F1} → {curr.Factor:F1}");
                        Console.WriteLine($"  ├─ ΔQ = {deltaQ:+#.##;-#.##;0} МВар");
                        Console.WriteLine($"  ├─ ΔPred = {deltaPred:+#.###;-#.###;0} МВт");
                        Console.WriteLine($"  └─ Коэф. влияния = {influenceCoeff:+#.####;-#.####;0} МВт/МВар");
                    }
                }
            }

            Console.WriteLine($"\n{'=',100}");
        }

        /// <summary>
        /// Экспорт коэффициентов влияния в CSV файл
        /// </summary>
        public void ExportInfluenceCoefficientsToCsv(string csvPath)
        {
            if (_reports.Count == 0)
            {
                Console.WriteLine("[WARN] Нет данных для экспорта коэффициентов влияния");
                return;
            }

            try
            {
                var sb = new StringBuilder();
                
                // Заголовок CSV файла
                sb.AppendLine("Station;Transition;Factor_From;Factor_To;Delta_Q_MVar;Delta_Pred_MW;Influence_Coefficient_MW_per_MVar");

                // Группируем отчёты по станциям
                var stations = _reports.GroupBy(r => r.StationName).ToList();

                foreach (var stationGroup in stations)
                {
                    var stationName = stationGroup.Key.Replace(";", ",");
                    var sortedReports = stationGroup.OrderBy(r => r.Factor).ToList();

                    for (int i = 1; i < sortedReports.Count; i++)
                    {
                        var prev = sortedReports[i - 1];
                        var curr = sortedReports[i];

                        // Пропускаем если нет результатов
                        if (!prev.Result.Pred.HasValue || !curr.Result.Pred.HasValue)
                            continue;

                        // Σ(|Qmin| + |QmaxOld|) для предыдущего коэффициента
                        double sumQ_prev = prev.GeneratorModifications.Sum(g => Math.Abs(g.Qmin) + Math.Abs(g.QmaxOld));

                        // Σ(|Qmin| + |QmaxOld|) для текущего коэффициента
                        double sumQ_curr = curr.GeneratorModifications.Sum(g => Math.Abs(g.Qmin) + Math.Abs(g.QmaxOld));

                        // ΔQ_суммарное
                        double deltaQ = (sumQ_curr * curr.Factor) - (sumQ_prev * prev.Factor);

                        // ΔPred
                        double deltaPred = curr.Result.Pred.Value - prev.Result.Pred.Value;

                        // Коэффициент влияния
                        double influenceCoeff = deltaQ != 0 ? deltaPred / deltaQ : 0;

                        // Формируем строку
                        string transition = $"{prev.Factor:F1}-{curr.Factor:F1}";
                        sb.AppendLine($"{stationName};" +
                                     $"{transition};" +
                                     $"{prev.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +
                                     $"{curr.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +
                                     $"{deltaQ.ToString("F2", CultureInfo.InvariantCulture)};" +
                                     $"{deltaPred.ToString("F3", CultureInfo.InvariantCulture)};" +
                                     $"{influenceCoeff.ToString("F6", CultureInfo.InvariantCulture)}");
                    }
                }

                File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
                Console.WriteLine($"\n[INFO] Коэффициенты влияния экспортированы: {csvPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Экспорт коэффициентов влияния не удался: {ex.Message}");
            }
        }

        private string FormatNull(double? value)
        {
            if (value.HasValue)
                return value.Value.ToString("F3", CultureInfo.InvariantCulture);
            return "-";
        }

        public void PrintSummaryTable()
        {
            if (_reports.Count == 0)
            {
                Console.WriteLine("[WARN] Нет данных для вывода");
                return;
            }

            Console.WriteLine($"\n{'=',70}");
            Console.WriteLine("                    СВОДНАЯ ТАБЛИЦА РЕЗУЛЬТАТОВ");
            Console.WriteLine($"{'=',70}");
            Console.WriteLine("+--------------+----------+-------------+-------------+-----------+--------+");
            Console.WriteLine("|   Станция    | Коэфф.   |   ItogMDP   |  ItogMDP_PA |   PRED    | Статус |");
            Console.WriteLine("+--------------+----------+-------------+-------------+-----------+--------+");

            foreach (var r in _reports)
            {
                string status = GetStatusIcon(r.Status);
                string stationShort = r.StationName.Length > 12 ? r.StationName.Substring(0, 12) : r.StationName;

                Console.WriteLine($"| {stationShort,-12} |  {r.Factor,4:F1}   |   {FormatNull(r.Result.ItogMdp),8}  |" +
                                 $"   {FormatNull(r.Result.ItogMdpPa),8}  |  {FormatNull(r.Result.Pred),8} |  {status,-4}  |");
            }
            Console.WriteLine("+--------------+----------+-------------+-------------+-----------+--------+");
            Console.WriteLine($"\n[INFO] Всего расчётов: {_reports.Count}");
        }

        private string GetStatusIcon(string status)
        {
            if (status == "OK") return "✓ OK";
            if (status.StartsWith("ERR")) return "✗ ERR";
            if (status.StartsWith("FAIL")) return "⚠ FAIL";
            if (status.StartsWith("INV")) return "⚠ INV";
            if (status.StartsWith("NO")) return "⚠ NO_DATA";
            return status;
        }
    }
}