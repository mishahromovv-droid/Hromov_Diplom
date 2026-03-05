#nullable enable
using System;

namespace Hromov_Diplom
{
    /// <summary>Конфигурация станции/регулятора</summary>
    public class RegulatorConfig
    {
        public string Name { get; set; } = string.Empty;
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
    }

    public static class VirSettings
    {
        public static readonly string TableName = "ut_vir";
        public static readonly string ColumnName = "Act";
        public static readonly int KeepNum = 54;
    }

    public static class CalculationSettings
    {
        public static readonly double[] ScaleFactors =
        {
            0.1, 0.2, 0.3, 0.4, 0.5,
            0.6, 0.7, 0.8, 0.9, 1.0
        };

        public static readonly RegulatorConfig[] Stations =
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
            }
        };
    }

    public static class AppSettings
    {
        public static readonly string RegimeDirectory =
            @"C:\Users\Михаил\Desktop\Diplom_Hromov\mdp_debug_0";

        public static readonly string CsvPath =
            @"C:\Users\Михаил\Desktop\Diplom_Hromov\results_mdp.csv";

        public const int DecimalPrecision = 3;

        // ✅ Сечение для Pраб (таблица sechen, ns = 17, столбец psech)
        public const int PrabSectionNum = 17;

        // ✅ Требуемый запас устойчивости ζтр, %
        // Поставь как требует преподаватель (например 20)
        public const double RequiredReservePct = 20.0;
    }
}