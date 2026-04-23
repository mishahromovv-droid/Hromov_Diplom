using System;

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
}