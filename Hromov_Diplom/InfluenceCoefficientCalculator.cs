using System;
using System.Collections.Generic;
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

        public static List<KBResult> CalculateKBForAllStations(List<IterationReport> reports)
        {
            var kbResults = new List<KBResult>();
            var stationsGroups = reports.GroupBy(r => r.StationName);

            foreach (var stationGroup in stationsGroups)
            {
                var sortedReports = stationGroup.OrderBy(r => r.Factor).ToList();

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

                    KBResult result = new KBResult
                    {
                        StationName = current.StationName,
                        FactorStart = current.Factor,
                        FactorEnd = next.Factor,
                        DeltaQ = deltaQ,
                        DeltaP = deltaP,
                        KB = kB
                    };

                    kbResults.Add(result);
                }
            }

            return kbResults;
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
                        string line = $"{res.StationName};{res.FactorStart:F1};{res.FactorEnd:F1};{res.DeltaQ:F3};{res.DeltaP:F3};{res.KB:F4}";
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
    }
}