using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hromov_Diplom
{
    public class RastrEngine : IDisposable
    {
        private dynamic _rastrObj;
        private bool _disposed;

        public dynamic RawObject
        {
            get { return _rastrObj ?? throw new Exception("RastrWin3 не инициализирован."); }
        }

        public void Initialize(string progId = "Astra.Rastr")
        {
            string[] possibleIds = { "Astra.Rastr", "RASTRA.Astra", "Astra.RastrWin3", "RASTRA.RastrWin3" };

            foreach (string id in possibleIds)
            {
                Type type = Type.GetTypeFromProgID(id);
                if (type != null)
                {
                    _rastrObj = Activator.CreateInstance(type);
                    Console.WriteLine($"✓ COM-объект '{id}' найден и запущен!");
                    return;
                }
            }

            throw new Exception($"COM-объект 'Astra.Rastr' не зарегистрирован в системе.\nУбедитесь, что программа RASTRA запущена и её версия поддерживается.");
        }

        public void LoadModeFiles(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                throw new DirectoryNotFoundException($"Путь не найден: {path}");

            if (File.Exists(path))
            {
                _rastrObj?.Load(1, path, "");
                return;
            }

            foreach (string file in Directory.GetFiles(path).OrderBy(f => f))
            {
                if (!Path.GetFileName(file).StartsWith("~"))
                    _rastrObj?.Load(1, file, "");
            }
        }

        public int ZeroVirTable(string tableName, string columnName, int keepNum)
        {
            int modified = 0;
            var table = _rastrObj?.Tables(tableName);
            if (table == null) return 0;

            var colNum = table.Cols("Num");
            var colAct = table.Cols(columnName);

            for (int i = 0; i < table.Count; i++)
            {
                int num = Convert.ToInt32(colNum.Z[i]);
                if (num != keepNum)
                {
                    colAct.Z[i] = 0;
                    modified++;
                }
            }

            Console.WriteLine($"  ✓ ut_vir: занулено {modified} строк (сохранена строка Num={keepNum})");
            return modified;
        }

        public void SolveBaseMode()
        {
            if (_rastrObj != null) _rastrObj.rgm("");
        }

        public double GetSectionFlow(int sectionNum)
        {
            try
            {
                if (_rastrObj == null) return 0.0;
                var t = _rastrObj.Tables("sechen");
                if (t == null) return 0.0;
                var colNs = t.Cols("ns");
                var colPsech = t.Cols("psech");

                for (int i = 0; i < t.Count; i++)
                {
                    int ns = Convert.ToInt32(colNs.Z[i]);
                    if (ns == sectionNum)
                        return Convert.ToDouble(colPsech.Z[i]);
                }
                return 0.0;
            }
            catch { return 0.0; }
        }

        public void CalculateEmergencies()
        {
            if (_rastrObj != null) _rastrObj.emergencies("");
        }

        public bool HasResults()
        {
            try
            {
                if (_rastrObj == null) return false;
                var table = _rastrObj.Tables("ut_os_result");
                return table != null && table.Count > 0;
            }
            catch { return false; }
        }

        public CalculationResult ReadResults()
        {
            var result = new CalculationResult();

            try
            {
                if (_rastrObj == null) return result;

                var table = _rastrObj.Tables("ut_os_result");
                if (table == null || table.Count == 0) return result;

                var columnNames = new[] { "pred", "padp_real", "itog_mdp", "itog_mdp_pa" };

                foreach (var colName in columnNames)
                {
                    double maxVal = -999999.0;
                    var col = table.Cols(colName);

                    for (int i = 0; i < table.Count; i++)
                    {
                        double val = TryGetDoubleValue(col, i);
                        if (val > maxVal)
                            maxVal = val;
                    }

                    if (maxVal > -100000.0)
                    {
                        switch (colName)
                        {
                            case "pred": result.Pred = maxVal; break;
                            case "padp_real": result.PadpReal = maxVal; break;
                            case "itog_mdp": result.ItogMdp = maxVal; break;
                            case "itog_mdp_pa": result.ItogMdpPa = maxVal; break;
                        }
                    }
                }
            }
            catch { /* Пропускаем ошибки */ }

            return result;
        }

        private static double TryGetDoubleValue(dynamic col, int row)
        {
            try
            {
                object obj = col.Z[row];
                if (obj == null) return 0.0;

                string str = Convert.ToString(obj).Trim();
                if (string.IsNullOrEmpty(str) || str == "-" || str == "Н/Д" || str == "---")
                    return 0.0;

                return Convert.ToDouble(str);
            }
            catch
            {
                return 0.0;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_rastrObj != null)
            {
                try { Marshal.ReleaseComObject(_rastrObj); } catch { }
                _rastrObj = null;
            }
            _disposed = true;
        }
    }
}