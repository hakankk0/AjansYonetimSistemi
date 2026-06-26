using System;
using AjansYonetim.Sabitler;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Ödeme bilgilerini temsil eden model sınıfı.
    /// </summary>
    public class Odeme
    {
        public int OdemeID { get; set; }
        public int ProjeID { get; set; }
        public decimal Tutar { get; set; }
        public DateTime OdemeTarihi { get; set; }
        public string Aciklama { get; set; } = string.Empty;

        /// <summary>
        /// Para birimi: TL, USD veya EUR.
        /// </summary>
        public string ParaBirimi { get; set; } = ParaBirimleri.VARSAYILAN;

        /// <summary>
        /// Ödeme alındığı andaki döviz kuru (1 birim = ? TL).
        /// </summary>
        public decimal OdemeKuru { get; set; } = 1.0m;

        /// <summary>
        /// Proje adı (JOIN sorgusu ile doldurulur).
        /// </summary>
        public string ProjeAdi { get; set; } = string.Empty;

        /// <summary>
        /// Müşteri adı soyadı (JOIN sorgusu ile doldurulur).
        /// </summary>
        public string MusteriAdSoyad { get; set; } = string.Empty;

        /// <summary>
        /// Tutarın TL karşılığı (ödeme kuruna göre).
        /// </summary>
        public decimal TutarTL => ParaBirimi == ParaBirimleri.TL ? Tutar : Tutar * OdemeKuru;

        /// <summary>
        /// Simgeli tutar gösterimi (Örn: $15.00).
        /// </summary>
        public string TutarGosterim => ParaBirimleri.FiyatFormatla(Tutar, ParaBirimi);
    }
}
