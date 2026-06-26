using System;

namespace AjansYonetim.Modeller
{
    public class Aktivite
    {
        public int AktiviteID { get; set; }
        public string AksiyonMetni { get; set; } = string.Empty;
        public string Ikon { get; set; } = "\uE718";
        public DateTime OlusturmaTarihi { get; set; }

        public string FormatliTarih => OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm");
    }
}
