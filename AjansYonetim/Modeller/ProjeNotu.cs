using System;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Proje notu bilgilerini temsil eden model sınıfı.
    /// </summary>
    public class ProjeNotu
    {
        public int NotID { get; set; }
        public int ProjeID { get; set; }
        public string NotMetni { get; set; } = string.Empty;
        public DateTime OlusturmaTarihi { get; set; }
    }
}
