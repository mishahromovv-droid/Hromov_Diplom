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
    /// Статический класс-экспортер данных генераторов
    /// </summary>
    public static class ReportManager
    {
        /// <summary>
        /// Экспорт данных генераторов в чистом формате CSV с расчетом разницы Qmax-Q
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

                // ✅ ОБНОВЛЕННЫЙ ЗАГОЛОВОК С НОВЫМ СТОЛБЦОМ
                writer.WriteLine("Станция;Номер генератора;Наименование;Активная мощность P (МВт);Реактивная мощность Q (Мвар);Qmin (Мвар);Qmax (Мвар);sta;Разница Q (Мвар) - Qmax (Мвар)");

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

                    // Считаем сумму Qmax для станции (по ВСЕМ генераторам без фильтра!)
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

                        // Для каждой строки ставим прочерк (разница считается только по сумме)
                        writer.WriteLine($"{stationName};{gen.GeneratorNum:D3};\"{nameEscaped}\";{pStr};{qStr};{gen.Qmin:F0};{gen.Qmax:F0};{staStr};-");
                    }

                    writer.WriteLine("--------------------------------------------");

                    // ✅ РАСЧЕТ РАЗНИЦЫ ДЛЯ СУММЫ (ПОЛОЖИТЕЛЬНОЕ ЗНАЧЕНИЕ 4745,87)
                    double diff = Math.Abs(sumQmax - Math.Abs(sumQ));

                    // Строка с итоговой суммой для станции
                    // Новые поля: ...;Qmax_sum;Diff
                    writer.WriteLine($"СУММА;{stationName};;{Math.Abs(sumP):F3};{Math.Abs(sumQ):F3};;;{sumQmax:F3};{diff:F3}");
                    writer.WriteLine();
                }

                // Общая сводка
                writer.WriteLine("================================================================================");
                writer.WriteLine("ОБЩАЯ СВОДКА ПО ВСЕМ СТАНЦИЯМ");
                writer.WriteLine("================================================================================");

                double grandTotalQmax = allGenerators.Sum(g => g.Qmax);
                double grandTotalP = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => Math.Abs(g.ActivePower));
                double grandTotalQ = allGenerators.Where(g => g.StatusFlag == 0).Sum(g => Math.Abs(g.ReactivePower));

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