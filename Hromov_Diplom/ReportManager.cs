using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hromov_Diplom
{
    public static class ReportManager
    {
        /// <summary>
        /// Извлекает название станции из имени генератора
        /// Пример: "Абаканская ТЭЦ, 1Г" → "Абаканская ТЭЦ"
        /// </summary>
        private static string ExtractStationName(string generatorName)
        {
            if (string.IsNullOrWhiteSpace(generatorName))
                return "Неизвестно";

            // Убираем ВСЕ кавычки (и одинарные, и двойные)
            generatorName = generatorName.Replace("\"", "").Replace("'", "").Trim();

            // Ищем запятую — всё до неё это название станции
            int commaIndex = generatorName.IndexOf(',');
            if (commaIndex > 0)
                return generatorName.Substring(0, commaIndex).Trim();

            // Если запятой нет — возвращаем как есть (может быть короткое имя)
            return generatorName;
        }

        public static void ExportCleanDataWithSums(List<GeneratorReportData> allGenerators, string filePath)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
                writer.WriteLine("Станция;Номер генератора;Наименование;Активная мощность P (МВт);Реактивная мощность Q (Мвар);Qmin (Мвар);Qmax (Мвар);sta");

                // 1. Извлекаем названия станций и группируем
                var grouped = allGenerators
                    .Select(g => new { Data = g, Station = ExtractStationName(g.Name) })
                    .GroupBy(x => x.Station)
                    .OrderBy(g => g.Key);

                foreach (var group in grouped)
                {
                    var gens = group.Select(x => x.Data).ToList();

                    writer.WriteLine("");
                    writer.WriteLine($">>> СТАНЦИЯ: {group.Key}");
                    writer.WriteLine($"Всего генераторов: {gens.Count} шт.");

                    double sumP = 0, sumQ = 0, sumQmax = 0;
                    foreach (var gen in gens.Where(g => g.StatusFlag == 0))
                    {
                        sumP += gen.ActivePower;
                        sumQ += gen.ReactivePower;
                        sumQmax += gen.Qmax;
                    }

                    foreach (var gen in gens.OrderBy(g => g.GeneratorNum))
                    {
                        string pStr = gen.ActivePower != 0 ? gen.ActivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string qStr = gen.ReactivePower != 0 ? gen.ReactivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        writer.WriteLine($"{group.Key};{gen.GeneratorNum:D3};\"{EscapeCsv(gen.Name)}\";{pStr};{qStr};{gen.Qmin:F0};{gen.Qmax:F0};{gen.StatusFlag}");
                    }

                    writer.WriteLine("--------------------------------------------");
                    double diff = sumQmax - sumQ;
                    writer.WriteLine($"СУММА;{group.Key};;{sumP:F3};{sumQ:F3};;;{sumQmax:F3};{diff:F3}\n");
                }

                // Общие итоги
                var active = allGenerators.Where(g => g.StatusFlag == 0).ToList();
                writer.WriteLine("================================================================================");
                writer.WriteLine($"Общее количество обработанных генераторов: {allGenerators.Count} шт.");
                writer.WriteLine($"Суммарная активная мощность (P, ста=0): {active.Sum(g => g.ActivePower):F3} МВт");
                writer.WriteLine($"Суммарная реактивная мощность (Q, ста=0): {active.Sum(g => g.ReactivePower):F3} Мвар");
                writer.WriteLine($"Суммарный предел Qmax (ста=0): {active.Sum(g => g.Qmax):F3} Мвар");
                writer.WriteLine("=== КОНЕЦ ФАЙЛА ===");
            }
            catch (Exception ex) { Console.WriteLine($"❌ Ошибка Raschyot.csv: {ex.Message}"); }
        }

        public static void ExportStationQSummary(List<GeneratorReportData> allGenerators, string outputCsvPath)
        {
            try
            {
                using var writer = new StreamWriter(outputCsvPath, false, Encoding.UTF8);
                writer.WriteLine("Станция;sumQ (Мвар);sumQmax (Мвар);diff (Мвар)");

                var grouped = allGenerators
                    .Select(g => new { Data = g, Station = ExtractStationName(g.Name) })
                    .GroupBy(x => x.Station)
                    .OrderBy(g => g.Key);

                foreach (var group in grouped)
                {
                    double sumQ = 0, sumQmax = 0;
                    foreach (var gen in group.Select(x => x.Data).Where(g => g.StatusFlag == 0))
                    {
                        sumQ += gen.ReactivePower;
                        sumQmax += gen.Qmax;
                    }
                    writer.WriteLine($"{group.Key};{sumQ:F3};{sumQmax:F3};{sumQmax - sumQ:F3}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"❌ Ошибка Ytog.csv: {ex.Message}"); }
        }

        private static string EscapeCsv(string text) => '"' + text.Replace("\"", "\"\"") + '"';
    }
}