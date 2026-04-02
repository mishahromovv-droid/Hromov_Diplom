using System;
using System.Collections.Generic;

namespace Hromov_Diplom.Models
{
    /// <summary>
    /// Структура данных для одного генератора (для экспорта CSV)
    /// </summary>
    public class GeneratorReportData
    {
        public string StationName { get; set; } = string.Empty;
        public int GeneratorNum { get; set; }
        public string Name { get; set; } = string.Empty;
        public double ActivePower { get; set; }
        public double ReactivePower { get; set; }
        public double Qmin { get; set; }
        public double Qmax { get; set; }
        public int StatusFlag { get; set; } // STA флаг
    }
}