namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Çalışan departman değerlerini sabit olarak tutan sınıf.
    /// Magic string kullanımını önler.
    /// </summary>
    public static class CalisanDepartmanlari
    {
        public const string TASARIM = "Tasarım";
        public const string GELISTIRME = "Geliştirme";
        public const string PAZARLAMA = "Pazarlama";
        public const string YONETIM = "Yönetim";
        public const string DIGER = "Diğer";

        /// <summary>
        /// Tüm departmanları içeren liste (ComboBox için).
        /// </summary>
        public static readonly string[] TumDepartmanlar = new[]
        {
            TASARIM,
            GELISTIRME,
            PAZARLAMA,
            YONETIM,
            DIGER
        };

        /// <summary>
        /// Filtreleme için "Tümü" seçeneği dahil liste.
        /// </summary>
        public const string FILTRE_TUMU = "Tümü";

        public static readonly string[] FiltreDepartmanlar = new[]
        {
            FILTRE_TUMU,
            TASARIM,
            GELISTIRME,
            PAZARLAMA,
            YONETIM,
            DIGER
        };
    }
}
