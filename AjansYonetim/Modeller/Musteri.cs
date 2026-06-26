namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Müşteri bilgilerini temsil eden model sınıfı.
    /// </summary>
    public class Musteri
    {
        public int MusteriID { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;
        public string SirketAdi { get; set; } = string.Empty;
        public string VergiNo { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public string Notlar { get; set; } = string.Empty;
        public string MusteriTuru { get; set; } = string.Empty;

        /// <summary>
        /// ComboBox ve listelerde gösterilecek metin.
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(SirketAdi) ? AdSoyad : $"{AdSoyad} ({SirketAdi})";
        }
    }
}
