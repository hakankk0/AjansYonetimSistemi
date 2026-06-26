using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AjansYonetim.Pencereler;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;

namespace AjansYonetim
{
    /// <summary>
    /// Uygulama başlangıç noktası.
    /// Veritabanını başlatır, lisans kontrolü yapar ve global hata yönetimini kurar.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Hata log dosyası adı.
        /// </summary>
        private const string HataLogDosyasi = "hata_log.txt";

        /// <summary>
        /// Maksimum log dosyası boyutu (byte).
        /// </summary>
        private const long MaksimumLogBoyutu = 5 * 1024 * 1024; // 5 MB

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global hata yakalayıcıları kur
            DispatcherUnhandledException += UygulamaHataYakalayici;
            AppDomain.CurrentDomain.UnhandledException += AlanHataYakalayici;

            // Veritabanı tablolarını oluştur (yoksa)
            VeritabaniBaglanti.VeritabaniBaslat();

            // Arka planda döviz kurlarını güncel tutan servisi başlat
            DovizKurServisi.BaslatArkaPlanSenkronizasyonu();

            // 2 Günlük "Beni Hatırla" Kontrolü
            bool beniHatirlaGecerli = false;
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                komut.CommandText = "SELECT Deger FROM Ayarlar WHERE Anahtar = 'SonGirisTarihi'";
                var sonuc = komut.ExecuteScalar()?.ToString();
                if (!string.IsNullOrWhiteSpace(sonuc) && DateTime.TryParse(sonuc, out var songiris))
                {
                    if ((DateTime.Now - songiris).TotalDays <= 2 && LisansYoneticisi.LisansGecerliMi())
                    {
                        beniHatirlaGecerli = true;
                    }
                }
            }
            catch { /* İlk girişte veritabanı veya ayar yoksa sessizce geç */ }

            if (beniHatirlaGecerli)
            {
                // Geçerli oturum varsa direkt ana pencereyi aç
                var anaPencere = new AnaPencere();
                anaPencere.Show();
            }
            else
            {
                // Oturum yoksa veya süresi (2 gün) dolduysa Şifre ekranını aç
                var girisPenceresi = new GirisPenceresi();
                girisPenceresi.Show();
            }
        }

        /// <summary>
        /// UI thread'deki yakalanmamış hataları yakalar.
        /// </summary>
        private void UygulamaHataYakalayici(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HataKaydet(e.Exception);

#if DEBUG
            OnayDiyalogu.Hata(
                $"Beklenmeyen bir hata oluştu:\n\n{e.Exception.Message}\n\nDetay: {e.Exception.StackTrace}",
                "Hata (Debug)");
#else
            OnayDiyalogu.Uyari(
                "Beklenmeyen bir hata oluştu. Uygulama çalışmaya devam edecek.\nDetaylar hata_log.txt dosyasına kaydedildi.",
                "Hata");
#endif

            e.Handled = true;
        }

        /// <summary>
        /// Tüm AppDomain'deki yakalanmamış hataları yakalar.
        /// </summary>
        private void AlanHataYakalayici(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HataKaydet(ex);
            }
        }

        /// <summary>
        /// Hatayı log dosyasına yazar.
        /// Log dosyası belirli boyutu aşarsa otomatik temizlenir.
        /// </summary>
        public static void HataKaydet(Exception ex)
        {
            try
            {
                var logYolu = Path.Combine(DosyaYollari.UygulamaVeriDizini, HataLogDosyasi);

                // Log dosyası çok büyükse arşivle (rotasyon)
                if (File.Exists(logYolu) && new FileInfo(logYolu).Length > MaksimumLogBoyutu)
                {
                    var arsivAdi = Path.Combine(
                        DosyaYollari.UygulamaVeriDizini,
                        $"hata_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    File.Move(logYolu, arsivAdi);

                    // En fazla 3 arşiv dosyası tut
                    const int maksimumArsivSayisi = 3;
                    var arsivler = Directory.GetFiles(
                            DosyaYollari.UygulamaVeriDizini, "hata_log_*.txt")
                        .OrderByDescending(f => f)
                        .Skip(maksimumArsivSayisi)
                        .ToArray();

                    foreach (var eskiArsiv in arsivler)
                    {
                        File.Delete(eskiArsiv);
                    }
                }

                var logSatiri = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(logYolu, logSatiri);
            }
            catch
            {
                // Log yazılamadıysa sessizce geç
            }
        }
    }
}
