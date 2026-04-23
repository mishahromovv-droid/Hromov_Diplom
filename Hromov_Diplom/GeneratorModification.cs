namespace Hromov_Diplom.Models
{
    public class GeneratorModification
    {
        public int Num { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Qmin { get; set; }
        public double QmaxOld { get; set; }
        public double QmaxNew { get; set; }
    }
}