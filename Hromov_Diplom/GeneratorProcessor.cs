#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hromov_Diplom
{
    /// <summary>Обработчик генераторов: масштабирование Qmax и подсчёт ΣQ</summary>
    public class GeneratorProcessor
    {
        private readonly HashSet<int> _targetGenNums;

        public GeneratorProcessor(int[] targetGenNums)
        {
            _targetGenNums = targetGenNums?.ToHashSet()
                ?? throw new ArgumentNullException(nameof(targetGenNums));
        }

        /// <summary>
        /// Изменяем Qmax у выбранных генераторов по формуле:
        /// Qmax_new = Qmin + (|Qmin| + |Qmax_old|) * factor
        /// </summary>
        public List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)>
            ApplyLimits(dynamic rastr, double factor)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colName = genTable.Cols("Name");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");
            var colNumPQ = genTable.Cols("NumPQ");

            var modifications = new List<(int, string, double, double, double)>();

            for (int i = 0; i < genTable.Count; i++)
            {
                int num = Convert.ToInt32(colNum.Z[i]);
                if (!_targetGenNums.Contains(num))
                    continue;

                double qmin = Convert.ToDouble(colQmin.Z[i]);
                double qmaxOld = Convert.ToDouble(colQmax.Z[i]);
                string name = colName.Z[i]?.ToString() ?? "Без имени";

                // участие в регулировании отключаем
                colNumPQ.Z[i] = 0;

                double volume = Math.Abs(qmin) + Math.Abs(qmaxOld);
                double qmaxNew = Math.Max(qmin, qmin + volume * factor);

                colQmax.Z[i] = Math.Round(qmaxNew, 10);

                modifications.Add((num, name, qmin, qmaxOld, qmaxNew));
            }

            return modifications;
        }

        /// <summary>Проверяем что Qmax >= Qmin у выбранных генераторов</summary>
        public bool ValidateRanges(dynamic rastr)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");

            for (int i = 0; i < genTable.Count; i++)
            {
                int num = Convert.ToInt32(colNum.Z[i]);
                if (!_targetGenNums.Contains(num))
                    continue;

                double qmin = Convert.ToDouble(colQmin.Z[i]);
                double qmax = Convert.ToDouble(colQmax.Z[i]);
                if (qmax < qmin)
                    return false;
            }
            return true;
        }

        /// <summary>ΣQmin, ΣQmax и ΔQ по выбранным генераторам</summary>
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
                if (!_targetGenNums.Contains(num))
                    continue;

                qminTotal += Convert.ToDouble(colQmin.Z[i]);
                qmaxTotal += Convert.ToDouble(colQmax.Z[i]);
            }

            return new QTotals(qminTotal, qmaxTotal);
        }
    }
}