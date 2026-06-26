using System;
using System.IO;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Uygulama içi tüm dosya kayıt (veritabanı, log, yedek vb.) yollarını merkezi olarak yönetir.
    /// Publish (Yayın) durumunda klasör erişim yetki hataları yaşatmayan (UAC Güvenli)
    /// Windows AppData/Roaming klasörünü kullanır.
    /// </summary>
    public static class DosyaYollari
    {
        private const string KlasorAdi = "AjansYonetim";

        /// <summary>
        /// Uygulamanın veritabanı, yedek ve lisanslarının tutulacağı, 
        /// hiçbir zaman klasör/yazma izni hatasına yakalanmayan güvenli AppData yolu.
        /// </summary>
        public static string UygulamaVeriDizini
        {
            get
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var tamYol = Path.Combine(appDataPath, KlasorAdi);

                // Klasör yoksa oluştur
                if (!Directory.Exists(tamYol))
                {
                    Directory.CreateDirectory(tamYol);
                }

                return tamYol;
            }
        }
    }
}
