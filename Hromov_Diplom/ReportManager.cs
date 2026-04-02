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

    /// <summary>
    /// Статический класс-экспортер данных генераторов
    /// </summary>
    public static class ReportManager
    {
        /// <summary>
        /// Экспорт данных генераторов в чистом формате CSV (каждая колонка отдельно)
        /// Суммирование ТОЛЬКО для генераторов со ста=0
        /// </summary>
        public static void ExportCleanDataWithSums(List<GeneratorReportData> allGenerators, string filePath)
        {
            try
            {
                // Создаем папку если она не существует
                string outDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

                // Заголовок файла
                writer.WriteLine("Станция;Номер генератора;Наименование;Активная мощность P (МВт);Реактивная мощность Q (Мвар);Qmin (Мвар);Qmax (Мвар);sta");

                // Группируем данные по станциям для удобства отображения
                var stationsGrouped = allGenerators.GroupBy(g => g.StationName).ToList();

                foreach (var stationGroup in stationsGrouped)
                {
                    string stationName = stationGroup.Key;
                    var generators = stationGroup.ToList();

                    // Пустые строки между станциями
                    writer.WriteLine("");

                    // Расчет сумм только по ста=0
                    double sumP = 0;
                    double sumQ = 0;

                    foreach (var gen in generators)
                    {
                        // Фильтр: учитываем только генераторы со статусом 0
                        if (gen.StatusFlag == 0)
                        {
                            sumP += gen.ActivePower;
                            sumQ += gen.ReactivePower;
                        }
                    }

                    writer.WriteLine($">>> СТАНЦИЯ: {stationName}");
                    writer.WriteLine($"Всего генераторов: {generators.Count} шт.");

                    // Данные по каждому генератору
                    foreach (var gen in generators.OrderBy(g => g.GeneratorNum))
                    {
                        string pStr = gen.ActivePower != 0 ? gen.ActivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string qStr = gen.ReactivePower != 0 ? gen.ReactivePower.ToString("F3", CultureInfo.InvariantCulture) : "-";
                        string staStr = gen.StatusFlag == 1 ? "1" : "0";
                        string nameEscaped = EscapeCsv(gen.Name);

                        writer.WriteLine($"{stationName};{gen.GeneratorNum:D3};\"{nameEscaped}\";{pStr};{qStr};{gen.Qmin:F0};{gen.Qmax:F0};{staStr}");
                    }

                    // Строка с итоговой суммой для станции
                    writer.WriteLine("--------------------------------------------");
                    writer.WriteLine($"СУММА;{stationName};;{sumP.ToString("F3", CultureInfo.InvariantCulture)};{Math.Abs(sumQ).ToString("F3", CultureInfo.InvariantCulture)};;;");

                    writer.WriteLine();
                }

                // Общая сводка
                writer.WriteLine("================================================================================");
                writer.WriteLine("ОБЩАЯ СВОДКА ПО ВСЕМ СТАНЦИЯМ");
                writer.WriteLine("================================================================================");

                // Учитываем только генераторы со статусом 0 и для общей сводки
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