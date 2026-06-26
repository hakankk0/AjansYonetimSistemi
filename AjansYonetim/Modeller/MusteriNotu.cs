using System;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Müşteri iletişim notu modeli — tarih damgalı iletişim logları.
    /// </summary>
    public class MusteriNotu
    {
        public int NotID { get; set; }
        public int MusteriID { get; set; }
        public string NotMetni { get; set; } = string.Empty;
        public string IletisimTuru { get; set; } = string.Empty;
        public DateTime OlusturmaTarihi { get; set; }
    }
}
