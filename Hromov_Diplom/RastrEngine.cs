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
    public class RastrEngine : IDisposable
    {
        private dynamic _rastrObj;
        private bool _isDisposed = false;

        public void Initialize(string progId = "Astra.Rastr")
        {
            Type type = Type.GetTypeFromProgID(progId);
            if (type == null) throw new Exception($"COM-объект '{progId}' не зарегистрирован!");
            _rastrObj = Activator.CreateInstance(type);
        }

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

        public void ExportGeneratorDataToCSV()
        {
            try
            {
                // ✅ Убедимся, что директория существует
                if (!Directory.Exists(AppSettings.BaseDirectory))
                    Directory.CreateDirectory(AppSettings.BaseDirectory);

                string filePath = AppSettings.OutputPath;
                Console.WriteLine("⏳ Поиск таблицы генераторов...");
                var genTable = FindGeneratorTable();
                Console.WriteLine($"📊 Количество строк в таблице: {genTable.Count}");

                var colNum = genTable.Cols("Num");
                var colName = genTable.Cols("Name");
                var colP = genTable.Cols("P");
                var colQ = genTable.Cols("Q");
                var colQmin = genTable.Cols("Qmin");
                var colQmax = genTable.Cols("Qmax");

                dynamic colSta = null;
                try { colSta = genTable.Cols("sta"); Console.WriteLine("✅ Столбец 'sta' найден."); }
                catch { Console.WriteLine("⚠ Столбец 'sta' не найден, продолжаем без него."); }

                List<GeneratorReportData> generators = new List<GeneratorReportData>();

                for (int i = 0; i < genTable.Count; i++)
                {
                    int num = Convert.ToInt32(colNum.Z[i]);
                    var config = CalculationSettings.Stations.FirstOrDefault(s => s.GeneratorNums.Contains(num));

                    if (config != null)
                    {
                        double qmin = Convert.ToDouble(colQmin.Z[i]);
                        double qmax = Convert.ToDouble(colQmax.Z[i]);
                        double p = Convert.ToDouble(colP?.Z[i] ?? 0);
                        double q = Convert.ToDouble(colQ?.Z[i] ?? 0);

                        int staValue = 0;
                        if (colSta != null) try { staValue = Convert.ToInt32(colSta.Z[i]); } catch { }

                        generators.Add(new GeneratorReportData
                        {
                            StationName = config.Name,
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
                ReportManager.ExportCleanDataWithSums(generators, filePath);

                // ✅ ИЗМЕНЕНО: Сводная таблица теперь называется Ytog.csv
                string summaryPath = Path.Combine(AppSettings.BaseDirectory, "Ytog_bez_Vzd_bez_utyhelenia.csv");
                ReportManager.ExportStationQSummary(generators, summaryPath);

                Console.WriteLine($"\n✅ Данные успешно сохранены в: {AppSettings.BaseDirectory}");
                Console.WriteLine($"   - {AppSettings.OutputPath}");      // Raschyot_bez_Vzd_bez_utyhelenia.csv
                Console.WriteLine($"   - {summaryPath}");                 // Ytog_bez_Vzd_bez_utyhelenia.csv
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка экспорта: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private dynamic FindGeneratorTable()
        {
            string[] possibleNames = new[] { "Generator", "Генераторы", "Генераторы(УР)", "Генераторы(ПУР)", "Генераторы УР", "Gens" };
            foreach (var tableName in possibleNames)
            {
                try
                {
                    var table = _rastrObj.Tables(tableName);
                    if (table != null && table.Count > 0) { Console.WriteLine($"✅ Найдена таблица: {tableName}"); return table; }
                }
                catch { continue; }
            }
            Console.WriteLine("⚠️ Таблицы генераторов не найдены!");
            throw new Exception("Не удалось найти таблицу 'Генераторы'. Проверьте настройки режима.");
        }

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