using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hromov_Diplom
{
    public static class InfluenceCoefficientCalculator
    {
        /// <summary>
        /// Структура для хранения результата расчёта k_B между двумя итерациями
        /// </summary>
        public struct KBResult
        {
            public string StationName;
            public double FactorStart;
            public double FactorEnd;
            public double DeltaQ;
            public double DeltaP;
            public double KB;
        }

        /// <summary>
        /// Расчитывает коэффициент влияния k_B по всем данным за станции
        /// </summary>
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

        /// <summary>
        /// Вывод таблицы расчёта k_B в консоль
        /// </summary>
        public static void PrintKBTable(List<KBResult> kbResults)
        {
            Console.WriteLine("\n==========================================");
            Console.WriteLine("          КОЭФФИЦИЕНТ ВЛИЯНИЯ k_B");
            Console.WriteLine("==========================================");
            Console.WriteLine("| Станция              | Факторы   | ΔQ(Мвар) | ΔP(МВт) | k_B(МВт/Мвар) |");
            Console.WriteLine("+----------------------+-----------+----------+---------+---------------+");

            foreach (var res in kbResults)
            {
                string stationName = res.StationName.Length > 19 ? res.StationName.Substring(0, 19) : res.StationName;
                Console.WriteLine($"| {stationName,-20} | {res.FactorStart:F1}-{res.FactorEnd:F1} | {res.DeltaQ:F8},{res.DeltaP:F8},{res.KB:F5} |");
            }

            Console.WriteLine(new string('=', 75));
        }

        /// <summary>
        /// Сохраняет расчёт k_B в CSV-файл после основных результатов
        /// </summary>
        public static void WriteKBToCsv(string csvPath, List<KBResult> kbResults)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(csvPath))
                {
                    // Добавляем пустую строку для отступа от основного блока
                    sw.WriteLine();
                    sw.WriteLine("// --- РАСЧЁТ КОЭФФИЦИЕНТА ВЛИЯНИЯ k_B ---");

                    // Заголовок столбцов с разделителями точка с запятой
                    sw.WriteLine("Station;FactorStart;FactorEnd;DeltaQ;DeltaP;KB");

                    foreach (var res in kbResults)
                    {
                        // === ИСПРАВЛЕНИЕ: ВСЕ ДЕЛЯТСЯ ТОЧКОЙ С ЗАПЯТОЙ ===
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