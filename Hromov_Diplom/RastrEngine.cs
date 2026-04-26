using Hromov_Diplom.Models;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Hromov_Diplom.ComService
{
    public class RastrEngine : IDisposable
    {
        private dynamic? _rastrObj;
        private bool _isDisposed = false;

        public dynamic? RawObject => _rastrObj;

        public void Initialize(string progId = "Astra.Rastr")
        {
            Type type = Type.GetTypeFromProgID(progId)
                ?? throw new Exception($"COM-объект '{progId}' не зарегистрирован.");
            _rastrObj = Activator.CreateInstance(type);
        }

        /// <summary>
        /// Корректно освобождает COM-объект и сбрасывает состояние
        /// </summary>
        public void Reset()
        {
            if (_rastrObj != null)
            {
                try
                {
                    // Попытка закрыть режим в Rastr
                    _rastrObj?.Close();
                }
                catch { /* Игнорируем ошибки закрытия */ }

                // Освобождаем COM-объект
                try
                {
                    Marshal.ReleaseComObject(_rastrObj);
                }
                catch { /* Игнорируем ошибки освобождения */ }

                _rastrObj = null;
            }

            // Принудительный сборщик мусора для финализации COM-объектов
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void LoadModeFiles(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "Путь не может быть null");

            if (!File.Exists(path) && !Directory.Exists(path))
                throw new DirectoryNotFoundException($"Путь не найден: {path}");

            if (File.Exists(path)) _rastrObj?.Load(1, path, "");
            else if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path).OrderBy(f => f))
                {
                    if (!Path.GetFileName(file).StartsWith("~"))
                        _rastrObj?.Load(1, file, "");
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

                Console.WriteLine($"  ✓ ut_vir: занулено {modifiedCount} строк");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Ошибка: {ex.Message}");
            }

            return modifiedCount;
        }

        public void CalculateEmergencies() => _rastrObj?.emergencies("");

        public bool HasResults()
        {
            try
            {
                var table = _rastrObj?.Tables("ut_os_result");
                return table != null && table.Count > 0;
            }
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
                    double v = Convert.ToDouble(table.Cols("pred").Z[i]);
                    if (v != 0) result.Pred = v;
                    v = Convert.ToDouble(table.Cols("padp_real").Z[i]);
                    if (v != 0) result.PadpReal = v;
                    v = Convert.ToDouble(table.Cols("itog_mdp").Z[i]);
                    if (v != 0) result.ItogMdp = v;
                    v = Convert.ToDouble(table.Cols("itog_mdp_pa").Z[i]);
                    if (v != 0) result.ItogMdpPa = v;
                }
                catch { }
            }

            return result;
        }

        public void Dispose()
        {
            Reset();
            _isDisposed = true;
        }
    }
}