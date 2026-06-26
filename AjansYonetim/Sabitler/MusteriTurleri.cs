namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Müşteri türü değerlerini sabit olarak tutan sınıf.
    /// Magic string kullanımını önler.
    /// </summary>
    public static class MusteriTurleri
    {
        public const string YURT_ICI = "Yurt İçi";
        public const string YURT_DISI = "Yurt Dışı";

        /// <summary>
        /// Tüm müşteri türlerini içeren liste (ComboBox için).
        /// </summary>
        public static readonly string[] TumTurler = new[]
        {
            YURT_ICI,
            YURT_DISI
        };

        /// <summary>
        /// Filtreleme için "Tümü" seçeneği dahil liste.
        /// </summary>
        public const string FILTRE_TUMU = "Tümü";

        public static readonly string[] FiltreTurleri = new[]
        {
            FILTRE_TUMU,
            YURT_ICI,
            YURT_DISI
        };
    }
}
