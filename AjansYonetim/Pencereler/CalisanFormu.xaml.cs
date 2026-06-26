using System;
using System.Windows;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Çalışan ekleme ve düzenleme formu.
    /// </summary>
    public partial class CalisanFormu : Window
    {
        /// <summary>
        /// Düzenleme modunda olan çalışan (null ise yeni ekleme).
        /// </summary>
        private readonly Calisan? _duzenlenenCalisan;

        /// <summary>
        /// Yeni çalışan ekleme modu.
        /// </summary>
        public CalisanFormu()
        {
            InitializeComponent();
            _duzenlenenCalisan = null;
            txtFormBaslik.Text = "Yeni Çalışan Ekle";
            ComboBoxlariDoldur();

            // Varsayılan değerler
            cmbDurum.SelectedItem = CalisanDurumlari.AKTIF;
            cmbCalisanTuru.SelectedIndex = 0;
            cmbDepartman.SelectedIndex = 0;
            dpIseBaslama.SelectedDate = DateTime.Today;
        }

        /// <summary>
        /// Mevcut çalışanı düzenleme modu.
        /// </summary>
        public CalisanFormu(Calisan calisan)
        {
            InitializeComponent();
            _duzenlenenCalisan = calisan;
            txtFormBaslik.Text = "Çalışan Düzenle";
            ComboBoxlariDoldur();

            // Alanları mevcut verilerle doldur
            txtAdSoyad.Text = calisan.AdSoyad;
            txtTelefon.Text = calisan.Telefon;
            txtEposta.Text = calisan.Eposta;
            cmbDepartman.SelectedItem = calisan.Departman;
            txtPozisyon.Text = calisan.Pozisyon;
            cmbCalisanTuru.SelectedItem = calisan.CalisanTuru;
            dpIseBaslama.SelectedDate = calisan.IseBaslamaTarihi;
            cmbDurum.SelectedItem = calisan.Durum;
            txtNotlar.Text = calisan.Notlar;
        }

        /// <summary>
        /// ComboBox'ları sabitlerden doldurur.
        /// </summary>
        private void ComboBoxlariDoldur()
        {
            cmbDepartman.ItemsSource = CalisanDepartmanlari.TumDepartmanlar;
            cmbCalisanTuru.ItemsSource = CalisanTurleri.TumTurler;
            cmbDurum.ItemsSource = CalisanDurumlari.TumDurumlar;
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

            if (cmbDepartman.SelectedItem == null)
            {
                OnayDiyalogu.Uyari("Departman seçimi zorunludur.", "Doğrulama Hatası", this);
                return;
            }

            if (cmbCalisanTuru.SelectedItem == null)
            {
                OnayDiyalogu.Uyari("Çalışan türü seçimi zorunludur.", "Doğrulama Hatası", this);
                return;
            }

            // E-posta doğrulama (dolu ise)
            var eposta = txtEposta.Text.Trim();
            if (!string.IsNullOrWhiteSpace(eposta) && (!eposta.Contains('@') || !eposta.Contains('.')))
            {
                OnayDiyalogu.Uyari("Geçerli bir e-posta adresi giriniz.\nÖrnek: ornek@mail.com", "Doğrulama Hatası", this);
                return;
            }

            var calisan = new Calisan
            {
                AdSoyad = txtAdSoyad.Text.Trim(),
                Telefon = txtTelefon.Text.Trim(),
                Eposta = eposta,
                Departman = cmbDepartman.SelectedItem.ToString()!,
                Pozisyon = txtPozisyon.Text.Trim(),
                CalisanTuru = cmbCalisanTuru.SelectedItem.ToString()!,
                IseBaslamaTarihi = dpIseBaslama.SelectedDate ?? DateTime.Today,
                Durum = cmbDurum.SelectedItem?.ToString() ?? CalisanDurumlari.AKTIF,
                Notlar = txtNotlar.Text.Trim()
            };

            bool basarili;

            if (_duzenlenenCalisan != null)
            {
                calisan.CalisanID = _duzenlenenCalisan.CalisanID;
                basarili = CalisanIslemleri.CalisanGuncelle(calisan);
            }
            else
            {
                basarili = CalisanIslemleri.CalisanEkle(calisan);
            }

            if (!basarili)
            {
                OnayDiyalogu.Hata("Çalışan kaydedilirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Sadece harf ve boşluk girişine izin verir (Ad Soyad).
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
        /// Sadece rakam girişine izin verir (Telefon).
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
        /// İptal butonuna tıklandığında çağrılır.
        /// </summary>
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
    }
}
