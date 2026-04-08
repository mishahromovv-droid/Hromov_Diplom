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
                Name = "Березовская ГРЭС",
                GeneratorNums = new[] { 27, 28, 29}
            },

           new RegulatorConfig
            {
                Name = "Братская ГЭС",
                GeneratorNums = new[] {46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63 }
            },
           new RegulatorConfig
            {
                Name = " Канская ТЭЦ",
                GeneratorNums = new[] {131,132,133}
            },

           new RegulatorConfig
            {
                Name = "Красноярская ГЭС",
                GeneratorNums = new[] {156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167}
            },

           new RegulatorConfig
            {
                Name = "Красноярская ТЭЦ-2",
                GeneratorNums = new[] {178, 179, 180, 181}
            },

           new RegulatorConfig
            {
                Name = "Назаровская ГРЭС",
                GeneratorNums = new[] {198, 199, 200, 201, 202, 203, 204}
            },
           new RegulatorConfig
            {
                Name = "Богучанская ГЭС",
                GeneratorNums = new[] { 37, 38, 39, 40, 41, 42,43, 44, 45}
            },
           new RegulatorConfig
            {
                Name = "Красноярская ТЭЦ-1",
                GeneratorNums = new[] {168, 169, 170, 171, 172, 173, 174, 175, 176, 177}
            },
           new RegulatorConfig
            {
                Name = " Красноярская ТЭЦ-3",
                GeneratorNums = new[] {182}
            },

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