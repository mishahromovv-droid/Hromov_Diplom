using System;

namespace Hromov_Diplom
{
    public class CalculationResult
    {
        public double Pred { get; set; }
        public double PadpReal { get; set; }
        public double ItogMdp { get; set; }
        public double ItogMdpPa { get; set; }
        public double PrabSech17 { get; set; }
        public double QminTotal { get; set; }
        public double QmaxTotal { get; set; }
        public double QrangeTotal { get; set; }
        public double ReservePct { get; set; }
        public bool MeetsRequiredReserve { get; set; }
        public bool IsMinReactivePick { get; set; }
        public double KB_Group { get; set; }
        public bool HasCore { get; set; }
    }

    public class IterationReport
    {
        public string StationName { get; set; } = "";
        public double Factor { get; set; }
        public CalculationResult Result { get; set; } = new();
        public string Status { get; set; } = "UNSET";
        public string ErrorDetails { get; set; } = "";
    }

    public struct QTotals
    {
        public double QminTotal;
        public double QmaxTotal;
        public double QrangeTotal => QmaxTotal - QminTotal;
        public QTotals(double qminTotal, double qmaxTotal) { QminTotal = qminTotal; QmaxTotal = qmaxTotal; }
    }

    public struct GenNodeConnection
    {
        public int GenNum;           // Номер генератора
        public string GenName;       // Имя из таблицы Generator
        public int NodeNum;          // Номер узла
        public string StationName;   // Название станции
        public string StationCode;   // Код станции
    }
}