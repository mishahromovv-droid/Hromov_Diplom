// ⚠️ ВАЖНО: Добавлено DISABLE для аннотаций nullable
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
        public static string Repeat(this string str, int count)
        {
            return string.Concat(Enumerable.Repeat(str, count));
        }
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

                // Заголовок файла
                writer.WriteLine("Станция;Номер генератора;Наименование;Активная мощность P (МВт);Реактивная мощность Q (Мвар);Qmin (Мвар);Qmax (Мвар);sta");

                // Группируем данные по станциям
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
                        // Фильтр: учитываем только генераторы со статусом 0 для P и Q
                        if (gen.StatusFlag == 0)
                        {
                            sumP += gen.ActivePower;
                            sumQ += gen.ReactivePower;
                        }

                        // Но Qmax считаем ПО ВСЕМ генераторам БЕЗ ФИЛЬТРОВ!
                        sumQmax += gen.Qmax;
                    }

                    writer.WriteLine($">>> СТАНЦИЯ: {stationName}");
                    writer.WriteLine($"Всего генераторов: {generators.Count} шт.");

                    int activeGenCount = generators.Count(g => g.StatusFlag != 0 || g.ActivePower != 0 || g.ReactivePower != 0);

                    writer.WriteLine($"Активные генераторы: {activeGenCount} шт.");
                    writer.WriteLine();

                    // Данные по каждому генератору
                    foreach (var gen in generators.OrderBy(g => g.GeneratorNum))
                    {
                        string pStr = gen.ActivePower != 0 ? gen.ActivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string qStr = gen.ReactivePower != 0 ? gen.ReactivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string staStr = gen.StatusFlag == 1 ? "1" : "0";
                        string nameEscaped = EscapeCsv(gen.Name);

                        writer.WriteLine($"{stationName};{gen.GeneratorNum:D3};\"{nameEscaped}\";{pStr};{qStr};{gen.Qmin:F0};{gen.Qmax:F0};{staStr}");
                    }

                    writer.WriteLine("--------------------------------------------");

                    // ✅ РАЗНИЦА БЕЗ ЛЮБЫХ МОДУЛЕЙ!
                    double diff = sumQmax - sumQ;

                    // Строка с итоговой суммой для станции
                    writer.WriteLine($"СУММА;{stationName};;{sumP:F3};{sumQ:F3};;;{sumQmax:F3};{diff:F3}");
                    writer.WriteLine();
                }

                // Общая сводка
                writer.WriteLine("================================================================================");
                writer.WriteLine("ОБЩАЯ СВОДКА ПО ВСЕМ СТАНЦИЯМ");
                writer.WriteLine("================================================================================");

                double grandTotalQmax = allGenerators.Sum(g => g.Qmax);
                double grandTotalP = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.ActivePower);
                double grandTotalQ = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => g.ReactivePower);

                writer.WriteLine($"Общее количество обработанных генераторов: {allGenerators.Count} шт.");
                writer.WriteLine($"Количество станций: {stationsGrouped.Count} шт.");
                writer.WriteLine($"Суммарная активная мощность (P, ста=0): {grandTotalP:F3} МВт");
                writer.WriteLine($"Суммарная реактивная мощность (Q, ста=0): {grandTotalQ:F3} Мвар");
                writer.WriteLine("=== КОНЕЦ ФАЙЛА ===");

                Console.WriteLine($"✅ Чистые данные успешно сохранены в: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка экспорта: {ex.Message}");
            }
        }

        private static string EscapeCsv(string text)
        {
            return '"' + text.Replace("\"", "\"\"") + '"';
        }
    }
}