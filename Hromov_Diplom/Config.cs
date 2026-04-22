using System;
using System.IO;

namespace Hromov_Diplom.Config
{
    /// <summary>
    /// Конфигурация одного средства регулирования напряжения (СРН)
    /// </summary>
    public class RegulatorConfig
    {
        /// <summary>
        /// Название станции (должно точно совпадать с выводом RastrWin3)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Номера генераторов, относящихся к этой станции
        /// </summary>
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// Настройки для таблицы ut_vir (ВИР)
    /// </summary>
    public static class VirSettings
    {
        public static readonly string TableName = "ut_vir";
        public static readonly string ColumnName = "Act";
        public static readonly int KeepNum = 54;
    }

    public static class CalculationSettings
    {
        public static readonly double[] ScaleFactors = new double[]
        {
                        0.05, 0.10, 0.15, 0.20, 0.25, 0.30, 0.35, 0.40, 0.45, 0.50, 0.55, 0.60, 0.65, 0.70, 0.75, 0.80, 0.85, 0.90, 0.95, 1.00
        };

        // ✅ ВСЕ СТАНЦИИ ДОБАВЛЕНЫ КОРРЕКТНО
        public static readonly RegulatorConfig[] Stations = new[]
        {
           new RegulatorConfig
            {
                Name = "Березовская ГРЭС",
                GeneratorNums = new[] { 27, 28, 29}
            },

           new RegulatorConfig
            {
                Name = "Братская ГЭС",
                GeneratorNums = new[] {46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63 }
            },
           new RegulatorConfig
            {
                Name = " Канская ТЭЦ",
                GeneratorNums = new[] {131,132,133}
            },

           new RegulatorConfig
            {
                Name = "Красноярская ГЭС",
                GeneratorNums = new[] {156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167}
            },

           new RegulatorConfig
            {
                Name = "Красноярская ТЭЦ-2",
                GeneratorNums = new[] {178, 179, 180, 181}
            },

           new RegulatorConfig
            {
                Name = "Назаровская ГРЭС",
                GeneratorNums = new[] {198, 199, 200, 201, 202, 203, 204}
            },
           new RegulatorConfig
            {
                Name = "Богучанская ГЭС",
                GeneratorNums = new[] { 37, 38, 39, 40, 41, 42,43, 44, 45}
            },
           new RegulatorConfig
            {
                Name = "Красноярская ТЭЦ-1",
                GeneratorNums = new[] {168, 169, 170, 171, 172, 173, 174, 175, 176, 177}
            },
           new RegulatorConfig
            {
                Name = " Красноярская ТЭЦ-3",
                GeneratorNums = new[] {182}
            },
           new RegulatorConfig
            {
                Name = "Усть-Илимская ГЭС",
                GeneratorNums = new[] { 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 374 }
            },
           new RegulatorConfig
            {
                Name = "Усть-Илимская ТЭЦ",
                GeneratorNums = new[] { 375, 376, 377, 378, 379 }
            },
           // УШР
           // Подстанции 220 кВ
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Чадан",
                GeneratorNums = new[] { 446, 447 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Светлая",
                GeneratorNums = new[] { 443, 444 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Районная",
                GeneratorNums = new[] { 453, 454, 445 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Таксимо",
                GeneratorNums = new[] { 459, 460 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Пеледуй",
                GeneratorNums = new[] { 441, 442 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Петровск-Забайкальская",
                GeneratorNums = new[] { 457, 458 }
            },

            // Подстанции 500 кВ
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Восход",
                GeneratorNums = new[] { 421 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Озерная",
                GeneratorNums = new[] { 426, 427, 435, 436 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Сеней",
                GeneratorNums = new[] { 428 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Луговая",
                GeneratorNums = new[] { 424 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Нельм",
                GeneratorNums = new[] { 425 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Усть-Кут",
                GeneratorNums = new[] { 432 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Камала-1",
                GeneratorNums = new[] { 423 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Тайшет",
                GeneratorNums = new[] { 430 }
            },
            new RegulatorConfig
            {
                Name = "ПС 500 кВ Томская",
                GeneratorNums = new[] { 431 }
            },

            // Прочие объекты
            new RegulatorConfig
            {
                Name = "УШР-110 кВ ПС 220 Кызыльская",
                GeneratorNums = new[] { 456 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ НПС-9",
                GeneratorNums = new[] { 439, 440 }
            },
            new RegulatorConfig
            {
                Name = "РП 110 кВ Полюс",
                GeneratorNums = new[] { 455 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Чита",
                GeneratorNums = new[] { 448 }
            },
            new RegulatorConfig
            {
                Name = "ПС 220 кВ Ергаки",
                GeneratorNums = new[] { 438 }
            },

        };
    }

    public static class AppSettings
    {
        // ✅ ПУТИ ИСПРАВЛЕНЫ: УБРАНА КИРИЛЛИЦА ИЗ ПУТЕЙ!
        public static readonly string BaseDirectory = @"C:\Users\Михаил\Desktop\Diplom_Hromov";
        public static readonly string RegimeDirectory = Path.Combine(BaseDirectory, "mdp_debug_0");
        public static readonly string CsvPath = Path.Combine(BaseDirectory, "results_mdp.csv");
    }
}