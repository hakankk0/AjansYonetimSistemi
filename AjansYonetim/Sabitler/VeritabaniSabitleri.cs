namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Veritabanı işlemlerinde kullanılan ortak sabitler.
    /// Tekrarlanan değerlerin merkezi tanım noktası.
    /// </summary>
    public static class VeritabaniSabitleri
    {
        /// <summary>
        /// Tüm tarih alanlarında kullanılan standart format.
        /// </summary>
        public const string TarihFormati = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Yedek dosyalarının saklandığı klasör adı.
        /// </summary>
        public const string YedekDizinAdi = "Yedekler";

        /// <summary>
        /// Otomatik yedek dosya adı prefix'i.
        /// </summary>
        public const string OtomatikYedekOneki = "AjansYonetim_OtoYedek_";

        /// <summary>
        /// Manuel yedek dosya adı prefix'i.
        /// </summary>
        public const string ManuelYedekOneki = "AjansYonetim_Yedek_";
    }
}
