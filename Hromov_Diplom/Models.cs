using System;

namespace Hromov_Diplom.Models
{
    /// <summary>
    /// Результат расчёта МДП для одной итерации
    /// </summary>
    public class CalculationResult
    {
        /// <summary>
        /// PRED — Прогнозная мощность (МВт)
        /// </summary>
        public double? Pred { get; set; }

        /// <summary>
        /// PADP_REAL — Фактическая мощность АДП (МВт)
        /// </summary>
        public double? PadpReal { get; set; }

        /// <summary>
        /// ITOG_MDP — Итоговая мощность МДП (МВт)
        /// </summary>
        public double? ItogMdp { get; set; }

        /// <summary>
        /// ITOG_MDP_PA — Итоговая мощность МДП с учётом ПА (МВт)
        /// </summary>
        public double? ItogMdpPa { get; set; }

        /// <summary>
        /// Флаг успешности расчёта
        /// </summary>
        /// <returns>
        /// <c>true</c>, если Pred и ItogMdp имеют значения;
        /// иначе <c>false</c>
        /// </returns>
        public bool IsSuccess => Pred.HasValue && ItogMdp.HasValue;
    }

    /// <summary>
    /// Отчёт по одной итерации расчёта МДП
    /// </summary>
    public class IterationReport
    {
        /// <summary>
        /// Коэффициент масштабирования Qmax для данной итерации
        /// </summary>
        public double Factor { get; set; }

        /// <summary>
        /// Результаты расчёта МДП
        /// </summary>
        public CalculationResult Result { get; set; } = new CalculationResult();

        /// <summary>
        /// Статус выполнения итерации (OK, ERROR, NO_RESULTS и т.д.)
        /// </summary>
        public string Status { get; set; } = "UNSET";

        /// <summary>
        /// Подробное описание ошибки (если возникла)
        /// </summary>
        public string ErrorDetails { get; set; } = string.Empty;

        /// <summary>
        /// Название станции/регулятора, для которой проводился расчёт
        /// </summary>
        public string StationName { get; set; } = string.Empty;
    }
}