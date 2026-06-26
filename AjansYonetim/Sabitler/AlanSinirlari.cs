namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Form alanlarının karakter sınırlarını sabit olarak tutan sınıf.
    /// Magic number kullanımını önler.
    /// </summary>
    public static class AlanSinirlari
    {
        // Ad Soyad
        public const int AD_SOYAD_MIN = 3;
        public const int AD_SOYAD_MAX = 100;

        // Telefon
        public const int TELEFON_MAX = 15;

        // Notlar
        public const int NOTLAR_MAX = 500;

        // Adres
        public const int ADRES_MAX = 250;
    }
}
