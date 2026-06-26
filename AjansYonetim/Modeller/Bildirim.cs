using System;

namespace AjansYonetim.Modeller
{
    /// <summary>
    /// Sistem içi iletişim ve bildirim kayıtlarını temsil eden model sınıfı.
    /// </summary>
    public class Bildirim
    {
        public int BildirimID { get; set; }
        
        /// <summary>
        /// Bildirimin gönderildiği spesifik çalışan (Tüm sisteme gönderiliyorsa null olabilir).
        /// </summary>
        public int? CalisanID { get; set; }
        
        public string Mesaj { get; set; } = string.Empty;
        
        public bool OkunduMu { get; set; }
        
        public DateTime OlusturmaTarihi { get; set; }

        /// <summary>
        /// Arayüzde okundu durumuna göre renklendirme için yardımcı özellik.
        /// </summary>
        public string DurumRengi => OkunduMu ? "#94A3B8" : "#E2E8F0";
        public string ArkaPlan => OkunduMu ? "Transparent" : "#2A2A40";
        public string FormatliTarih => OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm");
    }
}
