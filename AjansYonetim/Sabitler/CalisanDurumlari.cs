namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Çalışan durum değerlerini sabit olarak tutan sınıf.
    /// Magic string kullanımını önler.
    /// </summary>
    public static class CalisanDurumlari
    {
        public const string AKTIF = "Aktif";
        public const string PASIF = "Pasif";

        /// <summary>
        /// Tüm durumları içeren liste (ComboBox için).
        /// </summary>
        public static readonly string[] TumDurumlar = new[]
        {
            AKTIF,
            PASIF
        };

        /// <summary>
        /// Filtreleme için "Tümü" seçeneği dahil liste.
        /// </summary>
        public const string FILTRE_TUMU = "Tümü";

        public static readonly string[] FiltreDurumlari = new[]
        {
            FILTRE_TUMU,
            AKTIF,
            PASIF
        };
    }
}
