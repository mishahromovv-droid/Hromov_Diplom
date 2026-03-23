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

        public dynamic RawObject => _rastrObj ?? throw new Exception("RastrWin3 не инициализирован.");

        public void Initialize(string progId = "Astra.Rastr")
        {
            Type type = Type.GetTypeFromProgID(progId);
            if (type == null) throw new Exception($"COM-объект '{progId}' не зарегистрирован.");
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