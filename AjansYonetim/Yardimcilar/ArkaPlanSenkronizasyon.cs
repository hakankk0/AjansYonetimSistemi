using System;
using System.Threading;
using System.Threading.Tasks;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Veritabanındaki değişiklikleri algılayıp debounce mantığıyla arka planda tek bir kez 
    /// bulut (Firebase) senkronizasyonu yapılmasını sağlayan yönetici sınıf.
    /// </summary>
    public static class ArkaPlanSenkronizasyon
    {
        private static CancellationTokenSource? _beklemeIptalKaynagi;
        private static readonly int _beklemeSuresiMs = 5000; // 5 saniye
        private static bool _senkronizasyonDevamEdiyor = false;

        /// <summary>
        /// Senkronizasyon durumu değiştiğinde tetiklenir (Başladı, Bitti vs).
        /// Parametreler: (bool isSyncing, string mesaj, DateTime? sonBasariliSenkron)
        /// </summary>
        public static event Action<bool, string, DateTime?>? SenkronDurumDegisti;

        public static DateTime? SonBasariliSenkronZamani { get; private set; }

        /// <summary>
        /// Veritabanında (ekleme/güncelleme/silme) yapıldığında çağrılır.
        /// Sayacı sıfırlar ve X saniye sonra arka planda yedeklemeyi tetikler.
        /// </summary>
        public static void DegisiklikBildir()
        {
            // Eğer halihazırda bekleyen bir sayaç varsa iptal et
            _beklemeIptalKaynagi?.Cancel();
            _beklemeIptalKaynagi = new CancellationTokenSource();

            var token = _beklemeIptalKaynagi.Token;

            Task.Run(async () =>
            {
                try
                {
                    // Belirlenen süre kadar (örn 5sn) iptal edilmeden beklerse işlemi başlatır
                    await Task.Delay(_beklemeSuresiMs, token);

                    if (!token.IsCancellationRequested)
                    {
                        await SenkronizasyonuBaslatAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Sayaç iptal edildi (yeni bir değişiklik bildirildiği için) - görmezden gel
                }
            });
        }

        /// <summary>
        /// Yedekleme işlemini senkron şekilde başlatır ve arayüzü bilgilendirir.
        /// </summary>
        private static async Task SenkronizasyonuBaslatAsync()
        {
            if (_senkronizasyonDevamEdiyor) return;

            try
            {
                _senkronizasyonDevamEdiyor = true;
                SenkronDurumDegisti?.Invoke(true, "Buluta Senkronize Ediliyor...", SonBasariliSenkronZamani);

                // Asıl yedekleme işlemini yap
                bool basarili = await BulutServisi.YedekleAsync();

                if (basarili)
                {
                    SonBasariliSenkronZamani = DateTime.Now;
                    SenkronDurumDegisti?.Invoke(false, "Güncel", SonBasariliSenkronZamani);
                }
                else
                {
                    SenkronDurumDegisti?.Invoke(false, "Senkronizasyon Başarısız!", SonBasariliSenkronZamani);
                    
                    // İşlem başarısız olursa ufak bir hata olarak console'a yaz
                    Console.WriteLine("Arka Plan Senkronizasyon Hatası: Buluta yedekleme işlemi false döndü.");
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                SenkronDurumDegisti?.Invoke(false, "Bağlantı Hatası", SonBasariliSenkronZamani);
            }
            finally
            {
                _senkronizasyonDevamEdiyor = false;
            }
        }

        /// <summary>
        /// İlk açılışta son yedek tarihini buluttan çekmek için kullanılabilir.
        /// </summary>
        public static async Task IlkAclistaBulutDurumunuSorgulaAsync()
        {
            SenkronDurumDegisti?.Invoke(true, "Bulut Durumu Sorgulanıyor...", SonBasariliSenkronZamani);
            
            string sonBulutTarihiStr = await BulutServisi.BulutYedekTarihiSorgulaAsync();
            
            if (DateTime.TryParse(sonBulutTarihiStr, out DateTime bulutTarihi))
            {
                SonBasariliSenkronZamani = bulutTarihi;
                SenkronDurumDegisti?.Invoke(false, "Güncel", SonBasariliSenkronZamani);
            }
            else
            {
                SenkronDurumDegisti?.Invoke(false, "Hazır", null);
            }
        }
    }
}
