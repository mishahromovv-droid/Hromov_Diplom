using System;
using System.Collections.Generic;

namespace Hromov_Diplom.Models
{
    public class CalculationResult
    {
        public double? Pred { get; set; }
        public double? PadpReal { get; set; }
        public double? ItogMdp { get; set; }
        public double? ItogMdpPa { get; set; }
        public bool IsSuccess => Pred.HasValue && ItogMdp.HasValue;
    }

    public class GeneratorModification
    {
        public int Num { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Qmin { get; set; }
        public double QmaxOld { get; set; }
        public double QmaxNew { get; set; }
    }

    public class IterationReport
    {
        public double Factor { get; set; }
        public CalculationResult Result { get; set; } = new CalculationResult();
        public string Status { get; set; } = "UNSET";
        public string ErrorDetails { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public List<GeneratorModification> GeneratorModifications { get; set; } = new List<GeneratorModification>();
    }
}