using System;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Çalışan bilgilerini temsil eden model sınıfı.
    /// </summary>
    public class Calisan
    {
        public int CalisanID { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;
        public string Departman { get; set; } = string.Empty;
        public string Pozisyon { get; set; } = string.Empty;
        public string CalisanTuru { get; set; } = string.Empty;
        public DateTime IseBaslamaTarihi { get; set; }
        public string Durum { get; set; } = string.Empty;
        public string Notlar { get; set; } = string.Empty;

        // ═══════════════ PERFORMANS METRİKLERİ ═══════════════
        // Bu alanlar veritabanı sorgularıyla doldurulur, tabloda sütun olarak bulunmaz.

        /// <summary>
        /// Atanan toplam proje sayısı.
        /// </summary>
        public int ToplamProjeSayisi { get; set; }

        /// <summary>
        /// Tamamlanan proje sayısı.
        /// </summary>
        public int TamamlananProjeSayisi { get; set; }

        /// <summary>
        /// Aktif (devam eden) proje sayısı.
        /// </summary>
        public int AktifProjeSayisi { get; set; }

        /// <summary>
        /// Geciken proje sayısı.
        /// </summary>
        public int GecikenProjeSayisi { get; set; }

        /// <summary>
        /// Tamamlanma oranı yüzdesi.
        /// </summary>
        public double TamamlanmaOrani =>
            ToplamProjeSayisi > 0
                ? Math.Round((double)TamamlananProjeSayisi / ToplamProjeSayisi * 100, 1)
                : 0;

        /// <summary>
        /// ComboBox ve listelerde gösterilecek metin.
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Pozisyon)
                ? AdSoyad
                : $"{AdSoyad} ({Pozisyon})";
        }
    }
}
