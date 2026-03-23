using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hromov_Diplom
{
    public static class VoltageAnalyzer
    {
        /// <summary>
        /// Читает связи между генераторами и узлами для заданных генераторов
        /// Без чтения названий узлов
        /// </summary>
        public static List<GenNodeConnection> GetConnections(dynamic rastr, int[] genNums, string stationName, string stationCode)
        {
            var result = new List<GenNodeConnection>();

            try
            {
                // ---------------------------------------------------------
                // ШАГ А: Открываем таблицу Generator и находим Num + Node + Name
                // ---------------------------------------------------------
                dynamic genTable = rastr.Tables("Generator");
                if (genTable == null) return result;

                dynamic colGenNum = FindColumn(genTable.Cols, "Num", "num", "НОМЕР", "ID");
                dynamic colGenName = FindColumn(genTable.Cols, "Name", "name", "NAME", "Название");
                dynamic colNode = FindColumn(genTable.Cols, "Node", "node", "NODE", "Узел");

                if (colGenNum == null || colNode == null) return result;

                Console.WriteLine("   ✓ Столбцы 'Num', 'Name' и 'Node' найдены.");

                // Находим все нужные нам пары
                for (int i = 0; i < genTable.Count; i++)
                {
                    try
                    {
                        object numObj = colGenNum.Z[i];
                        object nameObj = colGenName?.Z[i];
                        object nodeObj = colNode.Z[i];

                        if (numObj == null || nodeObj == null) continue;

                        int genNum = Convert.ToInt32(numObj.ToString().Trim());

                        if (Array.Exists(genNums, x => x == genNum))
                        {
                            int nodeNum = Convert.ToInt32(nodeObj.ToString().Trim());

                            string nameValue = "";
                            if (nameObj != null) nameValue = nameObj.ToString().Trim();
                            else nameValue = $"Нет названия";

                            result.Add(new GenNodeConnection
                            {
                                GenNum = genNum,
                                GenName = nameValue,
                                NodeNum = nodeNum,
                                StationName = stationName,
                                StationCode = stationCode
                            });
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR]: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Помощник: находит колонку по имени
        /// </summary>
        private static dynamic FindColumn(dynamic cols, params string[] names)
        {
            try
            {
                int count = cols.Count;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic col = cols[i];
                        if (col == null) continue;

                        string name = col.Name ?? "";

                        foreach (var n in names)
                        {
                            if ((name.ToLower() == n.ToLower()) || name.Contains(n.ToLower()))
                                return cols[i];
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }
    }
}