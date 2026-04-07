using System;
using System.IO;

namespace Hromov_Diplom
{
    public static class VirSettings
    {
        public static readonly string TableName = "ut_vir";
        public static readonly string ColumnName = "Act";
        public static readonly int KeepNum = 54;
    }

    public static class CalculationSettings
    {
        public static readonly double[] ScaleFactors = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };

        public static readonly RegulatorConfig[] Stations = new[]
        {
            new RegulatorConfig { Name = "Красноярская ГЭС", GeneratorNums = new int[] { 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167 }, Code = "14Г" },
            new RegulatorConfig { Name = "Западно-Сибирская ТЭЦ", GeneratorNums = new int[] { 88, 89, 90, 91, 92, 93, 94 }, Code = "7Г" },
            new RegulatorConfig { Name = "Бийская ТЭЦ", GeneratorNums = new int[] { 30, 31, 32, 33, 34, 35, 36 }, Code = "7Г" },
            new RegulatorConfig { Name = "ЕЭК", GeneratorNums = new int[] { 80, 81, 82, 83, 84, 85, 86, 87 }, Code = "8Г" },
            new RegulatorConfig { Name = "ПС 500 кВ Ангара", GeneratorNums = new int[] { 419, 433, 434 }, Code = "3Г" },
            new RegulatorConfig { Name = "ПС 500 кВ Тайшет", GeneratorNums = new int[] { 430 }, Code = "1Г" },
            new RegulatorConfig { Name = "ПС 500 кВ Томская", GeneratorNums = new int[] { 431 }, Code = "1Г" }
        };
    }

    public class RegulatorConfig
    {
        public string Name { get; set; } = "";
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
        public string Code { get; set; } = "";
    }

    public static class AppSettings
    {
        public static readonly string RegimeDirectory = @"C:\Users\Михаил\Desktop\Diplom_Hromov\mdp_debug_0";
        public static readonly string CsvPath = @"C:\Users\Михаил\Desktop\Diplom_Hromov\results_mdp.csv";
        public const int PrabSectionNum = 17;
        public const double RequiredReservePct = 20.0;
    }
}