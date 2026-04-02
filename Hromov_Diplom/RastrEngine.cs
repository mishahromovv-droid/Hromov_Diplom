// ⚠️ ВАЖНО: Добавлено DISABLE для аннотаций nullable
#nullable disable 

using Hromov_Diplom.Config;
using Hromov_Diplom.Models;
using Hromov_Diplom.Reports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Hromov_Diplom.ComService
{
    /// <summary>
    /// Класс для работы с RastrWin3 через COM
    /// </summary>
    public class RastrEngine : IDisposable
    {
        private dynamic _rastrObj;
        private bool _isDisposed = false;

        /// <summary>
        /// Инициализация COM-объекта RastrWin3
        /// </summary>
        public void Initialize(string progId = "Astra.Rastr")
        {
            Type type = Type.GetTypeFromProgID(progId);
            if (type == null)
                throw new Exception($"COM-объект '{progId}' не зарегистрирован!");

            _rastrObj = Activator.CreateInstance(type);
        }

        /// <summary>
        /// Загрузка файлов режима из указанной директории или файла
        /// </summary>
        public void LoadModeFiles(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path) && !Directory.Exists(path))
                throw new DirectoryNotFoundException($"Файлы режима не найдены: {path}");

            Console.WriteLine("⏳ Загрузка файла режима...");

            if (File.Exists(path))
            {
                _rastrObj.Load(1, path, "");
            }
            else if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path).OrderBy(f => f))
                    if (!Path.GetFileName(file).StartsWith("~")) _rastrObj.Load(1, file, "");
            }
        }

        /// ✅ МЕТОД ДЛЯ ЭКСПОРТА ДАННЫХ ИЗ ТАБЛИЦЫ "ГЕНЕРАТОРЫ" С ДОБАВЛЕНИЕМ КОЛОНКИ 'STA'
        public void ExportGeneratorDataToCSV()
        {
            try
            {
                // Создаем папку рядом с exe-файлом если она не существует
                string outDir = Path.GetDirectoryName(AppSettings.OutputPath) ?? ".";
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                string filePath = AppSettings.OutputPath;

                Console.WriteLine("⏳ Поиск таблицы генераторов...");
                var genTable = FindGeneratorTable();

                Console.WriteLine($"📊 Количество строк в таблице: {genTable.Count}");

                // Получаем колонки
                var colNum = genTable.Cols("Num");
                var colName = genTable.Cols("Name");
                var colP = genTable.Cols("P");
                var colQ = genTable.Cols("Q");
                var colQmin = genTable.Cols("Qmin");
                var colQmax = genTable.Cols("Qmax");

                // Попытка найти колонку 'sta'
                dynamic colSta = null;
                try
                {
                    colSta = genTable.Cols("sta");
                    Console.WriteLine("✅ Столбец 'sta' найден и добавлен в экспорт.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Столбец 'sta' не найден ({ex.Message}), продолжаем без него.");
                }

                // Группируем данные по станциям
                List<GeneratorReportData> generators = new List<GeneratorReportData>();

                for (int i = 0; i < genTable.Count; i++)
                {
                    int num = Convert.ToInt32(colNum.Z[i]);

                    // Ищем, какая станция соответствует этому номеру
                    var config = CalculationSettings.Stations.FirstOrDefault(s => s.GeneratorNums.Contains(num));

                    if (config != null)
                    {
                        double qmin = Convert.ToDouble(colQmin.Z[i]);
                        double qmax = Convert.ToDouble(colQmax.Z[i]);
                        double p = Convert.ToDouble(colP?.Z[i] ?? 0);
                        double q = Convert.ToDouble(colQ?.Z[i] ?? 0);

                        int staValue = 0;
                        if (colSta != null)
                        {
                            try { staValue = Convert.ToInt32(colSta.Z[i]); }
                            catch { /* Игнорируем ошибку чтения */ }
                        }

                        string stName = config.Name;

                        generators.Add(new GeneratorReportData
                        {
                            StationName = stName,
                            GeneratorNum = num,
                            Name = colName.Z[i]?.ToString() ?? "",
                            ActivePower = p,
                            ReactivePower = q,
                            Qmin = qmin,
                            Qmax = qmax,
                            StatusFlag = staValue
                        });
                    }
                }

                Console.WriteLine($"✅ Обработано генераторов: {generators.Count} шт.\n");

                // ✅ СТАТИЧЕСКИЙ ВЫЗОВ МЕТОДА БЕЗ CREATE INSTACE!
                ReportManager.ExportCleanDataWithSums(generators, filePath);

                Console.WriteLine($"✅ Данные успешно сохранены в: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка экспорта: {ex.Message}");
                Console.WriteLine($"{ex.StackTrace}");
            }
        }

        /// ✅ МЕТОД ДЛЯ ПОИСКА ТАБЛИЦЫ "ГЕНЕРАТОРЫ" С АЛЬТЕРНАТИВНЫМИ ИМЕНАМИ
        private dynamic FindGeneratorTable()
        {
            string[] possibleNames = new[]
            {
                "Генераторы(УР)",
                "Generator",
                "Генераторы",
                "Генераторы(ПУР)",
                "Генераторы УР",
                "Gens"
            };

            foreach (var tableName in possibleNames)
            {
                try
                {
                    var table = _rastrObj.Tables(tableName);
                    if (table != null && table.Count > 0)
                    {
                        Console.WriteLine($"✅ Найдена таблица: {tableName}");
                        return table;
                    }
                }
                catch
                {
                    continue;
                }
            }

            Console.WriteLine("⚠️ Таблицы генераторов не найдены!");
            throw new Exception("Не удалось найти таблицу 'Генераторы'. Проверьте настройки режима.");
        }

        /// <summary>
        /// Освобождение ресурсов COM-объекта
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed && _rastrObj != null)
            {
                Marshal.ReleaseComObject(_rastrObj);
                _rastrObj = null;
                _isDisposed = true;
            }
        }
    }
}