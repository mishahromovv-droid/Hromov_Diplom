using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Hromov_Diplom.Models;

namespace Hromov_Diplom.Reports
{
    public class ReportManager
    {
        private readonly List<IterationReport> _reports = new();
        private readonly string _csvPath;
        private const string Header = "Factor;Pred;PadpReal;ItogMdp;ItogMdpPa;Status;Station";

        public ReportManager(string csvPath) => _csvPath = csvPath;

        public void AddResult(IterationReport report) => _reports.Add(report);

        public void WriteToFile(bool append = false)
        {
            try
            {
                if (!append)
                {
                    // ✅ Заголовок с колонкой Station
                    File.WriteAllText(_csvPath, "Factor;Pred;PadpReal;ItogMdp;ItogMdpPa;Status;Station\n", Encoding.UTF8);
                }

                foreach (var report in _reports)
                {
                    // ✅ Используем точку как разделитель (InvariantCulture)
                    string line = $"{report.StationName};" +                          // Станция
                                 $"{report.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +  // 0.2 (точка!)
                                 $"{FormatNull(report.Result.Pred)};" +              // Pred
                                 $"{FormatNull(report.Result.PadpReal)};" +          // PadpReal
                                 $"{FormatNull(report.Result.ItogMdp)};" +           // ItogMdp
                                 $"{FormatNull(report.Result.ItogMdpPa)};" +         // ItogMdpPa
                                 $"{report.Status}";                                 // ✅ Без +"\"" в конце

                    File.AppendAllText(_csvPath, line + "\n", Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Запись CSV не удалась: {ex.Message}");
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