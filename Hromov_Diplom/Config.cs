using System;
using System.IO;

namespace Hromov_Diplom.Config
{
    /// <summary>
    /// Конфигурация одной станции для выгрузки данных о реактивной мощности
    /// </summary>
    public class RegulatorConfig
    {
        /// <summary>
        /// Название станции (должно точно совпадать с выводом RastrWin3)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Номера генераторов этой станции
        /// </summary>
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
    }

    public static class CalculationSettings
    {
        // ✅ СПИСОК СТАНЦИЙ ДЛЯ ВЫГРУЗКИ
        public static readonly RegulatorConfig[] Stations = new[]
        {
            new RegulatorConfig
            {
                Name = "Богучанская ГЭС",
                GeneratorNums = new[] { 37, 38, 39, 40, 41, 42, 43, 44, 45 }
            },

            new RegulatorConfig
            {
                Name = "Красноярская ГЭС",
                GeneratorNums = new[] { 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167 }
            },

            new RegulatorConfig
            {
                Name = "Красноярская ГРЭС-2",
                GeneratorNums = new[] { 147, 148, 149, 150, 151, 152, 153, 154, 155 }
            },

            new RegulatorConfig
            {
                Name = "Западно-Сибирская ТЭЦ",
                GeneratorNums = new[] { 88, 89, 90, 91, 92, 93, 94 }
            },

            new RegulatorConfig
            {
                Name = "Бийская ТЭЦ",
                GeneratorNums = new[] { 30, 31, 32, 33, 34, 35, 36 }
            },

            new RegulatorConfig
            {
                Name = "Cургутская ГРЭС-1",
                GeneratorNums = new[] { 301, 302, 303, 304, 305, 306, 307, 308, 309, 310 }
            }
        };
    }

    public static class AppSettings
    {
        // ВАЖНО: Путь должен вести к реальной папке с файлами режима!
        public static readonly string BaseDirectory = @"C:\Users\Михаил\Desktop\Diplom_Hромов";
        public static readonly string RegimeDirectory = Path.Combine(BaseDirectory, "mdp_debug_0");

        // Путь сохранения результатов
        public static readonly string OutputPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".", "generator_data_formatted.csv");
    }
}