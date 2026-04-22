#nullable disable 

using Hromov_Diplom.Config;
using Hromov_Diplom.Models;
using Hromov_Diplom.Reports;
using System;
using System.Collections.Generic;
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

            if (File.Exists(path)) _rastrObj.Load(1, path, "");
            else if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path).OrderBy(f => f))
                    if (!Path.GetFileName(file).StartsWith("~")) _rastrObj.Load(1, file, "");
            }
        }

        public void ZeroOutVirtualRegulators(int excludeVirNum = 54)
        {
            dynamic virTable = null;
            string foundName = null;
            var keywords = new[] { "ВИР", "Virtual", "Regulator", "Vir", "VR", "Вирт", "Регулятор", "VirtReg" };

            // 🔍 Автопоиск таблицы ВИР через COM-коллекцию
            try
            {
                var tablesCol = _rastrObj.Tables;
                int count = tablesCol.Count;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        string name = tablesCol(i).Name;
                        if (keywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            foundName = name;
                            break;
                        }
                    }
                    catch
                    {
                        try
                        {
                            string name = tablesCol[i].Name;
                            if (keywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                foundName = name;
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // 🔄 Фолбэк: прямой перебор имён
            if (foundName == null)
            {
                string[] fallbackNames = { "ВИР", "Виртуальные регуляторы", "Vir", "VR", "VIR_UR", "VirtualRegulators", "Регуляторы", "Regulators", "ВИР(УР)", "ВиртРег", "VirtualReg", "Регуляторы(В)", "Virtual_Regulators", "ВР", "VR_Table" };
                foreach (var name in fallbackNames)
                {
                    try
                    {
                        var t = _rastrObj.Tables(name);
                        if (t != null && t.Count > 0) { foundName = name; break; }
                    }
                    catch { }
                }
            }

            if (foundName == null) return;
            try { virTable = _rastrObj.Tables(foundName); } catch { return; }

            dynamic colNum = null, colP = null, colQ = null;
            foreach (var c in new[] { "Num", "NUM", "num", "№", "ID", "Number" }) try { colNum = virTable.Cols(c); if (colNum != null) break; } catch { }
            foreach (var c in new[] { "P", "P_MW", "p", "Power", "Pgen" }) try { colP = virTable.Cols(c); if (colP != null) break; } catch { }
            foreach (var c in new[] { "Q", "Q_MVAR", "q", "Reactive", "Qgen" }) try { colQ = virTable.Cols(c); if (colQ != null) break; } catch { }

            if (colNum == null || colP == null || colQ == null) return;

            for (int i = 0; i < virTable.Count; i++)
            {
                try
                {
                    int num = Convert.ToInt32(colNum.Z[i]);
                    if (num != excludeVirNum) { colP.Z[i] = 0.0; colQ.Z[i] = 0.0; }
                }
                catch { }
            }
            try { _rastrObj.Save(1, "", ""); } catch { }
        }

        public void CalculateMDP()
        {
            // ✅ ПРАВИЛЬНЫЙ ВЫЗОВ МДП (из кода твоего друга)
            try
            {
                _rastrObj.emergencies("");
            }
            catch
            {
                // Если emergencies не сработает, пробуем запасные варианты
                try { _rastrObj.Calc(0); }
                catch { try { _rastrObj.Calc(1); } catch { } }
            }
        }

        public void ExportGeneratorDataToCSV()
        {
            try
            {
                string outDir = Path.GetDirectoryName(AppSettings.OutputPath) ?? ".";
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                string filePath = AppSettings.OutputPath;
                var genTable = FindGeneratorTable();

                var colNum = genTable.Cols("Num");
                var colName = genTable.Cols("Name");
                var colP = genTable.Cols("P");
                var colQ = genTable.Cols("Q");
                var colQmin = genTable.Cols("Qmin");
                var colQmax = genTable.Cols("Qmax");

                dynamic colSta = null;
                try { colSta = genTable.Cols("sta"); } catch { }

                List<GeneratorReportData> generators = new List<GeneratorReportData>();

                for (int i = 0; i < genTable.Count; i++)
                {
                    int num = Convert.ToInt32(colNum.Z[i]);
                    var config = CalculationSettings.Stations.FirstOrDefault(s => s.GeneratorNums.Contains(num));

                    if (config != null)
                    {
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

                ReportManager.ExportCleanDataWithSums(generators, filePath);

                // ✅ ИЗМЕНЕНО: Сводная таблица теперь называется Ytog.csv и сохраняется в BaseDirectory
                string summaryPath = Path.Combine(AppSettings.BaseDirectory, "Ytog_Utythelenie_bez_Vzd.csv");
                ReportManager.ExportStationQSummary(generators, summaryPath);

                Console.WriteLine($"\n✅ Данные сохранены в: {AppSettings.BaseDirectory}");
                Console.WriteLine($"   - Raschyot.csv (детальные данные)");
                Console.WriteLine($"   - Ytog.csv (сводная таблица)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка экспорта: {ex.Message}");
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
                    if (table != null && table.Count > 0) return table;
                }
                catch { continue; }
            }
            throw new Exception("Таблица генераторов не найдена!");
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