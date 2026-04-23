using System.Collections.Generic;

namespace Hromov_Diplom.Models
{
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