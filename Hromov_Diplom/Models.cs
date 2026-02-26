using System;

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

    public class IterationReport
    {
        public double Factor { get; set; }
        public CalculationResult Result { get; set; } = new CalculationResult();
        public string Status { get; set; } = "UNSET";
        public string ErrorDetails { get; set; } = "";
        public string StationName { get; set; } = "";
    }
}