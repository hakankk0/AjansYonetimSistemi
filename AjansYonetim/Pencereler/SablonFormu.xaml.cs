using System;
using System.Windows;
using AjansYonetim.Modeller;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Proje şablonu ekleme ve düzenleme formu.
    /// </summary>
    public partial class SablonFormu : Window
    {
        /// <summary>
        /// Düzenleme modunda olan şablon (null ise yeni ekleme).
        /// </summary>
        private readonly ProjeSablonu? _duzenlenenSablon;

        /// <summary>
        /// Yeni şablon ekleme modu.
        /// </summary>
        public SablonFormu()
        {
            InitializeComponent();
            _duzenlenenSablon = null;
            txtFormBaslik.Text = "Yeni Şablon Ekle";
        }

        /// <summary>
        /// Mevcut şablonu düzenleme modu.
        /// </summary>
        public SablonFormu(ProjeSablonu sablon)
        {
            InitializeComponent();
            _duzenlenenSablon = sablon;
            txtFormBaslik.Text = "Şablon Düzenle";

            txtSablonAdi.Text = sablon.Ad;
            txtSure.Text = sablon.VarsayilanSureGun.ToString();
            txtFiyat.Text = AjansYonetim.Yardimcilar.FiyatYardimci.Formatla(sablon.TahminiFiyat);
        }

        private void KaydetTiklandi(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSablonAdi.Text))
            {
                OnayDiyalogu.Uyari("Şablon adı zorunludur.", "Doğrulama Hatası", this);
                return;
            }

            if (!int.TryParse(txtSure.Text, out var sure) || sure <= 0)
            {
                OnayDiyalogu.Uyari("Geçerli bir süre (gün) girin.", "Doğrulama Hatası", this);
                return;
            }

            if (!AjansYonetim.Yardimcilar.FiyatYardimci.Parse(txtFiyat.Text, out var fiyat))
            {
                OnayDiyalogu.Uyari("Geçerli bir fiyat girin.", "Doğrulama Hatası", this);
                return;
            }

            var sablon = new ProjeSablonu
            {
                Ad = txtSablonAdi.Text.Trim(),
                VarsayilanSureGun = sure,
                TahminiFiyat = fiyat
            };

            bool basarili;

            if (_duzenlenenSablon != null)
            {
                sablon.SablonID = _duzenlenenSablon.SablonID;
                basarili = SablonIslemleri.SablonGuncelle(sablon);
            }
            else
            {
                basarili = SablonIslemleri.SablonEkle(sablon);
            }

            if (!basarili)
            {
                OnayDiyalogu.Hata("Şablon kaydedilirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Pencere_Surukle(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void IptalTiklandi(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SadeceRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void FiyatRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != ',' && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}
