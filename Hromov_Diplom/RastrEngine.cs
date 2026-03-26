using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Hromov_Diplom.Models;

namespace Hromov_Diplom.ComService
{
    /// <summary>
    /// Движок для работы с RastrWin3 через COM-интерфейс
    /// </summary>
    public class RastrEngine : IDisposable
    {
        /// <summary>
        /// COM-объект RastrWin3
        /// </summary>
        private dynamic? _rastrObj;

        /// <summary>
        /// Флаг освобождения ресурсов
        /// </summary>
        private bool _isDisposed = false;

        /// <summary>
        /// Получение доступа к внутреннему COM-объекту
        /// </summary>
        public dynamic? RawObject => _rastrObj;

        /// <summary>
        /// Инициализация COM-объекта RastrWin3
        /// </summary>
        /// <param name="progId">ProgID COM-объекта (по умолчанию "Astra.Rastr")</param>
        /// <exception cref="Exception">
        /// Возникает, если COM-объект не зарегистрирован в системе
        /// </exception>
        public void Initialize(string progId = "Astra.Rastr")
        {
            Type type = Type.GetTypeFromProgID(progId)
                ?? throw new Exception($"COM-объект '{progId}' не зарегистрирован.");

            _rastrObj = Activator.CreateInstance(type);
        }

        /// <summary>
        /// Загрузка файлов режима из указанной директории или файла
        /// </summary>
        /// <param name="path">Путь к файлу или директории с файлами режима</param>
        /// <exception cref="DirectoryNotFoundException">
        /// Возникает, если указанный путь не существует
        /// </exception>
        public void LoadModeFiles(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                throw new DirectoryNotFoundException($"Путь не найден: {path}");

            if (File.Exists(path))
            {
                _rastrObj?.Load(1, path, "");
            }
            else if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path).OrderBy(f => f))
                {
                    // Пропускаем временные файлы Excel
                    if (!Path.GetFileName(file).StartsWith("~"))
                        _rastrObj?.Load(1, file, "");
                }
            }
        }

        /// <summary>
        /// Зануляет колонку Act в таблице ut_vir, кроме строки с указанным Num
        /// </summary>
        /// <param name="tableName">Имя таблицы в RastrWin3</param>
        /// <param name="columnName">Имя колонки для зануления</param>
        /// <param name="keepNum">Номер строки, которую НЕ нужно занулять</param>
        /// <returns>Количество изменённых строк</returns>
        public int ZeroVirTable(string tableName, string columnName, int keepNum)
        {
            int modifiedCount = 0;

            try
            {
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
                        modifiedCount++;
                    }
                }

                Console.WriteLine(
                    $"  ✓ ut_vir: занулено {modifiedCount} строк (сохранена строка Num={keepNum})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Ошибка при модификации ut_vir: {ex.Message}");
            }

            return modifiedCount;
        }

        /// <summary>
        /// Запуск расчёта МДП (аварийные режимы)
        /// </summary>
        public void CalculateEmergencies()
        {
            _rastrObj?.emergencies("");
        }

        /// <summary>
        /// Проверка наличия результатов расчёта в таблице ut_os_result
        /// </summary>
        /// <returns>
        /// <c>true</c>, если таблица существует и содержит данные;
        /// иначе <c>false</c>
        /// </returns>
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

        /// <summary>
        /// Получение результатов расчёта МДП из таблицы ut_os_result
        /// </summary>
        /// <returns>Объект с результатами расчёта</returns>
        public CalculationResult GetResults()
        {
            var result = new CalculationResult();
            var table = _rastrObj?.Tables("ut_os_result");

            if (table == null)
                return result;

            for (int i = 0; i < table.Count; i++)
            {
                try
                {
                    double v = Convert.ToDouble(table.Cols("pred").Z[i]);
                    if (v != 0)
                        result.Pred = v;

                    v = Convert.ToDouble(table.Cols("padp_real").Z[i]);
                    if (v != 0)
                        result.PadpReal = v;

                    v = Convert.ToDouble(table.Cols("itog_mdp").Z[i]);
                    if (v != 0)
                        result.ItogMdp = v;

                    v = Convert.ToDouble(table.Cols("itog_mdp_pa").Z[i]);
                    if (v != 0)
                        result.ItogMdpPa = v;
                }
                catch
                {
                    // Игнорируем ошибки преобразования отдельных ячеек
                }
            }

            return result;
        }

        /// <summary>
        /// Освобождение COM-ресурсов
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