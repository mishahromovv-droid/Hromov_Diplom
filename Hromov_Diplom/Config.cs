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
                        0.05, 0.50, 1.00
        };

        // ✅ ВСЕ СТАНЦИИ ДОБАВЛЕНЫ КОРРЕКТНО
        public static readonly RegulatorConfig[] Stations = new[]
        {
            new RegulatorConfig
            {
                Name = "Абаканская ТЭЦ",
                GeneratorNums = new[] { 1, 2, 3, 4 }
            },

            new RegulatorConfig
            {
                Name = "Барабинская ТЭЦ",
                GeneratorNums = new[] { 5, 6, 7, 8 }
            },

            new RegulatorConfig
            {
                Name = "Барнаульская ГТ ТЭЦ",
                GeneratorNums = new[] { 9, 10, 11, 12 }
            },

            new RegulatorConfig
            {
                Name = " Барнаульская ТЭЦ-2",
                GeneratorNums = new[] { 13, 14, 15, 16, 17 }
            },

            new RegulatorConfig
            {
                Name = "Барнаульская ТЭЦ-3",
                GeneratorNums = new[] { 18, 19, 20 }
            },

            new RegulatorConfig
            {
                Name = "Томская ГРЭС-2",
                GeneratorNums = new[] { 319, 320, 321, 322, 323, 324 }
            },

            new RegulatorConfig
            {
                Name = " Томская ТЭЦ-3",
                GeneratorNums = new[] {325}
            },

            new RegulatorConfig
            {
                Name = " ТЭЦ ПАО Омскшина",
                GeneratorNums = new[] { 413}
            },

            new RegulatorConfig
            {
                Name = " ТЭЦ Юргинский машиностроительный завод",
                GeneratorNums = new[] { 414}
            },

            new RegulatorConfig
            {
                Name = "Южноуральская ГРЭС",
                GeneratorNums = new[] { 472, 473, 474, 475, 476, 477 }
            },

            new RegulatorConfig
            {
                Name = "ТЭЦ СХК",
                GeneratorNums = new[] { 484 }
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