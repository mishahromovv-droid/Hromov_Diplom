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
    /// Статический класс-экспортер данных генераторов (без создания экземпляров new ReportManager())
    /// </summary>
    public static class ReportManager
    {
        /// <summary>
        /// Экспорт данных генераторов в чистом формате CSV (каждая колонка отдельно)
        /// Суммирование ТОЛЬКО для генераторов со ста=0
        /// Теперь сумма Qmax выводится в колонку I (одной ячейкой левее)
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

                    // ✅ НОВАЯ ФУНКЦИОНАЛЬНОСТЬ: Считаем сумму Qmax для станции (по ВСЕМ генераторам без фильтра!)
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

                    // Строка с итоговой суммой для станции
                    // ✅ ИЗМЕНЕНИЕ: Убран один лишний ";", теперь сумма Qmax будет в колонке H (под ста), а не I
                    writer.WriteLine($"СУММА;{stationName};;-{Math.Abs(sumP):F3};{Math.Abs(sumQ):F3};;{sumQmax:F3};;");
                    writer.WriteLine();
                }

                // Общая сводка
                writer.WriteLine("================================================================================");
                writer.WriteLine("ОБЩАЯ СВОДКА ПО ВСЕМ СТАНЦИЯМ");
                writer.WriteLine("================================================================================");

                // Считаем общую сумму Qmax для всех станций (все значения)
                double grandTotalQmax = allGenerators.Sum(g => g.Qmax);

                writer.WriteLine($"Общее количество обработанных генераторов: {allGenerators.Count} шт.");
                writer.WriteLine($"Количество станций: {stationsGrouped.Count} шт.");
                writer.WriteLine($"Суммарная активная мощность (P, ста=0): {allGenerators.Where(g => g.StatusFlag == 0).Sum(g => Math.Abs(g.ActivePower)):F3} МВт");
                writer.WriteLine($"Суммарная реактивная мощность (Q, ста=0): {allGenerators.Where(g => g.StatusFlag == 0).Sum(g => Math.Abs(g.ReactivePower)):F3} Мвар");
                writer.WriteLine($"Сумма столбца Qmax (ВСЕГДА): {grandTotalQmax:F3} Мвар");
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