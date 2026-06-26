namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Proje kategorileri sabit listesi.
    /// </summary>
    public static class ProjeKategorileri
    {
        public const string WEB = "Web";
        public const string BASKI = "Baskı";
        public const string SOSYAL_MEDYA = "Sosyal Medya";
        public const string KURUMSAL = "Kurumsal";
        public const string AMBALAJ = "Ambalaj";
        public const string VIDEO = "Video";
        public const string DIGER = "Diğer";

        /// <summary>
        /// Filtre için boş seçenek.
        /// </summary>
        public const string FILTRE_TUMU = "Tümü";

        /// <summary>
        /// Tüm kategorileri içeren liste.
        /// </summary>
        public static readonly string[] TumKategoriler = new[]
        {
            WEB,
            BASKI,
            SOSYAL_MEDYA,
            KURUMSAL,
            AMBALAJ,
            VIDEO,
            DIGER
        };

        /// <summary>
        /// Filtre listesi (Tümü + kategoriler).
        /// </summary>
        public static readonly string[] FiltreKategorileri = new[]
        {
            FILTRE_TUMU,
            WEB,
            BASKI,
            SOSYAL_MEDYA,
            KURUMSAL,
            AMBALAJ,
            VIDEO,
            DIGER
        };
    }
}
