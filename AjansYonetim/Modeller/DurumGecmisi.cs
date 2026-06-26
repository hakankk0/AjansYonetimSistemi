using System;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Proje durum değişiklik geçmişi model sınıfı.
    /// </summary>
    public class DurumGecmisi
    {
        public int GecmisID { get; set; }
        public int ProjeID { get; set; }
        public string EskiDurum { get; set; } = string.Empty;
        public string YeniDurum { get; set; } = string.Empty;
        public DateTime DegisimTarihi { get; set; }
    }
}
