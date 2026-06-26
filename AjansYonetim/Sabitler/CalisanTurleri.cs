namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Çalışan türü değerlerini sabit olarak tutan sınıf.
    /// Magic string kullanımını önler.
    /// </summary>
    public static class CalisanTurleri
    {
        public const string YURT_ICI = "Yurt İçi";
        public const string YURT_DISI = "Yurt Dışı";
        public const string DIS_AJANS = "Dış Ajans";

        /// <summary>
        /// Tüm çalışan türlerini içeren liste (ComboBox için).
        /// </summary>
        public static readonly string[] TumTurler = new[]
        {
            YURT_ICI,
            YURT_DISI,
            DIS_AJANS
        };

        /// <summary>
        /// Filtreleme için "Tümü" seçeneği dahil liste.
        /// </summary>
        public const string FILTRE_TUMU = "Tümü";

        public static readonly string[] FiltreTurleri = new[]
        {
            FILTRE_TUMU,
            YURT_ICI,
            YURT_DISI,
            DIS_AJANS
        };
    }
}
