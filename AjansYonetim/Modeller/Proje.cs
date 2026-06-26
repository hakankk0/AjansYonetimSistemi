using System;
using AjansYonetim.Sabitler;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Proje bilgilerini temsil eden model sınıfı.
    /// </summary>
    public class Proje
    {
        public int ProjeID { get; set; }
        public int MusteriID { get; set; }
        public string ProjeAdi { get; set; } = string.Empty;
        public DateTime BaslangicTarihi { get; set; }
        public DateTime TeslimTarihi { get; set; }
        public decimal Fiyat { get; set; }
        public string Durum { get; set; } = string.Empty;
        public string Kategori { get; set; } = string.Empty;
        public int TamamlanmaYuzdesi { get; set; }

        /// <summary>
        /// Para birimi: TL, USD veya EUR.
        /// </summary>
        public string ParaBirimi { get; set; } = ParaBirimleri.VARSAYILAN;

        /// <summary>
        /// Proje oluşturulduğu andaki döviz kuru (1 birim = ? TL).
        /// TL projelerde 1.0 olarak kalır.
        /// </summary>
        public decimal AnlasmaKuru { get; set; } = 1.0m;

        /// <summary>
        /// Müşteri adı (JOIN sorgusu ile doldurulur, veritabanında ayrı sütun değil).
        /// </summary>
        public string MusteriAdSoyad { get; set; } = string.Empty;

        /// <summary>
        /// Proje gecikme durumu — teslim tarihi geçmiş ve tamamlanmamışsa true.
        /// </summary>
        public bool Gecikme => Durum != ProjeDurumlari.TAMAMLANDI && TeslimTarihi < DateTime.Now;

        /// <summary>
        /// Gecikme etiketi — DataGrid/Kanban'da gösterilir.
        /// </summary>
        public string GecikmeEtiketi => Gecikme ? "⛔ GECİKME" : string.Empty;

        /// <summary>
        /// Projeye atanmış çalışanların isim listesi (GROUP_CONCAT ile doldurulur).
        /// </summary>
        public string EkipMetni { get; set; } = string.Empty;

        /// <summary>
        /// Fiyatın TL karşılığı (anlaşma kuruna göre).
        /// </summary>
        public decimal FiyatTL => ParaBirimi == ParaBirimleri.TL ? Fiyat : Fiyat * AnlasmaKuru;

        /// <summary>
        /// Formatlı fiyat gösterimi (sembol ile). Örn: "$5.000,00" veya "₺50.000,00".
        /// </summary>
        public string FiyatGosterim => ParaBirimleri.FiyatFormatla(Fiyat, ParaBirimi);
    }
}
