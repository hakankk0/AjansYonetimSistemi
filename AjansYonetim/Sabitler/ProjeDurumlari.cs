namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Proje durum değerlerini sabit olarak tutan sınıf.
    /// Magic string kullanımını önler.
    /// İş akışı: Görev Atandı → Devam Ediyor → Teslim Edildi → Tamamlandı
    /// </summary>
    public static class ProjeDurumlari
    {
        public const string GOREV_ATANDI = "Görev Atandı";
        public const string DEVAM_EDIYOR = "Devam Ediyor";
        public const string TESLIM_EDILDI = "Teslim Edildi";
        public const string TAMAMLANDI = "Tamamlandı";

        /// <summary>
        /// Eski durum değerleri (migration için).
        /// </summary>
        public const string ESKI_BRIEF_ALINDI = "Brief Alındı";
        public const string ESKI_TASARIM_ASAMASINDA = "Tasarım Aşamasında";
        public const string ESKI_REVIZYON_BEKLIYOR = "Revizyon Bekliyor";

        /// <summary>
        /// Tüm durum seçeneklerini içeren liste (ComboBox için).
        /// </summary>
        public static readonly string[] TumDurumlar = new[]
        {
            GOREV_ATANDI,
            DEVAM_EDIYOR,
            TESLIM_EDILDI,
            TAMAMLANDI
        };

        /// <summary>
        /// Filtreleme ComboBox'u için "Tümü" seçeneği dahil liste.
        /// </summary>
        public const string FILTRE_TUMU = "Tümü";

        public static readonly string[] FiltreDurumlari = new[]
        {
            FILTRE_TUMU,
            GOREV_ATANDI,
            DEVAM_EDIYOR,
            TESLIM_EDILDI,
            TAMAMLANDI
        };
    }
}
