using System;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Proje alt görevi (yapılacak) bilgilerini temsil eden model sınıfı.
    /// </summary>
    public class Gorev
    {
        public int GorevID { get; set; }
        public int ProjeID { get; set; }

        /// <summary>
        /// Atanan çalışan ID'si. null ise kimseye atanmamış.
        /// </summary>
        public int? CalisanID { get; set; }

        public string Baslik { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public bool Tamamlandi { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }

        /// <summary>
        /// Çalışan adı (LEFT JOIN ile doldurulur, veritabanında ayrı sütun değil).
        /// </summary>
        public string CalisanAdSoyad { get; set; } = string.Empty;
    }
}
