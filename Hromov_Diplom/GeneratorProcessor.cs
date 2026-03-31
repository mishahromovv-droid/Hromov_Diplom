using System;
using System.Collections.Generic;
using System.Linq;

namespace Hromov_Diplom.GeneratorProcessing
{
    public class GeneratorProcessor
    {
        private readonly HashSet<int> _targetGenNums;

        public GeneratorProcessor(int[] targetGenNums, string stationName = "")
        {
            _targetGenNums = targetGenNums?.ToHashSet() ?? throw new ArgumentNullException(nameof(targetGenNums));
        }

        public List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)> ApplyLimits(dynamic rastr, double scaleFactor)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colName = genTable.Cols("Name");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");
            var colNumPQ = genTable.Cols("NumPQ");

            var modifications = new List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)>();

            for (int i = 0; i < genTable.Count; i++)
            {
                int num = Convert.ToInt32(colNum.Z[i]);
                if (!_targetGenNums.Contains(num)) continue;

                double qmin = Convert.ToDouble(colQmin.Z[i]);
                double qmaxOld = Convert.ToDouble(colQmax.Z[i]);
                string name = colName.Z[i]?.ToString() ?? "Без имени";

                colNumPQ.Z[i] = 0;
                double volume = Math.Abs(qmin) + Math.Abs(qmaxOld);
                double qmaxNew = Math.Max(qmin, qmin + volume * scaleFactor);
                colQmax.Z[i] = Math.Round(qmaxNew, 10);

                modifications.Add((num, name, qmin, qmaxOld, qmaxNew));
            }

            return modifications;
        }

        public bool ValidateRanges(dynamic rastr)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");

            foreach (var genNum in _targetGenNums)
            {
                for (int i = 0; i < genTable.Count; i++)
                {
                    if ((int)colNum.Z[i] == genNum)
                    {
                        if ((double)colQmax.Z[i] < (double)colQmin.Z[i]) return false;
                    }
                }
            }

            return true;
        }
    }
}