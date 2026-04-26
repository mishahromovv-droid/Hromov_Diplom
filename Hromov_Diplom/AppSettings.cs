using System.IO;

namespace Hromov_Diplom
{
    public static class AppSettings
    {
        public static readonly string BaseRoot = @"C:\Users\Михаил\Desktop\ДИплом на 30.03.2026\Подготовленные данные для расчета";
        public static readonly string OutputRoot = Path.Combine(BaseRoot, "Новые данные");
    }
}