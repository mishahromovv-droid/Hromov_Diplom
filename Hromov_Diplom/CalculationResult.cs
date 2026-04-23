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
}