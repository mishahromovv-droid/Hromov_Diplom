using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("==========================================");
            Console.WriteLine("       СВЯЗИ ГЕНЕРАТОРОВ → УЗЛОВ");
            Console.WriteLine("==========================================\n");

            var engine = new RastrEngine();
            engine.Initialize();
            engine.LoadModeFiles(@"C:\Users\Михаил\Desktop\Diplom_Hromov\mdp_debug_0");

            Console.WriteLine("✓ RASTRA Win3 инициализирована\n");

            int totalFound = 0;
            List<string> csvLines = new List<string>();

            csvLines.Add("№;Имя генератора;Узел;Станция;Код");

            foreach (var station in CalculationSettings.Stations)
            {
                Console.WriteLine($"\n{'=',-80}");
                Console.WriteLine($"📊 СТАНЦИЯ: {station.Name} ({station.Code})");
                Console.WriteLine(new string('-', 80));

                // Получаем связи
                var connections = VoltageAnalyzer.GetConnections(engine.RawObject, station.GeneratorNums, station.Name, station.Code);

                if (connections.Count > 0)
                {
                    Console.WriteLine("\n=== НАЙДЕНО СВЯЗЕЙ ===");

                    foreach (var conn in connections)
                    {
                        // ВАШ ЗАПРОСИТЕЛЬНЫЙ ВЫВОД (без названия узла)
                        Console.WriteLine($" | Г-{conn.GenNum,-4} → Узел {conn.NodeNum,-6} | {conn.GenName,-25} | {conn.StationName}, {conn.StationCode} |");

                        // Добавляем в CSV (тоже без названия узла)
                        csvLines.Add($"{conn.GenNum};{conn.GenName.Replace(";", "")};{conn.NodeNum};{conn.StationName};{conn.StationCode}");

                        totalFound++;
                    }
                    Console.WriteLine(new string('=', 80));
                }
                else
                {
                    Console.WriteLine("\n⚠️ Нет данных для данной станции.");
                }
            }

            // Сохранение в CSV
            string csvPath = @"C:\Users\Михаил\Desktop\Diplom_Hromov\connections_clean.csv";
            Console.WriteLine($"\n>>> Сохранение в CSV: {csvPath}");
            try
            {
                using (StreamWriter sw = new StreamWriter(csvPath, false, Encoding.UTF8))
                {
                    foreach (var line in csvLines)
                    {
                        sw.WriteLine(line);
                    }
                }
                Console.WriteLine("✅ Данные сохранены!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка записи: {ex.Message}\n");
            }

            Console.WriteLine($"\n📈 ВСЕГО НАЙДЕНО СВЯЗЕЙ: {totalFound}\n");

            Console.WriteLine("Готово! Нажмите Enter для выхода...");
            Console.ReadKey();

            engine.Dispose();
        }
    }
}