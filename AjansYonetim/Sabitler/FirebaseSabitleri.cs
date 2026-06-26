namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Firebase Gerçek Zamanlı Veritabanı URL ve düğüm sabitleri.
    /// Tüm servislerde (AuthServisi, LisansYoneticisi, BulutServisi, SistemYoneticisiServisi)
    /// ortak kullanılarak magic string tekrarını önler.
    /// </summary>
    public static class FirebaseSabitleri
    {
        /// <summary>
        /// Firebase Gerçek Zamanlı Veritabanı kök URL'si.
        /// </summary>
        public const string BASE_URL = "https://ajansyonetimsistemi-default-rtdb.europe-west1.firebasedatabase.app/";

        /// <summary>
        /// Ajans kayıtlarının tutulduğu düğüm.
        /// </summary>
        public const string AGENCIES_NODE = "Agencies/";

        /// <summary>
        /// OTP (tek kullanımlık şifre) kodlarının tutulduğu düğüm.
        /// </summary>
        public const string OTP_NODE = "OTPCodes/";

        /// <summary>
        /// Lisans bilgilerinin tutulduğu düğüm.
        /// </summary>
        public const string LISANSLAR_NODE = "Lisanslar/";

        /// <summary>
        /// Bulut yedeklerinin tutulduğu düğüm.
        /// </summary>
        public const string YEDEKLER_NODE = "Yedekler/";

        /// <summary>
        /// Admin tarafından üretilen CD-Key kodlarının tutulduğu düğüm.
        /// </summary>
        public const string URETILEN_KODLAR_NODE = "UretilenLisansKodlari/";

        // ═══════════════ TAM URL YARDIMCILARI ═══════════════

        /// <summary>
        /// Lisanslar tam URL'si (BASE_URL + LISANSLAR_NODE).
        /// </summary>
        public const string LISANSLAR_URL = BASE_URL + LISANSLAR_NODE;

        /// <summary>
        /// Yedekler tam URL'si (BASE_URL + YEDEKLER_NODE).
        /// </summary>
        public const string YEDEKLER_URL = BASE_URL + YEDEKLER_NODE;

        /// <summary>
        /// Üretilen kodlar tam URL'si (BASE_URL + URETILEN_KODLAR_NODE).
        /// </summary>
        public const string URETILEN_KODLAR_URL = BASE_URL + URETILEN_KODLAR_NODE;
    }
}
