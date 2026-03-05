#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hromov_Diplom
{
    /// <summary>Обёртка над COM RastrWin3</summary>
    public class RastrEngine : IDisposable
    {
        private dynamic? _rastrObj;
        private bool _disposed;

        public dynamic RawObject => _rastrObj ?? throw new Exception("RastrWin3 не инициализирован.");

        public void Initialize(string progId = "Astra.Rastr")
        {
            Type type = Type.GetTypeFromProgID(progId)
                ?? throw new Exception($"COM-объект '{progId}' не зарегистрирован.");

            _rastrObj = Activator.CreateInstance(type);
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
            if (table == null)
            {
                Console.WriteLine($"  ⚠ Таблица {tableName} не найдена");
                return 0;
            }

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

        /// <summary>Расчёт базового режима (без утяжеления)</summary>
        public void SolveBaseMode()
        {
            _rastrObj?.rgm("");
        }

        /// <summary>Pраб: таблица sechen, ns=номер сечения, столбец psech</summary>
        public double? GetSectionFlow(int sectionNum)
        {
            try
            {
                var t = _rastrObj?.Tables("sechen");
                if (t == null) return null;

                var colNs = t.Cols("ns");
                var colPsech = t.Cols("psech");

                for (int i = 0; i < t.Count; i++)
                {
                    int ns = Convert.ToInt32(colNs.Z[i]);
                    if (ns == sectionNum)
                        return Convert.ToDouble(colPsech.Z[i]);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Запуск расчёта МДП/аварий</summary>
        public void CalculateEmergencies()
        {
            _rastrObj?.emergencies("");
        }

        public bool HasResults()
        {
            try
            {
                var table = _rastrObj?.Tables("ut_os_result");
                return table != null && table.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Читаем ut_os_result</summary>
        public CalculationResult ReadResults()
        {
            var result = new CalculationResult();

            try
            {
                var table = _rastrObj?.Tables("ut_os_result");
                if (table == null || table.Count == 0) return result;

                // Берём первую строку (обычно там нужные итоги)
                int i = 0;

                result.Pred = TryRead(table, "pred", i);
                result.PadpReal = TryRead(table, "padp_real", i);
                result.ItogMdp = TryRead(table, "itog_mdp", i);
                result.ItogMdpPa = TryRead(table, "itog_mdp_pa", i);

                return result;
            }
            catch
            {
                return result;
            }
        }

        private static double? TryRead(dynamic table, string col, int row)
        {
            try
            {
                double v = Convert.ToDouble(table.Cols(col).Z[row]);
                return v;
            }
            catch
            {
                return null;
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