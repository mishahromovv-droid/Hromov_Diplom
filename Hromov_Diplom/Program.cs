using System;
using System.Text;
using Hromov_Diplom.ComService;
using Hromov_Diplom.Config;
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
            Console.InputEncoding = Encoding.UTF8;

            try
            {
                Console.WriteLine("==============================================================================");
                Console.WriteLine("     ЗАГРУЗКА РЕЖИМА → ЗАНУЛЕНИЕ ВИР → РАСЧЁТ МДП → ЭКСПОРТ ДАННЫХ          ");
                Console.WriteLine("==============================================================================\n");

                using var rastrEngine = new RastrEngine();

                Console.Write("[1] Инициализация RastrWin3... ");
                rastrEngine.Initialize();
                Console.WriteLine("OK");

                Console.Write("[2] Загрузка режима... ");
                rastrEngine.LoadModeFiles(AppSettings.RegimeDirectory);
                Console.WriteLine("OK");

                Console.Write("[3] Зануление ВИР (кроме №54)... ");
                rastrEngine.ZeroOutVirtualRegulators(excludeVirNum: 54);
                Console.WriteLine("OK");

                Console.Write("[4] Расчёт МДП... ");
                rastrEngine.CalculateMDP();
                Console.WriteLine("OK");

                Console.Write("[5] Экспорт данных генераторов... ");
                rastrEngine.ExportGeneratorDataToCSV();
                Console.WriteLine("OK");

                Console.WriteLine("\n==============================================================================");
                Console.WriteLine("                         ЗАДАЧА ВЫПОЛНЕНА УСПЕШНО!                              ");
                Console.WriteLine("==============================================================================\n");
                Console.WriteLine("Для выхода нажмите любую клавишу...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ОШИБКА: {ex.Message}\n");
                Console.WriteLine("Для выхода нажмите любую клавишу...");
                Console.ReadKey();
            }
        }
    }
}