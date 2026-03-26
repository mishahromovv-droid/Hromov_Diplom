using System;

namespace Hromov_Diplom.Config
{
    /// <summary>
    /// Конфигурация управляющего объекта (регулятора/станции)
    /// </summary>
    public class RegulatorConfig
    {
        /// <summary>
        /// Название станции или регулятора
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Номера генераторов, относящихся к данной станции
        /// </summary>
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// Настройки для таблицы ut_vir (МДП ВИР)
    /// </summary>
    public static class VirSettings
    {
        /// <summary>
        /// Имя таблицы в RastrWin3
        /// </summary>
        public static readonly string TableName = "ut_vir";

        /// <summary>
        /// Имя колонки для зануления
        /// </summary>
        public static readonly string ColumnName = "Act";

        /// <summary>
        /// Номер строки, которую НЕ нужно занулять
        /// </summary>
        public static readonly int KeepNum = 54;
    }

    /// <summary>
    /// Настройки расчёта МДП
    /// </summary>
    public static class CalculationSettings
    {
        /// <summary>
        /// Коэффициенты масштабирования Qmax для перебора
        /// </summary>
        public static readonly double[] ScaleFactors =
        {
            0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0
        };

        /// <summary>
        /// Список регулируемых объектов (станций) для обработки
        /// </summary>
        public static readonly RegulatorConfig[] Stations =
        {
            new RegulatorConfig
            {
                Name = "Богучанская ГЭС",
                GeneratorNums = new[] { 37, 38, 39, 40, 41, 42, 43, 44, 45 }
            },
            new RegulatorConfig
            {
                Name = "Красноярская ГЭС",
                GeneratorNums = new[] { 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167 }
            },
            new RegulatorConfig
            {
                Name = "Красноярская ГРЭС-2",
                GeneratorNums = new[] { 147, 148, 149, 150, 151, 152, 153, 154, 155 }
            },

            // ═══════════════════════════════════════════════════════════
            /// Новые регуляторы добавлять СЮДА:
            // ═══════════════════════════════════════════════════════════
            //
            // new RegulatorConfig
            // {
            //     Name = "Название станции",
            //     GeneratorNums = new[] { 1, 2, 3, 4 }
            // },
            //
            // ═══════════════════════════════════════════════════════════
        };
    }

    /// <summary>
    /// Общие настройки приложения
    /// </summary>
    public static class AppSettings
    {
        /// <summary>
        /// Путь к директории с файлами режима RastrWin3
        /// </summary>
        public static readonly string RegimeDirectory =
            @"C:\Users\Михаил\Desktop\Diplom_Hromov\mdp_debug_0";

        /// <summary>
        /// Путь сохранения результатов в CSV
        /// </summary>
        public static readonly string CsvPath =
            @"C:\Users\Михаил\Desktop\Diplom_Hromov\results_mdp.csv";

        /// <summary>
        /// Точность вывода чисел (знаков после запятой)
        /// </summary>
        public const int DecimalPrecision = 3;
    }
}