using System.Windows;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Müşteri ekleme ve düzenleme formu.
    /// </summary>
    public partial class MusteriFormu : Window
    {
        /// <summary>
        /// Düzenleme modunda olan müşteri (null ise yeni ekleme).
        /// </summary>
        private readonly Musteri? _duzenlenenMusteri;

        /// <summary>
        /// Yeni müşteri ekleme modu.
        /// </summary>
        public MusteriFormu()
        {
            InitializeComponent();
            _duzenlenenMusteri = null;
            txtFormBaslik.Text = "Yeni Müşteri Ekle";
            cmbMusteriTuru.ItemsSource = MusteriTurleri.TumTurler;
            cmbMusteriTuru.SelectedIndex = 0;
        }

        /// <summary>
        /// Mevcut müşteriyi düzenleme modu.
        /// </summary>
        public MusteriFormu(Musteri musteri)
        {
            InitializeComponent();
            _duzenlenenMusteri = musteri;
            txtFormBaslik.Text = "Müşteri Düzenle";
            cmbMusteriTuru.ItemsSource = MusteriTurleri.TumTurler;

            // Alanları mevcut verilerle doldur
            txtAdSoyad.Text = musteri.AdSoyad;
            txtSirketAdi.Text = musteri.SirketAdi;
            txtVergiNo.Text = musteri.VergiNo;
            txtTelefon.Text = musteri.Telefon;
            txtEposta.Text = musteri.Eposta;
            txtAdres.Text = musteri.Adres;
            txtNotlar.Text = musteri.Notlar;
            cmbMusteriTuru.SelectedItem = string.IsNullOrEmpty(musteri.MusteriTuru)
                ? MusteriTurleri.YURT_ICI : musteri.MusteriTuru;
        }

        /// <summary>
        /// Kaydet butonuna tıklandığında çağrılır.
        /// </summary>
        private void KaydetTiklandi(object sender, RoutedEventArgs e)
        {
            // Doğrulama
            if (string.IsNullOrWhiteSpace(txtAdSoyad.Text))
            {
                OnayDiyalogu.Uyari("Ad Soyad alanı zorunludur.", "Doğrulama Hatası", this);
                return;
            }

            if (txtAdSoyad.Text.Trim().Length < AlanSinirlari.AD_SOYAD_MIN)
            {
                OnayDiyalogu.Uyari(
                    $"Ad Soyad en az {AlanSinirlari.AD_SOYAD_MIN} karakter olmalıdır.",
                    "Doğrulama Hatası", this);
                return;
            }

            // E-posta doğrulama (dolu ise)
            var eposta = txtEposta.Text.Trim();
            if (!string.IsNullOrWhiteSpace(eposta) && (!eposta.Contains('@') || !eposta.Contains('.')))
            {
                OnayDiyalogu.Uyari("Geçerli bir e-posta adresi giriniz.\nÖrnek: ornek@mail.com", "Doğrulama Hatası", this);
                return;
            }

            var musteri = new Musteri
            {
                AdSoyad = txtAdSoyad.Text.Trim(),
                SirketAdi = txtSirketAdi.Text.Trim(),
                VergiNo = txtVergiNo.Text.Trim(),
                Telefon = txtTelefon.Text.Trim(),
                Eposta = eposta,
                Adres = txtAdres.Text.Trim(),
                Notlar = txtNotlar.Text.Trim(),
                MusteriTuru = cmbMusteriTuru.SelectedItem?.ToString() ?? MusteriTurleri.YURT_ICI
            };

            bool basarili;

            if (_duzenlenenMusteri != null)
            {
                musteri.MusteriID = _duzenlenenMusteri.MusteriID;
                basarili = MusteriIslemleri.MusteriGuncelle(musteri);
            }
            else
            {
                basarili = MusteriIslemleri.MusteriEkle(musteri);
            }

            if (!basarili)
            {
                OnayDiyalogu.Hata("Müşteri kaydedilirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Sadece harf ve boşluk girişine izin verir (Ad Soyad).
        /// Türkçe karakterler de kabul edilir.
        /// </summary>
        private void SadeceHarf(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char karakter in e.Text)
            {
                if (!char.IsLetter(karakter) && karakter != ' ')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Sadece rakam girişine izin verir (Telefon, Vergi No).
        /// </summary>
        private void SadeceRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char karakter in e.Text)
            {
                if (!char.IsDigit(karakter))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Pencere sürükleme işlemi.
        /// </summary>
        private void Pencere_Surukle(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        /// <summary>
        /// İptal butonuna tıklandığında çağrılır.
        /// </summary>
        private void IptalTiklandi(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
