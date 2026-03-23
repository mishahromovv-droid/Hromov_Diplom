using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hromov_Diplom
{
    public class ReportManager
    {
        private readonly List<IterationReport> _reports = new();
        private string _csvPath;

        public ReportManager(string csvPath)
        {
            _csvPath = csvPath;
            try
            {
                if (File.Exists(_csvPath)) File.Delete(_csvPath);
            }
            catch { /* Ошибки игнорируем */ }
        }

        public string GetFinalCsvPath()
        {
            return _csvPath;
        }

        public void AddResult(IterationReport report) => _reports.Add(report);

        public List<IterationReport> GetReports()
        {
            return _reports;
        }

        public void ComputeReserves(double requiredReservePct)
        {
            foreach (var r in _reports)
            {
                if (r.Result.HasCore)
                {
                    double prab = r.Result.PrabSech17;
                    double pPred = r.Result.ItogMdp;
                    if (prab <= 0 || pPred <= 0) continue;

                    double zeta = (pPred - prab) / pPred * 100.0;
                    r.Result.ReservePct = zeta;
                    r.Result.MeetsRequiredReserve = zeta >= requiredReservePct;
                }
            }
        }

        public void MarkMinReactivePicks()
        {
            foreach (var r in _reports)
                r.Result.IsMinReactivePick = false;

            foreach (var g in _reports.GroupBy(x => x.StationName))
            {
                var ok = g.Where(x => x.Status == "OK" && x.Result.MeetsRequiredReserve).ToList();
                if (ok.Count == 0) continue;

                var best = ok.OrderBy(x => x.Result.QrangeTotal).First();
                best.Result.IsMinReactivePick = true;
            }
        }

        public void PrintSummaryTable(double requiredReservePct)
        {
            ComputeReserves(requiredReservePct);
            MarkMinReactivePicks();

            Console.WriteLine(new string('=', 110));
            Console.WriteLine("                                   СВОДНАЯ ТАБЛИЦА РЕЗУЛЬТАТОВ");
            Console.WriteLine(new string('=', 110));
            Console.WriteLine("+--------------+--------+------------+------------+------------+------------+----------+--------+-----+");
            Console.WriteLine("|   Станция    | Factor |  Prab(17)  |  ItogMDP   |  QmaxSum   |  Qrange    | ζ,%     | ReqOK  | MIN |");
            Console.WriteLine("+--------------+--------+------------+------------+------------+------------+----------+--------+-----+");

            foreach (var r in _reports)
            {
                string stationShort = r.StationName.Length > 12 ? r.StationName.Substring(0, 12) : r.StationName;
                Console.WriteLine(
                    $"| {stationShort,-12} | {r.Factor,6:F1} | {Fmt(r.Result.PrabSech17),10} | {Fmt(r.Result.ItogMdp),10} |" +
                    $" {Fmt(r.Result.QmaxTotal),10} | {Fmt(r.Result.QrangeTotal),10} | {Fmt(r.Result.ReservePct),8} |" +
                    $" {FmtBool(r.Result.MeetsRequiredReserve),6} | {FmtBoolShort(r.Result.IsMinReactivePick),3} |"
                );
            }

            Console.WriteLine("+--------------+--------+------------+------------+------------+------------+----------+--------+-----+");
            Console.WriteLine($"[INFO] ζтр = {requiredReservePct:F1}%   | Всего расчётов: {_reports.Count}");
        }

        public void WriteToFile(double requiredReservePct)
        {
            ComputeReserves(requiredReservePct);
            MarkMinReactivePicks();

            try
            {
                File.WriteAllText(
                    _csvPath,
                    "Station;Factor;Prab_Sech17;QminTotal;QmaxTotal;QrangeTotal;Pred;PadpReal;ItogMdp;ItogMdpPa;ReservePct;KB_Group;MeetsReq;MinReactivePick;Status\n",
                    Encoding.UTF8
                );

                foreach (var r in _reports)
                {
                    string line =
                        $"{r.StationName};" +
                        $"{r.Factor.ToString("F1", CultureInfo.InvariantCulture)};" +
                        $"{FmtCsv(r.Result.PrabSech17)};" +
                        $"{FmtCsv(r.Result.QminTotal)};" +
                        $"{FmtCsv(r.Result.QmaxTotal)};" +
                        $"{FmtCsv(r.Result.QrangeTotal)};" +
                        $"{FmtCsv(r.Result.Pred)};" +
                        $"{FmtCsv(r.Result.PadpReal)};" +
                        $"{FmtCsv(r.Result.ItogMdp)};" +
                        $"{FmtCsv(r.Result.ItogMdpPa)};" +
                        $"{FmtCsv(r.Result.ReservePct)};" +
                        $"{FmtCsv(r.Result.KB_Group)};" +
                        $"{FmtBoolCsv(r.Result.MeetsRequiredReserve)};" +
                        $"{FmtBoolCsv(r.Result.IsMinReactivePick)};" +
                        $"{r.Status}";

                    File.AppendAllText(_csvPath, line + "\n", Encoding.UTF8);
                }

                Console.WriteLine("[OK] CSV сохранён: " + _csvPath);
            }
            catch (IOException ex)
            {
                string dir = Path.GetDirectoryName(_csvPath) ?? ".";
                string name = Path.GetFileNameWithoutExtension(_csvPath);
                string ext = Path.GetExtension(_csvPath);
                string alt = Path.Combine(dir, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");

                Console.WriteLine($"[WARN] Файл занят другим процессом! Пересохраняю в: {alt}");
                Console.WriteLine($"[WARN] Причина: {ex.Message}");

                _csvPath = alt;
                WriteToFile(requiredReservePct);
            }
        }

        private static string Fmt(double v) => v.ToString("F3").Replace(",", ".").Replace(".", ",");
        private static string FmtCsv(double v) => v.ToString("F3").Replace(",", ".").Replace(".", ",");

        private static string FmtBool(bool v) => v ? "YES" : "NO";
        private static string FmtBoolShort(bool v) => v ? "Y" : "-";
        private static string FmtBoolCsv(bool v) => v ? "YES" : "NO";
    }
}