using System;
using AjansYonetim.Sabitler;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Fatura/Teklif bilgilerini temsil eden model sınıfı.
    /// </summary>
    public class Fatura
    {
        public int FaturaID { get; set; }
        public int MusteriID { get; set; }
        public int? ProjeID { get; set; }
        public string FaturaNo { get; set; } = string.Empty;
        public string FaturaTuru { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
        public decimal AraToplam { get; set; }
        public int KDVOrani { get; set; }
        public decimal ToplamTutar { get; set; }
        public string Aciklama { get; set; } = string.Empty;

        /// <summary>
        /// Para birimi: TL, USD veya EUR.
        /// </summary>
        public string ParaBirimi { get; set; } = ParaBirimleri.VARSAYILAN;

        /// <summary>
        /// Müşteri adı (JOIN sorgusu ile doldurulur).
        /// </summary>
        public string MusteriAdSoyad { get; set; } = string.Empty;

        /// <summary>
        /// Proje adı (JOIN sorgusu ile doldurulur, opsiyonel).
        /// </summary>
        public string ProjeAdi { get; set; } = string.Empty;
    }
}
