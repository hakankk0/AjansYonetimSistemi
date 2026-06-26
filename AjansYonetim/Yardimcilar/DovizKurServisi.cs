using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Hibrit döviz kuru servisi.
    /// Öncelik: Frankfurter API → TCMB XML → Veritabanı (son kur).
    /// </summary>
    public static class DovizKurServisi
    {
        /// <summary>
        /// HTTP istemcisi — uygulama genelinde tekil kullanım.
        /// </summary>
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Bellekte tutulan güncel kurlar (cache).
        /// </summary>
        private static decimal _usdKur;
        private static decimal _eurKur;
        private static DateTime _sonGuncelleme = DateTime.MinValue;

        /// <summary>
        /// Cache süresi — aynı saat içinde tekrar API çağrısı yapılmaz.
        /// </summary>
        private static readonly TimeSpan CACHE_SURESI = TimeSpan.FromHours(1);

        /// <summary>
        /// Kurlar en az bir kez yüklendi mi?
        /// </summary>
        public static bool KurlarYuklendi => _usdKur > 0 && _eurKur > 0;

        /// <summary>
        /// Güncel USD kuru (1 USD = ? TL).
        /// </summary>
        public static decimal UsdKur => _usdKur;

        /// <summary>
        /// Güncel EUR kuru (1 EUR = ? TL).
        /// </summary>
        public static decimal EurKur => _eurKur;

        /// <summary>
        /// Kurları günceller. Hibrit sıra: Frankfurter → TCMB → Veritabanı.
        /// </summary>
        public static async Task KurlariGuncelleAsync(bool zorlaGuncelle = false)
        {
            // Cache kontrolü — zorla güncelleme yoksa 1 saat içinde tekrar çağrılırsa atla
            if (!zorlaGuncelle && (DateTime.Now - _sonGuncelleme) < CACHE_SURESI && KurlarYuklendi)
                return;

            decimal eskiUsd = _usdKur;
            decimal eskiEur = _eurKur;

            bool basarili = false;

            // 1. Frankfurter API (öncelik)
            if (await FrankfurterdenCekAsync())
            {
                basarili = true;
            }
            // 2. TCMB XML (yedek)
            else if (await TCMBdenCekAsync())
            {
                basarili = true;
            }
            // 3. Veritabanından son kur (son çare)
            else
            {
                VeritabanindanYukle();
                basarili = true;
            }

            if (basarili)
            {
                KurlariKaydet();

                // Kur Şoku (Dalgalanma) Kontrolü — Asistan Bildirimi
                if (eskiUsd > 0 && eskiEur > 0)
                {
                    decimal usdDegisim = Math.Abs((_usdKur - eskiUsd) / eskiUsd) * 100;
                    decimal eurDegisim = Math.Abs((_eurKur - eskiEur) / eskiEur) * 100;

                    if (usdDegisim >= 2.0m || eurDegisim >= 2.0m)
                    {
                        string mesaj = "📈 DİKKAT KUR DALGALANMASI: ";
                        if (usdDegisim >= 2.0m) mesaj += $"Dolar kuru son kontrolden bu yana %{usdDegisim:F1} değişerek {_usdKur:F2} TL oldu. ";
                        if (eurDegisim >= 2.0m) mesaj += $"Euro kuru %{eurDegisim:F1} değişerek {_eurKur:F2} TL oldu.";

                        BildirimIslemleri.BildirimEkle(new Modeller.Bildirim
                        {
                            CalisanID = null,
                            Mesaj = mesaj.Trim(),
                            OlusturmaTarihi = DateTime.Now
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Frankfurter API'sinden kur çeker (JSON).
        /// Kayıt gerektirmez, sınırsız istek.
        /// </summary>
        private static async Task<bool> FrankfurterdenCekAsync()
        {
            try
            {
                // USD → TRY
                var usdUrl = string.Format(ParaBirimleri.FRANKFURTER_API_URL, ParaBirimleri.USD);
                var usdYanit = await _httpClient.GetStringAsync(usdUrl);
                var usdKur = FrankfurterJsonParsele(usdYanit);

                // EUR → TRY
                var eurUrl = string.Format(ParaBirimleri.FRANKFURTER_API_URL, ParaBirimleri.EUR);
                var eurYanit = await _httpClient.GetStringAsync(eurUrl);
                var eurKur = FrankfurterJsonParsele(eurYanit);

                if (usdKur > 0 && eurKur > 0)
                {
                    _usdKur = usdKur;
                    _eurKur = eurKur;
                    _sonGuncelleme = DateTime.Now;
                    return true;
                }
            }
            catch (Exception)
            {
                // Frankfurter erişilemedi — TCMB'ye geç
            }

            return false;
        }

        /// <summary>
        /// Frankfurter JSON yanıtından TRY kurunu parse eder.
        /// Format: {"amount":1,"base":"USD","date":"2026-03-27","rates":{"TRY":44.37}}
        /// </summary>
        private static decimal FrankfurterJsonParsele(string json)
        {
            try
            {
                // Basit JSON parse — System.Text.Json bağımlılığı eklemeden
                var tryIndex = json.IndexOf("\"TRY\"", StringComparison.Ordinal);
                if (tryIndex < 0) return 0;

                var colonIndex = json.IndexOf(':', tryIndex);
                if (colonIndex < 0) return 0;

                var endIndex = json.IndexOfAny(new[] { '}', ',' }, colonIndex);
                if (endIndex < 0) return 0;

                var degerStr = json.Substring(colonIndex + 1, endIndex - colonIndex - 1).Trim();

                if (decimal.TryParse(degerStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var kur))
                    return kur;
            }
            catch (Exception)
            {
                // Parse hatası
            }

            return 0;
        }

        /// <summary>
        /// TCMB XML API'sinden kur çeker (yedek kaynak).
        /// </summary>
        private static async Task<bool> TCMBdenCekAsync()
        {
            try
            {
                var xmlIcerik = await _httpClient.GetStringAsync(ParaBirimleri.TCMB_API_URL);
                var doc = XDocument.Parse(xmlIcerik);

                decimal usdKur = 0, eurKur = 0;

                foreach (var currency in doc.Descendants("Currency"))
                {
                    var kod = currency.Attribute("CurrencyCode")?.Value;
                    var forexSelling = currency.Element("ForexSelling")?.Value;

                    if (string.IsNullOrWhiteSpace(forexSelling)) continue;

                    // TCMB nokta kullanır (44.2887)
                    if (!decimal.TryParse(forexSelling, NumberStyles.Any, CultureInfo.InvariantCulture, out var kur))
                        continue;

                    switch (kod)
                    {
                        case ParaBirimleri.USD:
                            usdKur = kur;
                            break;
                        case ParaBirimleri.EUR:
                            eurKur = kur;
                            break;
                    }

                    if (usdKur > 0 && eurKur > 0) break;
                }

                if (usdKur > 0 && eurKur > 0)
                {
                    _usdKur = usdKur;
                    _eurKur = eurKur;
                    _sonGuncelleme = DateTime.Now;
                    return true;
                }
            }
            catch (Exception)
            {
                // TCMB erişilemedi
            }

            return false;
        }

        /// <summary>
        /// Veritabanındaki son başarılı kurları yükler (son çare).
        /// </summary>
        private static void VeritabanindanYukle()
        {
            try
            {
                var usdStr = AyarIslemleri.AyarGetir(ParaBirimleri.AYAR_SON_USD_KUR, "0");
                var eurStr = AyarIslemleri.AyarGetir(ParaBirimleri.AYAR_SON_EUR_KUR, "0");

                if (decimal.TryParse(usdStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var usd) && usd > 0)
                    _usdKur = usd;

                if (decimal.TryParse(eurStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var eur) && eur > 0)
                    _eurKur = eur;

                if (_usdKur > 0 && _eurKur > 0)
                    _sonGuncelleme = DateTime.Now;
            }
            catch (Exception)
            {
                // Veritabanı hatası — kurlar 0 kalır
            }
        }

        /// <summary>
        /// Güncel kurları veritabanına kaydeder (sonraki fallback için).
        /// </summary>
        private static void KurlariKaydet()
        {
            try
            {
                AyarIslemleri.AyarKaydet(ParaBirimleri.AYAR_SON_USD_KUR,
                    _usdKur.ToString(CultureInfo.InvariantCulture));
                AyarIslemleri.AyarKaydet(ParaBirimleri.AYAR_SON_EUR_KUR,
                    _eurKur.ToString(CultureInfo.InvariantCulture));
                AyarIslemleri.AyarKaydet(ParaBirimleri.AYAR_SON_KUR_TARIHI,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception)
            {
                // Kayıt hatası — kritik değil
            }
        }

        /// <summary>
        /// Belirtilen para birimi için TL karşılığı kurunu döndürür.
        /// </summary>
        public static decimal KurGetir(string paraBirimi)
        {
            return paraBirimi switch
            {
                ParaBirimleri.USD => _usdKur,
                ParaBirimleri.EUR => _eurKur,
                _ => 1m // TL → TL kuru 1
            };
        }

        /// <summary>
        /// Tutarı güncel kurla TL'ye çevirir.
        /// </summary>
        public static decimal TLyeCevir(decimal tutar, string paraBirimi)
        {
            if (paraBirimi == ParaBirimleri.TL) return tutar;
            var kur = KurGetir(paraBirimi);
            return kur > 0 ? tutar * kur : tutar;
        }

        /// <summary>
        /// Tutarı belirtilen kurla TL'ye çevirir (anlaşma kuru ile).
        /// </summary>
        public static decimal TLyeCevirKurlu(decimal tutar, string paraBirimi, decimal anlasmaKuru)
        {
            if (paraBirimi == ParaBirimleri.TL) return tutar;
            return anlasmaKuru > 0 ? tutar * anlasmaKuru : tutar;
        }

        /// <summary>
        /// Anlaşma kuru ile güncel kur arasındaki fark yüzdesini hesaplar.
        /// Pozitif = güncel kur daha yüksek (lehimize), negatif = daha düşük.
        /// </summary>
        public static decimal KurFarkiYuzdesi(decimal anlasmaKuru, string paraBirimi)
        {
            if (paraBirimi == ParaBirimleri.TL || anlasmaKuru <= 0) return 0;
            var guncelKur = KurGetir(paraBirimi);
            if (guncelKur <= 0) return 0;

            return ((guncelKur - anlasmaKuru) / anlasmaKuru) * 100m;
        }

        /// <summary>
        /// Kur bilgisi formatlanmış metin (Dashboard/UI için).
        /// Örn: "1$ = 44,37₺ | 1€ = 51,02₺"
        /// </summary>
        public static string KurBilgisiMetni()
        {
            if (!KurlarYuklendi) return "Kur bilgisi yükleniyor...";
            return $"1$ = {_usdKur:N2}₺ | 1€ = {_eurKur:N2}₺";
        }

        /// <summary>
        /// Arka planda kurları belirli aralıklarla (ör. saatte bir) sessizce güncelleyen döngü.
        /// </summary>
        public static void BaslatArkaPlanSenkronizasyonu()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        // Başlangıçta ve her saat başı günceller
                        await KurlariGuncelleAsync(zorlaGuncelle: true);
                    }
                    catch
                    {
                        // Arka plan hataları sessizce geçilir
                    }

                    await Task.Delay(CACHE_SURESI);
                }
            });
        }
    }
}
