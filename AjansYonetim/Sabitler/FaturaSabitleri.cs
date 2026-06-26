namespace AjansYonetim.Sabitler
{
    /// <summary>
    /// Fatura/Teklif modülü sabitleri.
    /// </summary>
    public static class FaturaSabitleri
    {
        // ═══════════════ FATURA TÜRLERİ ═══════════════

        public const string FATURA = "Fatura";
        public const string TEKLIF = "Teklif";

        public static readonly string[] TumTurler = { FATURA, TEKLIF };

        // ═══════════════ KDV ORANLARI ═══════════════

        public const int KDV_SIFIR = 0;
        public const int KDV_BIR = 1;
        public const int KDV_ON = 10;
        public const int KDV_YIRMI = 20;
        public const int VARSAYILAN_KDV_ORANI = 20;

        public static readonly int[] KDVOranlari = { KDV_SIFIR, KDV_BIR, KDV_ON, KDV_YIRMI };

        // ═══════════════ FATURA NO FORMATI ═══════════════

        /// <summary>
        /// Fatura numarası ön eki.
        /// </summary>
        public const string FATURA_NO_ONEKI = "FT";

        /// <summary>
        /// Fatura numarası formatı: FT-2026-0001
        /// </summary>
        public const string FATURA_NO_FORMAT = "{0}-{1}-{2:D4}";

        // ═══════════════ PDF SABİTLERİ ═══════════════

        public const string PDF_FOOTER_METNI = "Bu belge bilgisayar ortamında oluşturulmuştur.";
    }
}
