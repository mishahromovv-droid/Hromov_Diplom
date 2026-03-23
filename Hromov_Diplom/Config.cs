using System;

namespace Hromov_Diplom
{
    public static class CalculationSettings
    {
        public static readonly RegulatorConfig[] Stations = new[]
        {
            new RegulatorConfig { Name = "Богучанская ГЭС", GeneratorNums = new int[] { 37, 38, 39, 40, 41, 42, 43, 44, 45 }, Code = "9Г" },
            new RegulatorConfig { Name = "Красноярская ГЭС", GeneratorNums = new int[] { 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167 }, Code = "14Г" },
            new RegulatorConfig { Name = "Красноярская ГРЭС-2", GeneratorNums = new int[] { 147, 148, 149, 150, 151, 152, 153, 154, 155 }, Code = "9Г" }
        };
    }

    public class RegulatorConfig
    {
        public string Name { get; set; } = "";
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
        public string Code { get; set; } = "";
    }
}