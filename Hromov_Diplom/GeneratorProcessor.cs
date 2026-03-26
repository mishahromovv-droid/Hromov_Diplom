using System;
using System.Collections.Generic;
using System.Linq;

namespace Hromov_Diplom.GeneratorProcessing
{
    /// <summary>
    /// Обработчик для модификации параметров генераторов в режиме RastrWin3
    /// </summary>
    public class GeneratorProcessor
    {
        /// <summary>
        /// Множество номеров целевых генераторов для обработки
        /// </summary>
        private readonly HashSet<int> _targetGenNums;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="GeneratorProcessor"/>
        /// </summary> 
        /// <param name="targetGenNums">Массив номеров генераторов для обработки</param>
        /// <param name="stationName">Название станции (опционально)</param>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если <paramref name="targetGenNums"/> равен null
        /// </exception>
        public GeneratorProcessor(int[] targetGenNums, string stationName = "")
        {
            _targetGenNums = targetGenNums?.ToHashSet()
                ?? throw new ArgumentNullException(nameof(targetGenNums));
        }

        /// <summary>
        /// Применяет ограничения к генераторам и возвращает список изменений
        /// </summary>
        /// <param name="rastr">COM-объект RastrWin3</param>
        /// <param name="scaleFactor">Коэффициент масштабирования для расчёта Qmax</param>
        /// <returns>
        /// Список кортежей с информацией об изменениях:
        /// номер генератора, имя, Qmin, старый Qmax, новый Qmax
        /// </returns>
        public List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)>
            ApplyLimits(dynamic rastr, double scaleFactor)
        {
            var genTable = rastr.Tables("Generator");
            var colNum = genTable.Cols("Num");
            var colName = genTable.Cols("Name");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");
            var colNumPQ = genTable.Cols("NumPQ");

            var modifications =
                new List<(int num, string name, double qmin, double qmaxOld, double qmaxNew)>();

            for (int i = 0; i < genTable.Count; i++)
            {
                int num = (int)colNum.Z[i];
                if (!_targetGenNums.Contains(num))
                    continue;

                double qmin = (double)colQmin.Z[i];
                double qmaxOld = (double)colQmax.Z[i];
                string name = colName.Z[i]?.ToString() ?? "Без имени";

                // Устанавливаем NumPQ = 0 (генератор участвует в регулировании)
                colNumPQ.Z[i] = 0;

                // Расчёт нового Qmax по формуле: Qmax_new = Qmin + Volume × scaleFactor
                double volume = Math.Abs(qmin) + Math.Abs(qmaxOld);
                double qmaxNew = Math.Max(qmin, qmin + volume * scaleFactor);
                colQmax.Z[i] = Math.Round(qmaxNew, 10);

                modifications.Add((num, name, qmin, qmaxOld, qmaxNew));
            }

            return modifications;
        }

        /// <summary>
        /// Проверяет корректность диапазонов Qmin/Qmax для целевых генераторов
        /// </summary>
        /// <param name="rastr">COM-объект RastrWin3</param>
        /// <returns>
        /// <c>true</c>, если все диапазоны валидны (Qmax >= Qmin);
        /// иначе <c>false</c>
        /// </returns>
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
                        if ((double)colQmax.Z[i] < (double)colQmin.Z[i])
                            return false;
                    }
                }
            }

            return true;
        }
    }
}