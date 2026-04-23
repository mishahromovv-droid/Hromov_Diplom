using System;

namespace Hromov_Diplom.Config
{
    public static class CalculationSettings
    {
        public static readonly double[] ScaleFactors = new double[] { 0.05, 0.50, 1.00 };

        // ✅ ПОЛНЫЙ СПИСОК СТАНЦИЙ (объединённый из двух кодов)
        public static readonly RegulatorConfig[] Stations = new[]
        {
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Томская УШР",
                GeneratorNums = new[] { 431 }
            },

        };
    }
}