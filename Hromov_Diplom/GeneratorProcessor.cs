using System;
using System.Collections.Generic;

namespace Hromov_Diplom
{
    public class GeneratorProcessor
    {
        private readonly HashSet<int> _targetGenNums;

        public GeneratorProcessor(int[] targetGenNums)
        {
            _targetGenNums = new HashSet<int>(targetGenNums ?? throw new ArgumentNullException(nameof(targetGenNums)));
        }

        public List<object> ApplyLimits(dynamic rastr, double factor)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colName = genTable.Cols("Name");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");
            var colNumPQ = genTable.Cols("NumPQ");

            var modifications = new List<object>();

            for (int i = 0; i < genTable.Count; i++)
            {
                int num = Convert.ToInt32(colNum.Z[i]);
                if (!_targetGenNums.Contains(num)) continue;

                double qmin = Convert.ToDouble(colQmin.Z[i]);
                double qmaxOld = Convert.ToDouble(colQmax.Z[i]);

                colNumPQ.Z[i] = 0;

                double volume = Math.Abs(qmin) + Math.Abs(qmaxOld);
                double qmaxNew = Math.Max(qmin, qmin + volume * factor);

                colQmax.Z[i] = Math.Round(qmaxNew, 10);

                modifications.Add(new
                {
                    Num = num,
                    Qmin = qmin,
                    QmaxOld = qmaxOld,
                    QmaxNew = qmaxNew
                });
            }

            return modifications;
        }

        public bool ValidateRanges(dynamic rastr)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");

            for (int i = 0; i < genTable.Count; i++)
            {
                int num = Convert.ToInt32(colNum.Z[i]);
                if (!_targetGenNums.Contains(num)) continue;

                double qmin = Convert.ToDouble(colQmin.Z[i]);
                double qmax = Convert.ToDouble(colQmax.Z[i]);
                if (qmax < qmin) return false;
            }
            return true;
        }

        public QTotals GetQTotals(dynamic rastr)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");

            double qminTotal = 0.0;
            double qmaxTotal = 0.0;

            for (int i = 0; i < genTable.Count; i++)
            {
                int num = Convert.ToInt32(colNum.Z[i]);
                if (!_targetGenNums.Contains(num)) continue;

                qminTotal += Convert.ToDouble(colQmin.Z[i]);
                qmaxTotal += Convert.ToDouble(colQmax.Z[i]);
            }

            return new QTotals(qminTotal, qmaxTotal);
        }
    }
}