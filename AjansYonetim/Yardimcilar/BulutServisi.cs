using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using AjansYonetim.Veritabani;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Yardimcilar
{
    public class YedekModeli
    {
        public string VeritabaniBase64 { get; set; } = string.Empty;
        public string YedekTarihi { get; set; } = string.Empty;
        public string KullaniciAdi { get; set; } = string.Empty;
    }

    /// <summary>
    /// Firebase ile veritabanı yedeği alıp geri yükleme işlemlerini yapan servis.
    /// SQLite Online Backup API kullanarak canlı sistemde (Lock olmadan) veri akışı sağlar.
    /// </summary>
    public static class BulutServisi
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        /// <returns>Başarılıysa true, hata olursa false döner.</returns>
        public static async Task<bool> YedekleAsync()
        {
            try
            {
                var lisansID = LisansYoneticisi.MevcutLisans?.LisansID;
                var ajansAdi = LisansYoneticisi.MevcutLisans?.AjansAdi ?? "Belinmeyen Ajans";

                if (string.IsNullOrWhiteSpace(lisansID))
                    return false;

                var dbYolu = VeritabaniBaglanti.VeritabaniYolu;
                if (!File.Exists(dbYolu))
                    return false;

                var geciciYol = Path.Combine(Path.GetTempPath(), $"kopya_yedek_{Guid.NewGuid():N}.db");
                if (File.Exists(geciciYol)) File.Delete(geciciYol);

                Debug.WriteLine("[BulutServisi] Yedek kopya oluşturuluyor...");
                using (var asilBaglanti = VeritabaniBaglanti.BaglantiAcVeHazirla())
                {
                    using (var yedekBaglanti = new SqliteConnection($"Data Source={geciciYol};Pooling=False"))
                    {
                        yedekBaglanti.Open();
                        Debug.WriteLine("[BulutServisi] BackupDatabase başlatıldı.");
                        asilBaglanti.BackupDatabase(yedekBaglanti);
                        Debug.WriteLine("[BulutServisi] BackupDatabase bitti.");
                    }
                }

                // Temporary DB'yi GZip ile sıkıştırıp Base64'e çevir
                Debug.WriteLine("[BulutServisi] GZip ile sıkıştırılıp Base64'e çevriliyor...");
                var bytes = await File.ReadAllBytesAsync(geciciYol);
                byte[] sıkıştırılmışBytes;
                using (var outStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(outStream, CompressionMode.Compress))
                    {
                        await gzipStream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    sıkıştırılmışBytes = outStream.ToArray();
                }

                var base64 = Convert.ToBase64String(sıkıştırılmışBytes);
                
                try { File.Delete(geciciYol); } catch { }

                Debug.WriteLine($"[BulutServisi] Sıkıştırılmış Payload boyutu: {base64.Length / 1024} KB");

                var yedekModel = new YedekModeli
                {
                    VeritabaniBase64 = base64,
                    YedekTarihi = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    KullaniciAdi = ajansAdi
                };

                var json = JsonSerializer.Serialize(yedekModel);
                var icerik = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Firebase'e gönder (30 saniye timeout ile)
                Debug.WriteLine("[BulutServisi] Firebase PutAsync başlatıldı...");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                var response = await _httpClient.PutAsync($"{Sabitler.FirebaseSabitleri.YEDEKLER_URL}{lisansID}.json", icerik, cts.Token);
                Debug.WriteLine($"[BulutServisi] Firebase PutAsync bitti. Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[BulutServisi] Firebase Yedekleme Hatası: {response.StatusCode} - {response.ReasonPhrase}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("[BulutServisi] Firebase yüklemesi zaman aşımına uğradı (Timeout).");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BulutServisi] Firebase Yedekleme Exception: {ex.Message}");
                App.HataKaydet(ex);
                return false;
            }
        }

        /// <summary>
        /// Buluttaki yedeği bilgisayardaki mevcut veritabanına dönüştürür.
        /// </summary>
        /// <returns>Başarılıysa true, hata olursa veya yedek yoksa false döner.</returns>
        public static async Task<bool> YedektenDonAsync()
        {
            try
            {
                var lisansID = LisansYoneticisi.MevcutLisans?.LisansID;

                if (string.IsNullOrWhiteSpace(lisansID))
                    return false;

                var cevap = await _httpClient.GetStringAsync($"{Sabitler.FirebaseSabitleri.YEDEKLER_URL}{lisansID}.json");

                // Firebase'de veri yoksa cevap "null" string'idir
                if (cevap == "null" || string.IsNullOrWhiteSpace(cevap))
                    return false;

                var yedekModel = JsonSerializer.Deserialize<YedekModeli>(cevap);

                if (yedekModel != null && !string.IsNullOrWhiteSpace(yedekModel.VeritabaniBase64))
                {
                    var sıkıştırılmışBytes = Convert.FromBase64String(yedekModel.VeritabaniBase64);
                    byte[] bytes;

                    // GZip çöz (eski yedeklerde GZip yoksa Exception yiyecektir, yakalayıp düz byte olarak deneriz)
                    try
                    {
                        using (var inStream = new MemoryStream(sıkıştırılmışBytes))
                        {
                            using (var gzipStream = new GZipStream(inStream, CompressionMode.Decompress))
                            {
                                using (var outStream = new MemoryStream())
                                {
                                    await gzipStream.CopyToAsync(outStream);
                                    bytes = outStream.ToArray();
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Sıkıştırılmamış eski stil base64
                        bytes = sıkıştırılmışBytes;
                    }

                    // Geçici bir yere inen DB'yi kaydet
                    var guvenliGeciciYol = Path.Combine(Path.GetTempPath(), $"AjansYonetim_BulutTemp_{Guid.NewGuid():N}.db");
                    await File.WriteAllBytesAsync(guvenliGeciciYol, bytes);

                    try
                    {
                         // İnen Geçici DB'yi Canlı Sisteme Yedekleme (Backup API ile - Sqlite üzerinden)
                         using (var inenDbBaglantisi = new SqliteConnection($"Data Source={guvenliGeciciYol};Pooling=False"))
                         {
                             inenDbBaglantisi.Open();
                             
                             using (var asilBaglanti = VeritabaniBaglanti.BaglantiAcVeHazirla())
                             {
                                  // Kaynaktan (inen DB) -> Hedefe (Canlı sisteme) Backup yönünde restore!
                                  inenDbBaglantisi.BackupDatabase(asilBaglanti); 
                             }
                         }

                         try { File.Delete(guvenliGeciciYol); } catch { }
                         return true;
                    }
                    catch (Exception geriYuklemeHatasi)
                    {
                        App.HataKaydet(geriYuklemeHatasi);
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return false;
            }
        }
        
        /// <summary>
        /// Bulutta mevcut bir yedek olup olmadığını ve tarihini sorgular.
        /// </summary>
        public static async Task<string> BulutYedekTarihiSorgulaAsync()
        {
             try
             {
                 var lisansID = LisansYoneticisi.MevcutLisans?.LisansID;
                 if (string.IsNullOrWhiteSpace(lisansID))
                     return string.Empty;

                 using var yClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                 var cevap = await yClient.GetStringAsync($"{Sabitler.FirebaseSabitleri.YEDEKLER_URL}{lisansID}.json");

                 if (cevap == "null" || string.IsNullOrWhiteSpace(cevap))
                     return string.Empty;

                 var yedekModel = JsonSerializer.Deserialize<YedekModeli>(cevap);
                 return yedekModel?.YedekTarihi ?? string.Empty;
             }
             catch
             {
                 return string.Empty; // İnternet yoksa veya hata varsa boş döner
             }
        }
    }
}
