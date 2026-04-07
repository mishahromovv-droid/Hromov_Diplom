using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Hromov_Diplom
{
    public static class InfluenceCoefficientCalculator
    {
        public struct KBResult
        {
            public string StationName;
            public double FactorStart;
            public double FactorEnd;
            public double DeltaQ;
            public double DeltaP;
            public double KB;
        }

        public class StationGroupKB
        {
            public string StationName { get; set; } = "";
            public int M { get; set; }
            public int IntervalsCount { get; set; }
            public double GroupKB { get; set; }
            public List<KBResult> IndividualResults { get; set; } = new List<KBResult>();
        }

        public static List<KBResult> CalculateKBForAllStations(List<IterationReport> reports)
        {
            var kbResults = new List<KBResult>();
            var stationsGroups = reports.GroupBy(r => r.StationName);

            foreach (var stationGroup in stationsGroups)
            {
                var sortedReports = stationGroup
                    .Where(r => r.Status == "OK" && r.Result.QrangeTotal != 0)
                    .OrderBy(r => r.Factor)
                    .ToList();

                if (sortedReports.Count < 2) continue;

                for (int i = 0; i < sortedReports.Count - 1; i++)
                {
                    var current = sortedReports[i];
                    var next = sortedReports[i + 1];

                    double deltaQ = next.Result.QrangeTotal - current.Result.QrangeTotal;
                    double deltaP = next.Result.ItogMdp - current.Result.ItogMdp;

                    double kB = 0.0;
                    if (Math.Abs(deltaQ) > 1e-6)
                        kB = deltaP / deltaQ;

                    kbResults.Add(new KBResult
                    {
                        StationName = current.StationName,
                        FactorStart = current.Factor,
                        FactorEnd = next.Factor,
                        DeltaQ = deltaQ,
                        DeltaP = deltaP,
                        KB = kB
                    });
                }
            }

            return kbResults;
        }

        public static List<StationGroupKB> CalculateGroupKBForAllStations(List<IterationReport> reports)
        {
            var groupResults = new List<StationGroupKB>();
            var stationsGroups = reports.GroupBy(r => r.StationName);

            foreach (var stationGroup in stationsGroups)
            {
                var sortedReports = stationGroup
                    .Where(r => r.Status == "OK" && r.Result.QrangeTotal != 0)
                    .OrderBy(r => r.Factor)
                    .ToList();

                int m = sortedReports.Count;
                if (m < 2) continue;

                int intervalsCount = m - 1;
                double sumIndividualKB = 0.0;
                var individualList = new List<KBResult>();
                int validIntervals = 0;

                for (int i = 0; i < intervalsCount; i++)
                {
                    var current = sortedReports[i];
                    var next = sortedReports[i + 1];

                    double deltaQ = next.Result.QrangeTotal - current.Result.QrangeTotal;
                    double deltaP = next.Result.ItogMdp - current.Result.ItogMdp;

                    double kBi = 0.0;
                    if (Math.Abs(deltaQ) > 1e-6)
                    {
                        kBi = deltaP / deltaQ;
                        validIntervals++;
                    }

                    individualList.Add(new KBResult
                    {
                        StationName = stationGroup.Key,
                        FactorStart = current.Factor,
                        FactorEnd = next.Factor,
                        DeltaQ = deltaQ,
                        DeltaP = deltaP,
                        KB = kBi
                    });

                    sumIndividualKB += kBi;
                }

                double groupKB = validIntervals > 0 ? sumIndividualKB / validIntervals : 0.0;

                groupResults.Add(new StationGroupKB
                {
                    StationName = stationGroup.Key,
                    M = m,
                    IntervalsCount = intervalsCount,
                    GroupKB = groupKB,
                    IndividualResults = individualList
                });
            }

            return groupResults;
        }

        public static void WriteKBToCsv(string csvPath, List<KBResult> kbResults)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(csvPath))
                {
                    sw.WriteLine();
                    sw.WriteLine("// --- РАСЧЁТ КОЭФФИЦИЕНТА ВЛИЯНИЯ k_B ---");
                    sw.WriteLine("Station;FactorStart;FactorEnd;DeltaQ;DeltaP;KB");

                    foreach (var res in kbResults)
                    {
                        string line = $"{res.StationName};{res.FactorStart.ToString("F1", CultureInfo.InvariantCulture)};{res.FactorEnd.ToString("F1", CultureInfo.InvariantCulture)};{res.DeltaQ.ToString("F3", CultureInfo.InvariantCulture)};{res.DeltaP.ToString("F3", CultureInfo.InvariantCulture)};{res.KB.ToString("F4", CultureInfo.InvariantCulture)}";
                        sw.WriteLine(line);
                    }

                    sw.WriteLine("// --- КОНЕЦ РАСЧЁТА k_B ---");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] Не удалось записать k_B в CSV: " + ex.Message);
            }
        }

        public static void WriteGroupKBToCsv(string csvPath, List<StationGroupKB> groupResults)
        {
            try
            {
                string groupFilePath = Path.ChangeExtension(csvPath, "_GROUP_KB.csv");

                using (StreamWriter sw = new StreamWriter(groupFilePath, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("Station;M(Points);Intervals(M-1);Group_KB_Average");

                    foreach (var res in groupResults.OrderBy(x => x.StationName))
                    {
                        string line = $"{res.StationName};{res.M};{res.IntervalsCount};{res.GroupKB.ToString("F6", CultureInfo.InvariantCulture)}";
                        sw.WriteLine(line);
                    }
                }

                Console.WriteLine($"✅ Групповые коэффициенты сохранены в: {groupFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Ошибка записи групповых K_B: {ex.Message}");
            }
        }
    }
}