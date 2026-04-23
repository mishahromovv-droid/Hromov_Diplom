#nullable disable
using Hromov_Diplom.Config;
using Hromov_Diplom.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hromov_Diplom.ComService
{
    public class RastrEngine : IDisposable
    {
        private dynamic _rastrObj;
        private bool _isDisposed = false;
        public dynamic RawObject => _rastrObj;

        public void Initialize(string progId = "Astra.Rastr")
        {
            if (_rastrObj != null)
            {
                try { Marshal.ReleaseComObject(_rastrObj); } catch { }
                _rastrObj = null;
            }

            Type type = Type.GetTypeFromProgID(progId)
                ?? throw new Exception($"COM-объект '{progId}' не зарегистрирован.");
            _rastrObj = Activator.CreateInstance(type);
        }

        public void LoadModeFiles(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path) && !Directory.Exists(path))
                throw new DirectoryNotFoundException($"Путь не найден: {path}");

            if (File.Exists(path))
                _rastrObj?.Load(1, path, "");
            else if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path).OrderBy(f => f).ToList();
                Console.WriteLine($"    Загружено файлов: {files.Count}");
                foreach (string file in files)
                {
                    if (!Path.GetFileName(file).StartsWith("~"))
                    {
                        Console.WriteLine($"      - {Path.GetFileName(file)}");
                        _rastrObj?.Load(1, file, "");
                    }
                }
            }
        }

        public int ZeroVirTable(string tableName, string columnName, int keepNum)
        {
            int modifiedCount = 0;
            try
            {
                var table = _rastrObj?.Tables(tableName);
                if (table == null) return 0;
                var colNum = table.Cols("Num");
                var colAct = table.Cols(columnName);
                for (int i = 0; i < table.Count; i++)
                {
                    int num = Convert.ToInt32(colNum.Z[i]);
                    if (num != keepNum) { colAct.Z[i] = 0; modifiedCount++; }
                }
            }
            catch (Exception ex) { Console.WriteLine($"   Ошибка ut_vir: {ex.Message}"); }
            return modifiedCount;
        }

        public void CalculateEmergencies() => _rastrObj?.emergencies("");

        public bool HasResults()
        {
            try { return _rastrObj?.Tables("ut_os_result")?.Count > 0; }
            catch { return false; }
        }

        public CalculationResult GetResults()
        {
            var result = new CalculationResult();
            var table = _rastrObj?.Tables("ut_os_result");
            if (table == null) return result;

            for (int i = 0; i < table.Count; i++)
            {
                try
                {
                    double v = Convert.ToDouble(table.Cols("pred").Z[i]); if (v != 0) result.Pred = v;
                    v = Convert.ToDouble(table.Cols("padp_real").Z[i]); if (v != 0) result.PadpReal = v;
                    v = Convert.ToDouble(table.Cols("itog_mdp").Z[i]); if (v != 0) result.ItogMdp = v;
                    v = Convert.ToDouble(table.Cols("itog_mdp_pa").Z[i]); if (v != 0) result.ItogMdpPa = v;
                }
                catch { }
            }
            return result;
        }

        public List<GeneratorReportData> ExportGeneratorDataToList()
        {
            Console.WriteLine("    🔍 Поиск таблицы генераторов...");
            var generators = new List<GeneratorReportData>();
            var genTable = FindGeneratorTable();

            Console.WriteLine($"    📊 Строк в таблице: {genTable.Count}");

            var colNum = genTable.Cols("Num");
            var colName = genTable.Cols("Name");
            var colP = genTable.Cols("P");
            var colQ = genTable.Cols("Q");
            var colQmin = genTable.Cols("Qmin");
            var colQmax = genTable.Cols("Qmax");
            dynamic colSta = null;
            try { colSta = genTable.Cols("sta"); Console.WriteLine("    ✅ Столбец 'sta' найден"); }
            catch { Console.WriteLine("    ⚠ Столбец 'sta' не найден"); }

            int matchedCount = 0;
            int totalProcessed = 0;

            for (int i = 0; i < genTable.Count; i++)
            {
                totalProcessed++;
                int num = Convert.ToInt32(colNum.Z[i]);
                var config = CalculationSettings.Stations.FirstOrDefault(s => s.GeneratorNums.Contains(num));

                if (config != null)
                {
                    matchedCount++;
                    generators.Add(new GeneratorReportData
                    {
                        StationName = config.Name,
                        GeneratorNum = num,
                        Name = colName.Z[i]?.ToString() ?? "",
                        ActivePower = Convert.ToDouble(colP?.Z[i] ?? 0),
                        ReactivePower = Convert.ToDouble(colQ?.Z[i] ?? 0),
                        Qmin = Convert.ToDouble(colQmin.Z[i]),
                        Qmax = Convert.ToDouble(colQmax.Z[i]),
                        StatusFlag = colSta != null ? Convert.ToInt32(colSta.Z[i]) : 0
                    });
                }
            }

            Console.WriteLine($"    📋 Всего обработано: {totalProcessed}");
            Console.WriteLine($"    ✅ Совпало со списком станций: {matchedCount}");

            if (matchedCount == 0)
            {
                Console.WriteLine("    ⚠️  ВНИМАНИЕ: Ни один генератор не совпал!");
                Console.WriteLine("    Проверь номера генераторов в режиме и в CalculationSettings");

                // Покажем первые 5 номеров для отладки
                Console.WriteLine("    Первые 5 номеров генераторов в режиме:");
                for (int i = 0; i < Math.Min(5, genTable.Count); i++)
                {
                    int num = Convert.ToInt32(colNum.Z[i]);
                    Console.WriteLine($"      {i + 1}. Num={num}");
                }
            }

            return generators;
        }

        private dynamic FindGeneratorTable()
        {
            string[] possibleNames = new[] { "Generator", "Генераторы", "Генераторы(УР)", "Генераторы(ПУР)", "Генераторы УР", "Gens" };

            foreach (var tableName in possibleNames)
            {
                try
                {
                    var table = _rastrObj.Tables(tableName);
                    if (table != null && table.Count > 0)
                    {
                        Console.WriteLine($"    ✅ Найдена таблица: {tableName}");
                        return table;
                    }
                }
                catch { continue; }
            }

            Console.WriteLine("    ❌ Таблица генераторов НЕ НАЙДЕНА!");
            throw new Exception("Не удалось найти таблицу 'Генераторы'. Проверьте имя таблицы в режиме.");
        }

        public void Dispose()
        {
            if (!_isDisposed && _rastrObj != null)
            {
                try { Marshal.ReleaseComObject(_rastrObj); } catch { }
                _rastrObj = null;
                _isDisposed = true;
            }
        }
    }
}