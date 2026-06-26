using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AjansYonetim.Sabitler;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Firebase'den dönen lisans bilgisini temsil eden özel model.
    /// </summary>
    public class FirebaseLisansModel
    {
        public bool aktif_mi { get; set; }
        public string bitis_tarihi { get; set; } = string.Empty;
        public string musteri_adi { get; set; } = string.Empty;
        public string ajans_adi { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lisans bilgilerini temsil eden sınıf.
    /// </summary>
    public class LisansBilgisi
    {
        public string AjansAdi { get; set; } = string.Empty;
        public string SonKullanma { get; set; } = string.Empty;
        public string LisansID { get; set; } = string.Empty;
    }

    /// <summary>
    /// Firebase tabanlı online ve offline (önbellekli) lisans doğrulama yöneticisi.
    /// </summary>
    public static class LisansYoneticisi
    {
        /// <summary>
        /// Firebase Gerçek Zamanlı Veritabanı Lisans URL'si.
        /// </summary>
        private static string FIREBASE_URL => FirebaseSabitleri.LISANSLAR_URL;

        /// <summary>
        /// Offline çalışma koşulu için önbellek dosyası adı.
        /// </summary>
        private const string LISANS_DOSYA_ADI = "lisans_cache.json";

        /// <summary>
        /// Bellekte tutulan mevcut geçerli lisans bilgisi.
        /// </summary>
        private static LisansBilgisi? _mevcutLisans;

        /// <summary>
        /// Mevcut geçerli lisans bilgisini döndürür.
        /// </summary>
        public static LisansBilgisi? MevcutLisans => _mevcutLisans;

        /// <summary>
        /// Lisans dosyasının tam yolunu döndürür.
        /// </summary>
        private static string LisansDosyaYolu =>
            Path.Combine(DosyaYollari.UygulamaVeriDizini, LISANS_DOSYA_ADI);

        /// <summary>
        /// Uygulama ilk açıldığında senkron olarak çağrılan varsayılan doğrulama (App.xaml.cs içinde kullanılır).
        /// İnternetten kontrol edilene kadar arka planda Task bekler.
        /// </summary>
        public static bool LisansGecerliMi()
        {
            var anahtarMetni = LisansDosyasiOku();
            if (string.IsNullOrWhiteSpace(anahtarMetni))
                return false;

            // Arka planda internetten doğrula, internet yoksa önbelleği kullan
            var task = Task.Run(() => LisansDogrulaAsync(anahtarMetni));
            var lisans = task.GetAwaiter().GetResult();

            if (lisans == null)
                return false;

            _mevcutLisans = lisans;
            return true;
        }

        /// <summary>
        /// Lisans anahtarını asenkron olarak Firebase üzerinden doğrular. (Gerçek Lisans Tablosundan)
        /// </summary>
        public static async Task<LisansBilgisi?> LisansDogrulaAsync(string anahtarMetni)
        {
            if (string.IsNullOrWhiteSpace(anahtarMetni)) return null;
            anahtarMetni = anahtarMetni.Trim();

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // 5 saniye bekle
                var cevap = await client.GetStringAsync($"{FIREBASE_URL}{anahtarMetni}.json");

                if (cevap == "null" || string.IsNullOrWhiteSpace(cevap))
                {
                    // Lisans kaydı bulunamadı.
                    return OfflineLisansKontrolu(anahtarMetni);
                }

                var firebaseData = JsonSerializer.Deserialize<FirebaseLisansModel>(cevap);

                if (firebaseData != null && firebaseData.aktif_mi)
                {
                    if (DateTime.TryParse(firebaseData.bitis_tarihi, out DateTime bitis))
                    {
                        if (bitis.Date >= DateTime.Now.Date)
                        {
                            var yeniLisans = new LisansBilgisi
                            {
                                AjansAdi = !string.IsNullOrEmpty(firebaseData.ajans_adi) ? firebaseData.ajans_adi : firebaseData.musteri_adi,
                                LisansID = anahtarMetni,
                                SonKullanma = firebaseData.bitis_tarihi
                            };
                            _mevcutLisans = yeniLisans;
                            return yeniLisans;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // İNTERNET YOK (Timeout vs) VEYA AĞ HATASI — Önbellekten çalışmasına izin ver
                return OfflineLisansKontrolu(anahtarMetni);
            }
            return null;
        }

        /// <summary>
        /// Admin tarafından üretilen CD-Key'i (Lisans Kodunu) kullanır. Doğruysa ajansın lisans süresini uzatıp geri döner.
        /// </summary>
        public static async Task<LisansBilgisi?> KoduKullanVeLisansUzatAsync(string ajansId, string ajansAdi, string email, string cdKey)
        {
            if (string.IsNullOrWhiteSpace(cdKey) || string.IsNullOrWhiteSpace(ajansId)) return null;
            cdKey = cdKey.Trim().ToUpper();

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                // 1. Kodu Bul
                var keyCevap = await client.GetStringAsync($"{FirebaseSabitleri.URETILEN_KODLAR_URL}{cdKey}.json");
                if (keyCevap == "null" || string.IsNullOrWhiteSpace(keyCevap))
                    return null; // Öyle bir kod yok
                    
                var kodModel = JsonSerializer.Deserialize<FirebaseUretilenKodModel>(keyCevap);
                if (kodModel == null || !kodModel.aktif_mi)
                    return null; // Kod aktif/geçerli değil
                    
                // 2. Kodu YAK (Sil/Tek kullanımlık)
                await client.DeleteAsync($"{FirebaseSabitleri.URETILEN_KODLAR_URL}{cdKey}.json");
                
                // 3. Mevcut lisansı kontrol et
                var mevcutLisansCevap = await client.GetStringAsync($"{FIREBASE_URL}{ajansId}.json");
                DateTime baslangicTarihi = DateTime.Now.Date;
                
                if (mevcutLisansCevap != "null" && !string.IsNullOrWhiteSpace(mevcutLisansCevap))
                {
                    var varolanLisans = JsonSerializer.Deserialize<FirebaseLisansModel>(mevcutLisansCevap);
                    if (varolanLisans != null && DateTime.TryParse(varolanLisans.bitis_tarihi, out var eskiBitis))
                    {
                        if (eskiBitis > DateTime.Now.Date)
                        {
                            baslangicTarihi = eskiBitis; // Süresi varsa üzerine ekle
                        }
                    }
                }
                
                // 4. Yeni tarihi hesapla (Limitsiz için kodModel.ay_suresi 120 (10 yıl) olarak verilecek)
                var yeniBitis = baslangicTarihi.AddMonths(kodModel.ay_suresi).ToString("yyyy-MM-dd");
                
                // 5. Yeni lisansı kaydet
                var firebaseLisansPayload = new FirebaseLisansModel
                {
                    aktif_mi = true,
                    ajans_adi = string.IsNullOrEmpty(ajansAdi) ? "Bilinmeyen Ajans" : ajansAdi,
                    musteri_adi = string.IsNullOrEmpty(email) ? "bilinmeyen@email.com" : email,
                    bitis_tarihi = yeniBitis
                };
                
                var content = new StringContent(JsonSerializer.Serialize(firebaseLisansPayload), System.Text.Encoding.UTF8, "application/json");
                var putRes = await client.PutAsync($"{FIREBASE_URL}{ajansId}.json", content);
                
                if (putRes.IsSuccessStatusCode)
                {
                    var guncelLisans = new LisansBilgisi
                    {
                         AjansAdi = firebaseLisansPayload.ajans_adi,
                         LisansID = ajansId,
                         SonKullanma = yeniBitis
                    };
                    return guncelLisans;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CD-Key hatası: {ex.Message}");
            }
            return null;
        }


        /// <summary>
        /// İnternet bağlantısı yoksa, en son doğrulanan cihaz içi önbelleği kontrol eder.
        /// "Kullanıcının daha önce onaylanmış lisansı varsa offline açılması" kuralını işletir.
        /// </summary>
        private static LisansBilgisi? OfflineLisansKontrolu(string anahtarMetni)
        {
            try
            {
                if (!File.Exists(LisansDosyaYolu)) return null;

                var dosyaIcerik = File.ReadAllText(LisansDosyaYolu);
                var cached = JsonSerializer.Deserialize<LisansBilgisi>(dosyaIcerik);

                // Anahtar aynı mı kontrolü
                if (cached != null && cached.LisansID == anahtarMetni)
                {
                    if (DateTime.TryParse(cached.SonKullanma, out var bitisTarihi))
                    {
                        if (bitisTarihi.Date >= DateTime.Now.Date)
                        {
                            _mevcutLisans = cached;
                            return cached;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex) { App.HataKaydet(ex); return null; }
        }

        public static string LisansDosyasiOku()
        {
            try
            {
                if (File.Exists(LisansDosyaYolu))
                {
                    var dosyaIcerik = File.ReadAllText(LisansDosyaYolu);
                    var cached = JsonSerializer.Deserialize<LisansBilgisi>(dosyaIcerik);
                    return cached?.LisansID ?? string.Empty;
                }
            }
            catch (Exception ex) { App.HataKaydet(ex); }

            return string.Empty;
        }

        /// <summary>
        /// Doğrulanmış lisans bilgisini Offline önbellek için dosyaya JSON formatında kaydeder.
        /// </summary>
        public static void LisansDosyasiKaydet(LisansBilgisi lisans)
        {
            try
            {
                var jsonMetni = JsonSerializer.Serialize(lisans);
                File.WriteAllText(LisansDosyaYolu, jsonMetni);
                
                // HATA DÜZELTMESİ: Bellekteki canlı referansı da güncelle ki uygulamayı kapatıp açmaya gerek kalmasın.
                _mevcutLisans = lisans;
            }
            catch (Exception ex) { App.HataKaydet(ex); }
        }
    }

    /// <summary>
    /// UretilenLisansKodlari düğümü için Firebase'deki CD-Key json modelini tutar
    /// </summary>
    public class FirebaseUretilenKodModel
    {
        public bool aktif_mi { get; set; }
        public int ay_suresi { get; set; }
        public string olusturulma_tarihi { get; set; } = string.Empty;
    }
}
