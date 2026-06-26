namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Müşteri iletişim türleri sabit listesi.
    /// </summary>
    public static class IletisimTurleri
    {
        public const string TELEFON = "📞 Telefon";
        public const string EPOSTA = "📧 E-posta";
        public const string TOPLANTI = "🤝 Toplantı";
        public const string MESAJ = "💬 Mesaj";
        public const string DIGER = "📝 Diğer";

        /// <summary>
        /// Tüm iletişim türleri.
        /// </summary>
        public static readonly string[] TumTurler = new[]
        {
            TELEFON,
            EPOSTA,
            TOPLANTI,
            MESAJ,
            DIGER
        };
    }
}
