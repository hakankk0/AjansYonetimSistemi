namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Para birimi değerlerini sabit olarak tutan sınıf.
    /// Magic string kullanımını önler.
    /// </summary>
    public static class ParaBirimleri
    {
        // ═══════════════ PARA BİRİMLERİ ═══════════════

        public const string TL = "TL";
        public const string USD = "USD";
        public const string EUR = "EUR";

        /// <summary>
        /// Varsayılan para birimi.
        /// </summary>
        public const string VARSAYILAN = TL;

        /// <summary>
        /// Tüm para birimlerini içeren liste (ComboBox için).
        /// </summary>
        public static readonly string[] TumParaBirimleri = { TL, USD, EUR };

        // ═══════════════ API ADRESLERİ ═══════════════

        /// <summary>
        /// Frankfurter API — öncelikli kur kaynağı (kayıt gerektirmez, sınırsız).
        /// </summary>
        public const string FRANKFURTER_API_URL = "https://api.frankfurter.dev/latest?from={0}&to=TRY";

        /// <summary>
        /// TCMB günlük kur XML — yedek kur kaynağı.
        /// </summary>
        public const string TCMB_API_URL = "https://www.tcmb.gov.tr/kurlar/today.xml";

        // ═══════════════ AYAR ANAHTARLARI ═══════════════

        public const string AYAR_SON_USD_KUR = "SonUsdKur";
        public const string AYAR_SON_EUR_KUR = "SonEurKur";
        public const string AYAR_SON_KUR_TARIHI = "SonKurGuncellemeTarihi";

        // ═══════════════ KUR FARKI EŞİĞİ ═══════════════

        /// <summary>
        /// Kur farkı uyarı eşiği (yüzde). Bu oranın üzerinde fark varsa uyarı gösterilir.
        /// </summary>
        public const decimal KUR_FARKI_UYARI_ESIGI = 10m;

        // ═══════════════ YARDIMCI METOTLAR ═══════════════

        /// <summary>
        /// Para birimine göre sembol döndürür (₺, $, €).
        /// </summary>
        public static string SembolGetir(string paraBirimi)
        {
            return paraBirimi switch
            {
                USD => "$",
                EUR => "€",
                _ => "₺"
            };
        }

        /// <summary>
        /// Para birimine göre formatlı fiyat metni döndürür (örn: $5.000,00 veya ₺50.000,00).
        /// </summary>
        public static string FiyatFormatla(decimal fiyat, string paraBirimi)
        {
            var sembol = SembolGetir(paraBirimi);
            return $"{sembol}{fiyat:N2}";
        }

        /// <summary>
        /// Para biriminin döviz olup olmadığını kontrol eder.
        /// </summary>
        public static bool DovizMi(string paraBirimi)
        {
            return paraBirimi == USD || paraBirimi == EUR;
        }
    }
}
