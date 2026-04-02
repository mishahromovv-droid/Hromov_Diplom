using System;
using System.Globalization;
using System.IO;
using System.Text;
using Hromov_Diplom.ComService;
using Hromov_Diplom.Config;
using Hromov_Diplom.Models;
using Hromov_Diplom.Reports;

namespace Hromov_Diplom
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Run();
        }

        static void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                Console.WriteLine("==============================================================================");
                Console.WriteLine("               ЗАГРУЗКА РЕЖИМА И ВЫГРУЗКА ДАННЫХ О ГЕНЕРАТОРАХ               ");
                Console.WriteLine("==============================================================================\n");

                using var rastrEngine = new RastrEngine();

                Console.Write("[1] Инициализация RastrWin3... ");
                rastrEngine.Initialize();
                Console.WriteLine("OK\n");

                Console.Write($"[2] Загрузка режима из: {AppSettings.RegimeDirectory}... ");
                rastrEngine.LoadModeFiles(AppSettings.RegimeDirectory);
                Console.WriteLine("OK\n");

                // Выгрузка с чистым оформлением
                Console.WriteLine("[3] Создание отчета о генераторах...\n");

                rastrEngine.ExportGeneratorDataToCSV();

                Console.WriteLine("\n==============================================================================");
                Console.WriteLine("                         ЗАДАЧА ВЫПОЛНЕНА УСПЕШНО!                              ");
                Console.WriteLine("==============================================================================\n");
                Console.WriteLine("Для выхода нажмите любую клавишу...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ОШИБКА: {ex.Message}\n");
                Console.WriteLine("Проверьте путь к файлам режима в AppSettings.RegimeDirectory");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey();
            }
        }
    }
}