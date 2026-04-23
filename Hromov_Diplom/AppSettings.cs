using System.IO;

namespace Hromov_Diplom.Config
{
    public static class AppSettings
    {
        // 🔹 Корневая папка с данными
        public static readonly string BaseRoot = @"C:\Users\Михаил\Desktop\ДИплом на 30.03.2026\Подготовленные данные для расчета";
        
        // 🔹 Режимы для обработки (порядок важен!)
        public static readonly string[] RegimeFolders = new[]
        {
            "mdp_debug_1",                        // 1. Коэффициенты влияния
            "mdp_debug_1_Утяжеленный",            // 2. Утяжеленный
            "mdp_debug_1_Утяжеленный_до_МДП",     // 3. Утяжеленный_до_МДП
            "mdp_debug_1_Утяжеленный_с_Vzd",      // 4. Утяжеленный_с_Vzd(Станции)
            "mdp_debug_1_Утяжеленный_с_Vzd_до_МДП" // 5. Утяжеленный_с_Vzd(Станции)_до_МДП
        };

        // 🔹 Имя папки для финальных результатов
        public static readonly string OutputRoot = Path.Combine(BaseRoot, "Новые данные");
    }
}