using System;
using System.IO;

namespace Hromov_Diplom.Config
{
    /// <summary>
    /// Конфигурация одной станции для выгрузки данных о реактивной мощности
    /// </summary>
    public class RegulatorConfig
    {
        /// <summary>
        /// Название станции (должно точно совпадать с выводом RastrWin3)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Номера генераторов этой станции
        /// </summary>
        public int[] GeneratorNums { get; set; } = Array.Empty<int>();
    }

    public static class CalculationSettings
    {
        // ✅ СПИСОК СТАНЦИЙ ДЛЯ ВЫГРУЗКИ
        public static readonly RegulatorConfig[] Stations = new[]
        {
new RegulatorConfig
            {
                Name = "Абаканская ТЭЦ",
                GeneratorNums = new[] { 1, 2, 3, 4 }
            },

            new RegulatorConfig
            {
                Name = "Барабинская ТЭЦ",
                GeneratorNums = new[] { 5, 6, 7, 8 }
            },

            new RegulatorConfig
            {
                Name = "Барнаульская ГТ ТЭЦ",
                GeneratorNums = new[] { 9, 10, 11, 12 }
            },

            new RegulatorConfig
            {
                Name = " Барнаульская ТЭЦ-2",
                GeneratorNums = new[] { 13, 14, 15, 16, 17 }
            },

            new RegulatorConfig
            {
                Name = "Барнаульская ТЭЦ-3",
                GeneratorNums = new[] { 18, 19, 20 }
            },



            new RegulatorConfig
            {
                Name = "Беловская ГРЭС",
                GeneratorNums = new[] { 21, 22, 23,  24, 25, 26 }
            },

            new RegulatorConfig
            {
                Name = "Березовская ГРЭС",
                GeneratorNums = new[] { 27, 28, 29}
            },

            new RegulatorConfig
            {
                Name = "Бийская ТЭЦ",
                GeneratorNums = new[] { 30, 31, 32, 33, 34, 35, 36}
            },

            new RegulatorConfig
            {
                Name = "Богучанская ГЭС",
                GeneratorNums = new[] { 37, 38, 39, 40, 41, 42,43, 44, 45}
            },

new RegulatorConfig
            {
                Name = "Братская ГЭС",
                GeneratorNums = new[] {46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63 }
            },



            new RegulatorConfig
            {
                Name = "Ванкорская ГТЭС",
                GeneratorNums = new[] { 64, 65, 66, 67, 68, 69, 70, 71}
            },

            new RegulatorConfig
            {
                Name = "ГТЭС Ново-кузнецкая",
                GeneratorNums = new[] {72, 73}
            },

            new RegulatorConfig
            {
                Name = "Гусиноозерская ГРЭС",
                GeneratorNums = new[] {74, 75, 76, 77, 78,79}
            },

            new RegulatorConfig
            {
                Name = "ЕЭК",
                GeneratorNums = new[] {80, 81, 82, 83, 84, 85, 86, 87}
            },

            new RegulatorConfig
            {
                Name = "Западно-Сибирская ТЭЦ",
                GeneratorNums = new[] {88, 89, 90, 91, 92, 93, 94}
            },



            new RegulatorConfig
            {
                Name = "Иркутская ГЭС",
                GeneratorNums = new[] {95, 96, 97, 98, 99, 100, 101, 102,}
            },

new RegulatorConfig
{
    Name = "Иркутская ТЭЦ-9",
    GeneratorNums = new[] {103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115}
},

new RegulatorConfig
{
    Name = "Иркутская ТЭЦ-11",
    GeneratorNums = new[] {124, 125, 126, 127, 128, 129, 130}
},

new RegulatorConfig
{
    Name = " Канская ТЭЦ",
    GeneratorNums = new[] {131,132,133}
},

new RegulatorConfig
{
    Name = "Кемеровская ГРЭС",
    GeneratorNums = new[] {134, 135, 136, 137, 138, 139, 140, 141, 142}
},



new RegulatorConfig
{
    Name = " Кемеровская ТЭЦ",
    GeneratorNums = new[] {143, 144, 145, 146}
},

new RegulatorConfig
{
    Name = "Красноярская ГРЭС-2",
    GeneratorNums = new[] {147, 148, 149, 150, 151, 152, 153, 154, 155}
},

new RegulatorConfig
{
    Name = "Красноярская ГЭС",
    GeneratorNums = new[] {156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167}
},

new RegulatorConfig
{
    Name = "Красноярская ТЭЦ-1",
    GeneratorNums = new[] {168, 169, 170, 171, 172, 173, 174, 175, 176, 177}
},

new RegulatorConfig
{
    Name = "Красноярская ТЭЦ-2",
    GeneratorNums = new[] {178, 179, 180, 181}
},



new RegulatorConfig
{
    Name = " Красноярская ТЭЦ-3",
    GeneratorNums = new[] {182}
},


new RegulatorConfig
{
    Name = "Кузнецкая ТЭЦ",
    GeneratorNums = new[] {183, 184, 185, 186, 187, 188, 189}
},

new RegulatorConfig
{
    Name = "Майнская ГЭС",
    GeneratorNums = new[] {190, 191, 192}
},

new RegulatorConfig
{
    Name = "Мамаканская ГЭС",
    GeneratorNums = new[] {193, 194, 195, 196}
},

new RegulatorConfig
{
    Name = "Минусинская ТЭЦ",
    GeneratorNums = new[] {197}
},


new RegulatorConfig
{
    Name = "Назаровская ГРЭС",
    GeneratorNums = new[] {198, 199, 200, 201, 202, 203, 204}
},

new RegulatorConfig
{
    Name = " Нижневартовская ГРЭС",
    GeneratorNums = new[] {205, 206, 207, 208}
},

new RegulatorConfig
{
    Name = "Ново-Зиминская ТЭЦ",
    GeneratorNums = new[] {209, 210, 211, 212, 213, 214, 215, 216, 217}
},

new RegulatorConfig
{
    Name = "Ново-Кемеровская ТЭЦ",
    GeneratorNums = new[] {218, 219, 220, 221, 222, 223, 224, 225}
},

new RegulatorConfig
{
    Name = "Новосибирская ГЭС",
    GeneratorNums = new[] {226, 227, 228, 229, 230, 231, 232}
},



new RegulatorConfig
{
    Name = "Новосибирская ТЭЦ-2",
    GeneratorNums = new[] {233, 234, 235, 236, 237, 238, 239}
},

new RegulatorConfig
{
    Name = "Новосибирская ТЭЦ-3",
    GeneratorNums = new[] {240, 241, 242, 243, 244, 245, 246, 247, 248}
},

new RegulatorConfig
{
    Name = "Новосибирская ТЭЦ-4",
    GeneratorNums = new[] {249, 250, 251, 252, 253, 254}
},

new RegulatorConfig
{
    Name = "Новосибирская ТЭЦ-5",
    GeneratorNums = new[] {255, 256, 257, 258, 259, 260}
},

new RegulatorConfig
{
    Name = "Омская ТЭЦ-3",
    GeneratorNums = new[] {261, 262, 263, 264, 265, 266, 267, 268}
},



new RegulatorConfig
{
    Name = "Омская ТЭЦ-4",
    GeneratorNums = new[] {269, 270, 271, 272}
},

new RegulatorConfig
{
    Name = "Омская ТЭЦ-5",
    GeneratorNums = new[] {273, 274, 275, 276, 277}
},

new RegulatorConfig
{
    Name = "Приаргунская ТЭЦ",
    GeneratorNums = new[] { 278, 279}
},

new RegulatorConfig
{
    Name = "ПС 500 кВ Курчатовская, ТГ-7",
    GeneratorNums = new[] { 280}
},

new RegulatorConfig
{
    Name = "Рефтинская ГРЭС",
    GeneratorNums = new[] {281, 282, 283, 284, 285, 286, 287, 288, 289, 290}
},



new RegulatorConfig
{
    Name = "Саяно-Шушенская ГЭС",
    GeneratorNums = new[] {291, 292, 293, 294, 295, 296, 297, 298, 299, 300}
},

new RegulatorConfig
{
    Name = "Сургутская ГРЭС-1",
    GeneratorNums = new[] {301, 302, 303, 304, 305, 306, 307, 308, 309, 310}
},


new RegulatorConfig
{
    Name = "Сургутская ГРЭС-2",
    GeneratorNums = new[] {311, 312, 313, 314, 315, 316, 317, 318}
},


new RegulatorConfig
{
    Name = "Томская ГРЭС-2",
    GeneratorNums = new[] { 319, 320, 321, 322, 323, 324 }
},

new RegulatorConfig
{
    Name = " Томская ТЭЦ-3",
    GeneratorNums = new[] {325}
},


new RegulatorConfig
{
    Name = "Томь-Усинская ГРЭС",
    GeneratorNums = new[] { 326, 327, 328, 329, 330, 331, 332, 333, 334 }
},

new RegulatorConfig
{
    Name = " Троицкая ГРЭС",
    GeneratorNums = new[] { 335}
},

new RegulatorConfig
{
    Name = "ТЭЦ АКХЗ",
    GeneratorNums = new[] { 336, 337, 338 }
},

new RegulatorConfig
{
    Name = "ТЭЦ ППГХО",
    GeneratorNums = new[] { 339, 340, 341, 342, 343, 344 }
},

new RegulatorConfig
{
    Name = "ТЭЦ СХК",
    GeneratorNums = new[] { 345, 346, 348, 349, 350, 351, 354 }
},



new RegulatorConfig
{
    Name = "Улан-Удэнская ТЭЦ-1",
    GeneratorNums = new[] { 355, 356, 357, 358 }
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

new RegulatorConfig
{
    Name = "Харанорская ГРЭС",
    GeneratorNums = new[] { 380, 381, 382 }
},

new RegulatorConfig
{
    Name = "Читинская ТЭЦ-1",
    GeneratorNums = new[] { 383, 384, 385, 386, 387, 388 }
},



new RegulatorConfig
{
    Name = " Шерловогорская ТЭЦ",
    GeneratorNums = new[] {389}
},

new RegulatorConfig
{
    Name = "Экибастузская ГРЭС-1",
    GeneratorNums = new[] { 390, 391, 392, 393, 394, 395, 396, 397 }
},

new RegulatorConfig
{
    Name = "Экибастузская ГРЭС-2",
    GeneratorNums = new[] { 398, 399}
},

new RegulatorConfig
{
    Name = "Южно-Кузбасская ГРЭС",
    GeneratorNums = new[] { 400, 401, 402, 403, 404, 405, 406, 407 }
},


new RegulatorConfig
{
    Name = "ЗАО \"Инновация\" (Газопоршневая электрогенерационная установка)",
    GeneratorNums = new[] { 408 }
},


new RegulatorConfig
{
    Name = "Иркутская ТЭЦ-12",
    GeneratorNums = new[] { 409, 410}
},

new RegulatorConfig
{
    Name = " ТЭС филиала АО Группа  Илим в г.Усть-Илимске",
    GeneratorNums = new[] { 411}
},

new RegulatorConfig
{
    Name = " ТЭЦ ООО Омсктехуглерод",
    GeneratorNums = new[] { 412}
},


new RegulatorConfig
{
    Name = " ТЭЦ ПАО Омскшина",
    GeneratorNums = new[] { 413}
},

new RegulatorConfig
{
    Name = " ТЭЦ Юргинский машиностроительный завод",
    GeneratorNums = new[] { 414}
},


new RegulatorConfig
{
    Name = "Участок ТИиТС Иркутской ТЭЦ-6",
    GeneratorNums = new[] { 415}
},


new RegulatorConfig
{
    Name = "Центральная ТЭЦ",
    GeneratorNums = new[] { 416}
},


new RegulatorConfig
{
    Name = "Шелеховский Участок Ново-Иркутской ТЭЦ",
    GeneratorNums = new[] { 417}
},


new RegulatorConfig
{
    Name = "ЕЭК",
    GeneratorNums = new[] { 418}
},

new RegulatorConfig
{
    Name = "ПС 500 кВ Ангара",
    GeneratorNums = new[] { 419, 433, 434 }
},
new RegulatorConfig
{
    Name = "ПС 500 кВ Барабинская",
    GeneratorNums = new[] { 420 }
},

new RegulatorConfig
{
    Name = "ПС 500 кВ Восход",
    GeneratorNums = new[] { 421 }
},

new RegulatorConfig
{
    Name = "ПС 500 кВ Камала-1",
    GeneratorNums = new[] { 423 }
},

new RegulatorConfig
{
    Name = "ПС 500 кВ Луговая",
    GeneratorNums = new[] { 424 }
},

new RegulatorConfig
{
    Name = "ПС 500 кВ Нелым",
    GeneratorNums = new[] { 425 }
},



new RegulatorConfig
{
    Name = "ПС 500 кВ Озерная",
    GeneratorNums = new[] { 426, 427, 435, 436 }
},


new RegulatorConfig
{
    Name = "ПС 500 кВ Семей",
    GeneratorNums = new[] { 428 }
},


new RegulatorConfig
{
    Name = "ПС 500 кВ Таврическая",
    GeneratorNums = new[] { 429 }
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


new RegulatorConfig
{
    Name = "ПС 500 кВ Усть-Кут",
    GeneratorNums = new[] { 432 }
},


new RegulatorConfig
{
    Name = "ПС 220 кВ Ергаки",
    GeneratorNums = new[] { 438 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ НПС-9",
    GeneratorNums = new[] { 439, 440 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ Пеледуй",
    GeneratorNums = new[] { 441, 442 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ Светлая",
    GeneratorNums = new[] { 443, 444 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ Таксимо",
    GeneratorNums = new[] { 445, 459, 460 }
},


new RegulatorConfig
{
    Name = "ПС 220 кВ Чадан",
    GeneratorNums = new[] { 446, 447 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ Чита",
    GeneratorNums = new[] { 448 }
},


new RegulatorConfig
{
    Name = "ПС 220 кВ Приангарская",
    GeneratorNums = new[] { 449, 450 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ Раздолинская",
    GeneratorNums = new[] { 451, 452 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ Районная",
    GeneratorNums = new[] { 453, 454 }
},


new RegulatorConfig
{
    Name = "РП 110 кВ Полюс",
    GeneratorNums = new[] { 455 }
},



new RegulatorConfig
{
    Name = "УШР-110 кВ ПС 220 Кызыльская",
    GeneratorNums = new[] { 456 }
},

new RegulatorConfig
{
    Name = "ПС 220 кВ Петровск-Забайкальская",
    GeneratorNums = new[] { 457, 458 }
},
new RegulatorConfig
{
    Name = "ПС 500 кВ Заря",
    GeneratorNums = new[] { 461 }
},


new RegulatorConfig
{
    Name = "ПС 500 кВ Ново-Зиминская",
    GeneratorNums = new[] { 462 }
},


new RegulatorConfig
{
    Name = "Улан-Баторская ТЭЦ-4",
    GeneratorNums = new[] { 463, 464, 465, 466, 467, 468, 469 }
},


new RegulatorConfig
{
    Name = "Ириклинская ГРЭС",
    GeneratorNums = new[] { 470, 471, 478, 479, 480, 481, 482, 483 }
},






new RegulatorConfig
{
    Name = "Южноуральская ГРЭС",
    GeneratorNums = new[] { 472, 473, 474, 475, 476, 477 }
},


new RegulatorConfig
{
    Name = "ТЭЦ СХК",
    GeneratorNums = new[] { 484 }
}

        };
    }

    public static class AppSettings
    {
        // ВАЖНО: Путь должен вести к реальной папке с файлами режима!
        public static readonly string BaseDirectory = @"C:\Users\Михаил\Desktop\Diplom_Hromov";
        public static readonly string RegimeDirectory = Path.Combine(BaseDirectory, "mdp_debug_0");

        // Путь сохранения результатов
        public static readonly string OutputPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            "generator_data_formatted.csv");
    }
}