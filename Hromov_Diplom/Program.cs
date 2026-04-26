using System;
using System.IO;
using System.Text;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("🚀 СТАРТ ВЫГРУЗКИ ДАННЫХ (Q / Qmax)");
            Console.WriteLine($"📂 Базовая папка: {AppSettings.BaseRoot}\n");

            var regimes = new[]
            {
                ("mdp_debug_1", "Режим"),  // ← ИЗМЕНЕНО: было "Коэффициенты влияния"
                ("mdp_debug_1_Утяжеленный", "Утяжеленный"),
                ("mdp_debug_1_Утяжеленный_до_МДП", "Утяжеленный_до_МДП"),
                ("mdp_debug_1_Утяжеленный_с_Vzd", "Утяжеленный_с_Vzd(Станции)"),
                ("mdp_debug_1_Утяжеленный_с_Vzd_до_МДП", "Утяжеленный_с_Vzd(Станции)_до_МДП")
            };

            foreach (var (regimeName, outputFolder) in regimes)
            {
                Console.WriteLine($"\n==========================================");
                Console.WriteLine($"   РЕЖИМ: {regimeName}");
                Console.WriteLine($"   ПАПКА: {outputFolder}");
                Console.WriteLine("==========================================");

                var regimePath = Path.Combine(AppSettings.BaseRoot, regimeName);
                var outputPath = Path.Combine(AppSettings.OutputRoot, outputFolder);

                if (!Directory.Exists(regimePath) && !File.Exists(regimePath))
                {
                    Console.WriteLine($"⚠ Не найден: {regimePath}\n⏭ Пропуск...");
                    continue;
                }
                if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

                using var engine = new RastrEngine();
                try
                {
                    Console.WriteLine("[1] Инициализация..."); engine.Initialize(); Console.WriteLine("    ✅\n");
                    Console.WriteLine("[2] Загрузка..."); engine.LoadModeFiles(regimePath); Console.WriteLine("    ✅\n");
                    Console.WriteLine("[3] Экспорт...");
                    var gens = engine.ExportGeneratorDataToList();
                    Console.WriteLine($"    📊 Найдено: {gens.Count}\n");

                    ReportManager.ExportCleanDataWithSums(gens, Path.Combine(outputPath, "Raschyot.csv"));
                    ReportManager.ExportStationQSummary(gens, Path.Combine(outputPath, "Ytog.csv"));
                    Console.WriteLine($"✅ {regimeName} завершён!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ ОШИБКА {regimeName}: {ex.Message}");
                }

                Console.WriteLine("\n⏸️ Enter для следующего...");
                Console.ReadLine();
            }

            Console.WriteLine($"\n✅ ГОТОВО! Результаты: {AppSettings.OutputRoot}");
            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}
