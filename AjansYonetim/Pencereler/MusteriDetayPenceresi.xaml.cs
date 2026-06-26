using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Müşteri detay penceresi — müşterinin projeleri, finansal özeti ve iletişim geçmişi.
    /// </summary>
    public partial class MusteriDetayPenceresi : Window
    {
        private readonly Musteri _musteri;

        public MusteriDetayPenceresi(Musteri musteri)
        {
            InitializeComponent();
            _musteri = musteri;

            // Müşteri bilgileri
            txtMusteriAdi.Text = musteri.AdSoyad;

            var sirketBilgisi = string.Empty;
            if (!string.IsNullOrWhiteSpace(musteri.SirketAdi))
                sirketBilgisi += musteri.SirketAdi;

            txtSirketBilgisi.Text = sirketBilgisi;

            var iletisim = string.Empty;
            if (!string.IsNullOrWhiteSpace(musteri.Telefon))
                iletisim += $"{musteri.Telefon}  ";
            if (!string.IsNullOrWhiteSpace(musteri.Eposta))
                iletisim += musteri.Eposta;

            txtIletisim.Text = iletisim;

            // Projeleri yükle (SQL düzeyinde MusteriID filtresi)
            var projeler = ProjeIslemleri.MusterininProjeleriniGetir(musteri.MusteriID);

            dgMusteriProjeleri.ItemsSource = projeler;

            // Aktif proje sayısı
            var aktifProjeSayisi = projeler.Count(p => p.Durum != ProjeDurumlari.TAMAMLANDI);

            // Finansal özet hesapla (tek SQL sorgusuyla toplam ödeme)
            var toplamFiyat = projeler.Sum(p => p.FiyatTL);
            var toplamOdenen = OdemeIslemleri.MusterininToplamOdemeleriniGetir(musteri.MusteriID);

            var kalanBorc = toplamFiyat - toplamOdenen;

            txtToplamProje.Text = projeler.Count.ToString();
            txtAktifProje.Text = aktifProjeSayisi.ToString();
            txtToplamFiyat.Text = $"₺{toplamFiyat:N0}";
            txtToplamOdenen.Text = $"₺{toplamOdenen:N0}";
            if (kalanBorc < 0)
            {
                txtKalanBorc.Text = $"₺{Math.Abs(kalanBorc):N0} fazla";
                txtKalanBorc.Foreground = (System.Windows.Media.Brush)FindResource("BasariFirca");
            }
            else
            {
                txtKalanBorc.Text = $"₺{kalanBorc:N0}";
            }

            // İletişim geçmişi
            cmbIletisimTuru.ItemsSource = IletisimTurleri.TumTurler;
            cmbIletisimTuru.SelectedIndex = 0;
            IletisimNotlariniYukle();
        }

        // ═══════════════ FATURA / TEKLİF ═══════════════

        private void FaturaOlusturTiklandi(object sender, RoutedEventArgs e)
        {
            var pencere = new FaturaOlusturPenceresi(_musteri.MusteriID);
            pencere.Owner = this;
            pencere.ShowDialog();
        }

        // ═══════════════ İLETİŞİM GEÇMİŞİ ═══════════════

        private void IletisimNotlariniYukle()
        {
            lstIletisimNotlari.ItemsSource = MusteriNotuIslemleri.MusteriNotlariniGetir(_musteri.MusteriID);
        }

        private void IletisimNotuEkleTiklandi(object sender, RoutedEventArgs e)
        {
            var notMetni = txtYeniIletisimNotu.Text.Trim();
            if (string.IsNullOrWhiteSpace(notMetni))
            {
                OnayDiyalogu.Uyari("Not metni boş olamaz.", "Uyarı", this);
                return;
            }

            var iletisimTuru = cmbIletisimTuru.SelectedItem as string ?? IletisimTurleri.DIGER;

            var basarili = MusteriNotuIslemleri.NotEkle(new MusteriNotu
            {
                MusteriID = _musteri.MusteriID,
                NotMetni = notMetni,
                IletisimTuru = iletisimTuru,
                OlusturmaTarihi = DateTime.Now
            });

            if (!basarili)
            {
                OnayDiyalogu.Hata("Not eklenirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            txtYeniIletisimNotu.Text = string.Empty;
            IletisimNotlariniYukle();
        }

        private void IletisimNotuSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int notID)
            {
                if (!MusteriNotuIslemleri.NotSil(notID))
                {
                    OnayDiyalogu.Hata("Not silinirken bir hata oluştu.", "Silme Hatası", this);
                    return;
                }
                IletisimNotlariniYukle();
            }
        }
    }
}
