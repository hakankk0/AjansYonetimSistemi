using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AjansYonetim.Modeller;
using AjansYonetim.Pencereler;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Periyodik olarak acil projeleri kontrol eden hatırlatma servisi.
    /// DispatcherTimer ile her 30 dakikada bir çalışır.
    /// </summary>
    public class HatirlatmaServisi
    {
        /// <summary>
        /// Hatırlatma kontrol aralığı (dakika).
        /// </summary>
        private const int KontrolAraligiDakika = 30;

        /// <summary>
        /// Zamanlayıcı.
        /// </summary>
        private readonly DispatcherTimer _zamanlayici;

        /// <summary>
        /// Ana pencere referansı.
        /// </summary>
        private readonly Window _sahipPencere;

        /// <summary>
        /// Acil kabul edilen gün eşiği.
        /// </summary>
        private int _acilGunEsigi;

        /// <summary>
        /// Son bildirim gösterilen proje ID'leri (tekrar bildirimi önler).
        /// </summary>
        private readonly HashSet<int> _bildirilenProjeler = new();

        public HatirlatmaServisi(Window sahipPencere, int acilGunEsigi)
        {
            _sahipPencere = sahipPencere;
            _acilGunEsigi = acilGunEsigi;

            _zamanlayici = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(KontrolAraligiDakika)
            };
            _zamanlayici.Tick += ZamanlayiciTetiklendi;
        }

        /// <summary>
        /// Hatırlatma servisini başlatır.
        /// </summary>
        public void Baslat()
        {
            _zamanlayici.Start();
        }

        /// <summary>
        /// Hatırlatma servisini durdurur.
        /// </summary>
        public void Durdur()
        {
            _zamanlayici.Stop();
        }

        /// <summary>
        /// Acil gün eşiğini günceller.
        /// </summary>
        public void EsigiGuncelle(int yeniEsik)
        {
            _acilGunEsigi = yeniEsik;
        }

        /// <summary>
        /// Zamanlayıcı tetiklendiğinde acil projeleri kontrol eder.
        /// </summary>
        private void ZamanlayiciTetiklendi(object? sender, EventArgs e)
        {
            try
            {
                var aktifProjeler = ProjeIslemleri.AktifProjeleriGetir();
                
                // --- 1. GECİKEN / ACİL PROJE UYARILARI ---
                var yeniAcilProjeler = aktifProjeler
                    .Where(p => (p.TeslimTarihi - DateTime.Now).TotalDays <= _acilGunEsigi)
                    .Where(p => !_bildirilenProjeler.Contains(p.ProjeID))
                    .OrderBy(p => p.TeslimTarihi)
                    .ToList();

                foreach (var proje in yeniAcilProjeler)
                {
                    var kalanGun = (int)(proje.TeslimTarihi - DateTime.Now).TotalDays;
                    var gunMetni = kalanGun <= 0 ? "⛔ SÜRESİ GEÇTİ" : $"⏰ {kalanGun} gün kaldı";

                    BildirimIslemleri.BildirimEkle(new Bildirim
                    {
                        CalisanID = null,
                        Mesaj = $"⚠️ '{proje.ProjeAdi}' projesinin teslimine {gunMetni}. Lütfen durumunu kontrol edin.",
                        OlusturmaTarihi = DateTime.Now
                    });

                    _bildirilenProjeler.Add(proje.ProjeID);
                }

                // --- 2. GECİKEN ÖDEME HATIRLATMALARI ---
                var gecikmisOdemeler = aktifProjeler
                    .Where(p => p.TeslimTarihi < DateTime.Now) // Teslimi geçmiş
                    .Where(p => !_bildirilenProjeler.Contains(-p.ProjeID)) // Ödeme için ID'nin negatifiyle kayıt tutuyoruz (basit trick)
                    .ToList();

                foreach (var proje in gecikmisOdemeler)
                {
                    var projeninOdemeleri = OdemeIslemleri.ProjeOdemeleriniGetir(proje.ProjeID);
                    var toplamOdenen = projeninOdemeleri.Sum(o => o.Tutar);
                    var bakiye = proje.Fiyat - toplamOdenen;

                    if (bakiye > 0)
                    {
                        string formatliBakiye = ParaBirimleri.FiyatFormatla(bakiye, proje.ParaBirimi);
                        BildirimIslemleri.BildirimEkle(new Bildirim
                        {
                            CalisanID = null,
                            Mesaj = $"💳 ÖDEME GECİKMESİ: '{proje.MusteriAdSoyad}' müşterisine ait teslim tarihi geçmiş projede {formatliBakiye} bekleyen bakiye bulunuyor.",
                            OlusturmaTarihi = DateTime.Now
                        });

                        _bildirilenProjeler.Add(-proje.ProjeID); // Ödeme bildirimi eklendiğini işaretle
                    }
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }
    }
}
