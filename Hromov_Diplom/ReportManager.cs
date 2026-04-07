#nullable disable 

using Hromov_Diplom.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hromov_Diplom.Reports
{
    public static class StringExtensions
    {
        public static string Repeat(this string str, int count) => string.Concat(Enumerable.Repeat(str, count));
    }

    public static class ReportManager
    {
        public static void ExportCleanDataWithSums(List<GeneratorReportData> allGenerators, string filePath)
        {
            try
            {
                string outDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("Станция;Номер генератора;Наименование;Активная мощность P (МВт);Реактивная мощность Q (Мвар);Qmin (Мвар);Qmax (Мвар);sta");

                var stationsGrouped = allGenerators.GroupBy(g => g.StationName).ToList();

                foreach (var stationGroup in stationsGrouped)
                {
                    string stationName = stationGroup.Key;
                    var generators = stationGroup.ToList();

                    writer.WriteLine("");

                    double sumP = 0;
                    double sumQ = 0;
                    double sumQmax = 0;

                    foreach (var gen in generators)
                    {
                        // ✅ ИСПРАВЛЕНО: Все три параметра считаются ТОЛЬКО для генераторов в работе (sta=0)
                        if (gen.StatusFlag == 0)
                        {
                            sumP += gen.ActivePower;
                            sumQ += gen.ReactivePower;
                            sumQmax += gen.Qmax;
                        }
                    }

                    writer.WriteLine($">>> СТАНЦИЯ: {stationName}");
                    writer.WriteLine($"Всего генераторов: {generators.Count} шт.");
                    int activeGenCount = generators.Count(g => g.StatusFlag != 0 || g.ActivePower != 0 || g.ReactivePower != 0);
                    writer.WriteLine($"Активные генераторы: {activeGenCount} шт.");
                    writer.WriteLine();

                    foreach (var gen in generators.OrderBy(g => g.GeneratorNum))
                    {
                        string pStr = gen.ActivePower != 0 ? gen.ActivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string qStr = gen.ReactivePower != 0 ? gen.ReactivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string staStr = gen.StatusFlag == 1 ? "1" : "0";
                        string nameEscaped = EscapeCsv(gen.Name);

                        writer.WriteLine($"{stationName};{gen.GeneratorNum:D3};\"{nameEscaped}\";{pStr};{qStr};{gen.Qmin:F0};{gen.Qmax:F0};{staStr}");
                    }

                    writer.WriteLine("--------------------------------------------");

                    double diff = sumQmax - sumQ;
                    writer.WriteLine($"СУММА;{stationName};;{sumP:F3};{sumQ:F3};;;{sumQmax:F3};{diff:F3}");
                    writer.WriteLine();
                }

                // Общая сводка
                writer.WriteLine("================================================================================");
                writer.WriteLine("ОБЩАЯ СВОДКА ПО ВСЕМ СТАНЦИЯМ");
                writer.WriteLine("================================================================================");

                double grandTotalQmax = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.Qmax);
                double grandTotalP = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.ActivePower);
                double grandTotalQ = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.ReactivePower);

                writer.WriteLine($"Общее количество обработанных генераторов: {allGenerators.Count} шт.");
                writer.WriteLine($"Количество станций: {stationsGrouped.Count} шт.");
                writer.WriteLine($"Суммарная активная мощность (P, ста=0): {grandTotalP:F3} МВт");
                writer.WriteLine($"Суммарная реактивная мощность (Q, ста=0): {grandTotalQ:F3} Мвар");
                writer.WriteLine($"Суммарный предел Qmax (ста=0): {grandTotalQmax:F3} Мвар");
                writer.WriteLine("=== КОНЕЦ ФАЙЛА ===");

                Console.WriteLine($"✅ Чистые данные успешно сохранены в: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка экспорта: {ex.Message}");
            }
        }

        // ✅ НОВЫЙ МЕТОД: Компактная сводная таблица sumQ / sumQmax / diff
        public static void ExportStationQSummary(List<GeneratorReportData> allGenerators, string outputCsvPath)
        {
            try
            {
                var stationsGrouped = allGenerators.GroupBy(g => g.StationName).OrderBy(g => g.Key).ToList();

                Console.WriteLine("\n📊 СВОДНАЯ ТАБЛИЦА ПО РЕАКТИВНОЙ МОЩНОСТИ (ТОЛЬКО StatusFlag == 0)");
                Console.WriteLine(new string('-', 100));
                Console.WriteLine("{0,-50} | {1,10} | {2,10} | {3,10}", "Станция", "sumQ", "sumQmax", "diff");
                Console.WriteLine(new string('-', 100));

                using var writer = new StreamWriter(outputCsvPath, false, Encoding.UTF8);
                writer.WriteLine("Станция;sumQ (Мвар);sumQmax (Мвар);diff (Мвар)");

                foreach (var stationGroup in stationsGrouped)
                {
                    double sumQ = 0;
                    double sumQmax = 0;

                    foreach (var gen in stationGroup.Where(g => g.StatusFlag == 0))
                    {
                        sumQ += gen.ReactivePower;
                        sumQmax += gen.Qmax;
                    }

                    double diff = sumQmax - sumQ;

                    Console.WriteLine("{0,-50} | {1,10:F3} | {2,10:F3} | {3,10:F3}", stationGroup.Key, sumQ, sumQmax, diff);
                    writer.WriteLine($"{stationGroup.Key};{sumQ:F3};{sumQmax:F3};{diff:F3}");
                }

                Console.WriteLine($"\n✅ Сводная таблица сохранена: {outputCsvPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка создания сводной таблицы: {ex.Message}");
            }
        }

        private static string EscapeCsv(string text) => '"' + text.Replace("\"", "\"\"") + '"';
    }
}