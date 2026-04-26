using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hromov_Diplom
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

            for (int i = 0; i < genTable.Count; i++)
            {
                generators.Add(new GeneratorReportData
                {
                    StationName = "Неизвестно",
                    GeneratorNum = Convert.ToInt32(colNum.Z[i]),
                    Name = colName.Z[i]?.ToString()?.Trim() ?? "",
                    ActivePower = Convert.ToDouble(colP?.Z[i] ?? 0),
                    ReactivePower = Convert.ToDouble(colQ?.Z[i] ?? 0),
                    Qmin = Convert.ToDouble(colQmin.Z[i]),
                    Qmax = Convert.ToDouble(colQmax.Z[i]),
                    StatusFlag = colSta != null ? Convert.ToInt32(colSta.Z[i]) : 0
                });
            }
            Console.WriteLine($"    📋 Экспортировано генераторов: {generators.Count}");
            return generators;
        }

        private dynamic FindGeneratorTable()
        {
            string[] names = { "Generator", "Генераторы", "Генераторы(УР)", "Генераторы(ПУР)", "Gens" };
            foreach (var tableName in names)
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
            throw new Exception("Не удалось найти таблицу 'Генераторы'.");
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