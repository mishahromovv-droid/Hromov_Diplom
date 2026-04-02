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
    /// <summary>
    /// Статический класс для экспорта данных генераторов
    /// НЕЛЬЗЯ создавать экземпляры этого класса!
    /// </summary>
    public static class ReportManager
    {
        /// <summary>
        /// Экспорт данных генераторов с алгебраическими суммами
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
                writer.WriteLine("Станция;Номер генератора;Наименование;Активная мощность P (МВт);Реактивная мощность Q (Мвар);Qmin (Мвар);Qmax (Мвар);STA");

                // Группируем данные по станциям
                var stationsGrouped = allGenerators.GroupBy(g => g.StationName).ToList();

                foreach (var stationGroup in stationsGrouped)
                {
                    string stationName = stationGroup.Key;
                    var generators = stationGroup.ToList();

                    // Пустые строки для разделения станций
                    writer.WriteLine("");

                    // Суммы по станции (алгебраические, не модули!)
                    double sumP = generators.Sum(g => g.ActivePower);
                    double sumQ = generators.Sum(g => g.ReactivePower);

                    // Вывод данных каждого генератора
                    foreach (var gen in generators.OrderBy(g => g.GeneratorNum))
                    {
                        string pStr = gen.ActivePower.ToString("F3", CultureInfo.InvariantCulture);
                        string qStr = gen.ReactivePower.ToString("F3", CultureInfo.InvariantCulture);
                        string staStr = gen.StatusFlag == 1 ? "1" : "0";
                        string nameEscaped = EscapeCsv(gen.Name);

                        writer.WriteLine($"{stationName};{gen.GeneratorNum:D3};\"{nameEscaped}\";{pStr};{qStr};{gen.Qmin:F0};{gen.Qmax:F0};{staStr}");
                    }

                    // Строка с итоговой суммой по станции
                    writer.WriteLine();
                    writer.WriteLine($"СУММА;{stationName};;-;{sumP:F3};{sumQ:F3};;");
                    writer.WriteLine();
                }

                // Общая сводка
                writer.WriteLine("================================================================================");
                writer.WriteLine("ОБЩАЯ СВОДКА ПО ВСЕМ СТАНЦИЯМ");
                writer.WriteLine("================================================================================");

                double grandTotalP = allGenerators.Sum(g => g.ActivePower);
                double grandTotalQ = allGenerators.Sum(g => g.ReactivePower);

                writer.WriteLine($"Общее кол-во генераторов: {allGenerators.Count} шт.");
                writer.WriteLine($"Количество станций: {stationsGrouped.Count} шт.");
                writer.WriteLine($"Суммарная активная мощность (P): {grandTotalP:F3} МВт");
                writer.WriteLine($"Суммарная реактивная мощность (Q): {grandTotalQ:F3} Мвар");

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