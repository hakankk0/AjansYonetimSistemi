using System.Windows;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Çalışan detay penceresi — bilgiler, performans metrikleri ve atanan projeler.
    /// </summary>
    public partial class CalisanDetayPenceresi : Window
    {
        private Calisan _calisan;

        public CalisanDetayPenceresi(Calisan calisan)
        {
            InitializeComponent();
            _calisan = calisan;

            VerileriGoster();
        }

        /// <summary>
        /// Çalışan bilgilerini, performans metriklerini ve projelerini gösterir.
        /// </summary>
        private void VerileriGoster()
        {
            // Başlık
            txtBaslik.Text = _calisan.AdSoyad;
            txtAltBaslik.Text = $"{_calisan.CalisanTuru} — {_calisan.Departman}";

            // Bilgiler
            txtTelefon.Text = string.IsNullOrEmpty(_calisan.Telefon) ? "—" : _calisan.Telefon;
            txtEposta.Text = string.IsNullOrEmpty(_calisan.Eposta) ? "—" : _calisan.Eposta;
            txtDepartman.Text = string.IsNullOrEmpty(_calisan.Departman) ? "—" : _calisan.Departman;
            txtPozisyon.Text = string.IsNullOrEmpty(_calisan.Pozisyon) ? "—" : _calisan.Pozisyon;
            txtCalisanTuru.Text = _calisan.CalisanTuru;
            txtDurum.Text = _calisan.Durum;
            txtIseBaslama.Text = _calisan.IseBaslamaTarihi.ToString("dd.MM.yyyy");
            txtNotlar.Text = string.IsNullOrEmpty(_calisan.Notlar) ? "—" : _calisan.Notlar;

            // Durum rengi
            if (_calisan.Durum == CalisanDurumlari.PASIF)
            {
                txtDurum.Foreground = (System.Windows.Media.Brush)FindResource("TehlikeFirca");
            }

            // Performans metrikleri
            CalisanIslemleri.PerformansGetir(_calisan);
            txtToplamProje.Text = _calisan.ToplamProjeSayisi.ToString();
            txtTamamlanan.Text = _calisan.TamamlananProjeSayisi.ToString();
            txtAktif.Text = _calisan.AktifProjeSayisi.ToString();
            txtGeciken.Text = _calisan.GecikenProjeSayisi.ToString();
            txtBasariOrani.Text = $"%{_calisan.TamamlanmaOrani:0}";

            // Atanan projeler
            var projeler = ProjeCalisanIslemleri.CalisaninProjeleriniGetir(_calisan.CalisanID);
            dgProjeler.ItemsSource = projeler;

            pnlProjeBos.Visibility = projeler.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            dgProjeler.Visibility = projeler.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Düzenle butonuna tıklandığında çağrılır.
        /// </summary>
        private void DuzenleTiklandi(object sender, RoutedEventArgs e)
        {
            var form = new CalisanFormu(_calisan);
            form.Owner = this;

            if (form.ShowDialog() == true)
            {
                // Çalışanı ID ile yeniden yükle
                var guncellenmisCalisan = CalisanIslemleri.CalisanGetir(_calisan.CalisanID);
                if (guncellenmisCalisan != null)
                {
                    _calisan = guncellenmisCalisan;
                }

                VerileriGoster();
            }
        }
    }
}
