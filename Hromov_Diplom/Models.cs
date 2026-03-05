#nullable enable
namespace Hromov_Diplom
{
    /// <summary>Результаты, которые читаем из Rastr + доп. поля</summary>
    public class CalculationResult
    {
        public double? Pred { get; set; }
        public double? PadpReal { get; set; }
        public double? ItogMdp { get; set; }
        public double? ItogMdpPa { get; set; }

        public double? PrabSech17 { get; set; }

        public double? QminTotal { get; set; }
        public double? QmaxTotal { get; set; }
        public double? QrangeTotal { get; set; }

        // ζ = (Pпред - Pраб)/Pраб*100%
        public double? ReservePct { get; set; }

        // выполняет ли ζ ≥ ζтр
        public bool? MeetsRequiredReserve { get; set; }

        // отметка "минимальный резерв реактивной мощности" среди подходящих
        public bool? IsMinReactivePick { get; set; }

        public bool HasCore => PrabSech17.HasValue && ItogMdp.HasValue && PrabSech17.Value != 0;
    }

    public class IterationReport
    {
        public string StationName { get; set; } = string.Empty;
        public double Factor { get; set; }
        public CalculationResult Result { get; set; } = new CalculationResult();
        public string Status { get; set; } = "UNSET";
        public string ErrorDetails { get; set; } = string.Empty;
    }

    /// <summary>Суммарные Q по выбранным генераторам</summary>
    public readonly struct QTotals
    {
        public QTotals(double qminTotal, double qmaxTotal)
        {
            QminTotal = qminTotal;
            QmaxTotal = qmaxTotal;
        }

        public double QminTotal { get; }
        public double QmaxTotal { get; }
        public double QrangeTotal => QmaxTotal - QminTotal;
    }
}