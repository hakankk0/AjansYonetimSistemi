using System;
using System.IO;

namespace AjansYonetim.Yardimcilar
{
    public static class CihazYardimcisi
    {
        private static string _cihazIdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "AjansYonetim", 
            "cihaz.id");

        public static string GetCihazId()
        {
            try
            {
                if (File.Exists(_cihazIdPath))
                {
                    var id = File.ReadAllText(_cihazIdPath).Trim();
                    if (!string.IsNullOrEmpty(id))
                        return id;
                }

                var dir = Path.GetDirectoryName(_cihazIdPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                string yeniCihazId = Guid.NewGuid().ToString("N");
                File.WriteAllText(_cihazIdPath, yeniCihazId);
                return yeniCihazId;
            }
            catch
            {
                // Okuma/Yazma izni olmaması durumunda fallback
                return Guid.NewGuid().ToString("N");
            }
        }
    }
}
