using System;

namespace Hromov_Diplom
{
    /// <summary>Связь Генератор → Узел</summary>
    public struct GenNodeConnection
    {
        public int GenNum;           // Номер генератора
        public string GenName;       // Реальное имя генератора из таблицы
        public int NodeNum;          // Номер узла подключения
        public string StationName;   // Название станции
        public string StationCode;   // Код станции
    }
}