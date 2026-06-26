using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using AjansYonetim.Modeller;
using AjansYonetim.Pencereler;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Uygulama açılışında acil projeleri kontrol eden ve kullanıcıyı uyaran sınıf.
    /// </summary>
    public static class BildirimYonetici
    {
        /// <summary>
        /// Acil projeleri kontrol eder ve varsa uyarı mesajı gösterir.
        /// Uygulama başlangıcında çağrılır.
        /// </summary>
        public static void AcilProjeleriKontrolEt(Window sahipPencere, int acilGunEsigi)
        {
            try
            {
                var aktifProjeler = ProjeIslemleri.AktifProjeleriGetir();
                var acilProjeler = aktifProjeler
                    .Where(p => (p.TeslimTarihi - DateTime.Now).TotalDays <= acilGunEsigi)
                    .OrderBy(p => p.TeslimTarihi)
                    .ToList();

                if (acilProjeler.Count == 0) return;

                var mesajBuilder = new StringBuilder();
                mesajBuilder.AppendLine($"⚠️ Açılış Kontrolü: {acilProjeler.Count} acil proje bulunuyor.\n");

                foreach (var proje in acilProjeler)
                {
                    var kalanGun = (int)(proje.TeslimTarihi - DateTime.Now).TotalDays;
                    var gunMetni = kalanGun <= 0 ? "⛔ SÜRESİ GEÇTİ" : $"⏰ {kalanGun} gün kaldı";

                    mesajBuilder.AppendLine($"• '{proje.ProjeAdi}' projesi — {gunMetni}");
                }

                BildirimIslemleri.BildirimEkle(new Bildirim
                {
                    CalisanID = null,
                    Mesaj = mesajBuilder.ToString(),
                    OlusturmaTarihi = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }
    }
}
