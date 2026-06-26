using System.IO;

namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Sistem yöneticisi (süper admin) sabitleri.
    /// Yetkili e-posta adresleri ve bypass sabitleri burada tanımlanır.
    /// </summary>
    public static class SistemYoneticisiSabitleri
    {
        /// <summary>
        /// Birincil süper admin (kurucu) e-posta adresi.
        /// </summary>
        public const string SUPER_ADMIN_EMAIL = "hologramss12@gmail.com";

        /// <summary>
        /// Sistem e-posta adresi (SMTP gönderici ve admin bypass).
        /// </summary>
        public const string SISTEM_EMAIL = "ajansyonetimsistemi@gmail.com";

        /// <summary>
        /// Verilen e-posta adresinin süper admin olup olmadığını kontrol eder.
        /// </summary>
        public static bool SuperAdminMi(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var temiz = email.Trim().ToLowerInvariant();
            return temiz == SUPER_ADMIN_EMAIL || temiz == SISTEM_EMAIL;
        }

        // --- YEREL GÜVENLİK (LOCAL SECURITY) BYPASS İŞLEMLERİ ---
        
        /// <summary>
        /// Admin "İçeri Sız" yaptığında SQLite veritabanı kirlenmesin diye
        /// sadece bu bilgisayara özel oluşturulan yerel yetki sertifikası dosyası.
        /// </summary>
        private static string GizliAdminSertifikasi => Path.Combine(Yardimcilar.DosyaYollari.UygulamaVeriDizini, "admin_session.dat");

        /// <summary>
        /// Geçerli bir lokal Admin Bypass dosyasının başlatılmasını sağlar.
        /// </summary>
        public static void YerelAdminSertifikasiOlustur()
        {
            try
            {
                File.WriteAllText(GizliAdminSertifikasi, "SUPER_ADMIN_SESSION_ACTIVE");
            }
            catch { }
        }

        /// <summary>
        /// Çıkış yapıldığında yerel güvenlik sertifikasını yok eder (Güvenlik).
        /// </summary>
        public static void YerelAdminSertifikasiSil()
        {
            try
            {
                if (File.Exists(GizliAdminSertifikasi))
                    File.Delete(GizliAdminSertifikasi);
            }
            catch { }
        }

        /// <summary>
        /// Sadece Cihazda (Uygulama Dizini içinde) aktif bir Admin sertifikası olup olmadığını kontrol eder.
        /// (Veritabanı bağımsız Süper Admin kontrolü)
        /// </summary>
        public static bool YerelAdminBypassGecerliMi()
        {
            return File.Exists(GizliAdminSertifikasi);
        }
    }
}
