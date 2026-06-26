namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Proje şablonu bilgilerini temsil eden model sınıfı.
    /// Veritabanında saklanır, kullanıcı tarafından düzenlenebilir.
    /// </summary>
    public class ProjeSablonu
    {
        public int SablonID { get; set; }
        public string Ad { get; set; } = string.Empty;
        public int VarsayilanSureGun { get; set; }
        public decimal TahminiFiyat { get; set; }

        /// <summary>
        /// ComboBox ve listelerde gösterilecek metin.
        /// </summary>
        public override string ToString() => Ad;
    }
}
