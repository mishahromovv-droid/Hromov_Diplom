using System;
using System.IO;

namespace Hromov_Diplom.Config
{
    /// <summary>
    /// Конфигурация одного средства регулирования напряжения (СРН)
    /// </summary>
    public class RegulatorConfig
    {
        /// <summary>
        /// Название станции (должно точно совпадать с выводом RastrWin3)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Номера генераторов, относящихся к этой станции
        /// </summary>
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// Настройки для таблицы ut_vir (ВИР)
    /// </summary>
    public static class VirSettings
    {
        public static readonly string TableName = "ut_vir";
        public static readonly string ColumnName = "Act";
        public static readonly int KeepNum = 54;
    }

    public static class CalculationSettings
    {
        public static readonly double[] ScaleFactors = new double[]
        {
            0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0
        };

        // ✅ ВСЕ СТАНЦИИ ДОБАВЛЕНЫ КОРРЕКТНО
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
                Name = "Сургутская ГРЭС-1",
                GeneratorNums = new[] { 301, 302, 303, 304, 305, 306, 307, 308, 309, 310 }
            }
        };
    }

    public static class AppSettings
    {
        // ✅ ПУТИ ИСПРАВЛЕНЫ: УБРАНА КИРИЛЛИЦА ИЗ ПУТЕЙ!
        public static readonly string BaseDirectory = @"C:\Users\Михаил\Desktop\Diplom_Hromov";
        public static readonly string RegimeDirectory = Path.Combine(BaseDirectory, "mdp_debug_0");
        public static readonly string CsvPath = Path.Combine(BaseDirectory, "results_mdp.csv");
    }
}