using System.Windows;
using AjansYonetim.Yardimcilar;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Lisans aktivasyon penceresi — geçersiz veya eksik lisansta gösterilir.
    /// Kullanıcı geçerli bir anahtar girene kadar uygulamaya erişim engellenir.
    /// </summary>
    public partial class LisansAktivasyonPenceresi : Window
    {
        private readonly AjansYonetim.Veritabani.AjansModel _hedefAjans;

        /// <summary>
        /// Geçersiz anahtar hata mesajı.
        /// </summary>
        private const string GECERSIZ_ANAHTAR_MESAJI =
            "Girilen lisans anahtarı geçersiz veya süresi dolmuş.";

        /// <summary>
        /// Boş anahtar hata mesajı.
        /// </summary>
        private const string BOS_ANAHTAR_MESAJI =
            "Lütfen uygulamanızı aktifleştirmek için aldığınız lisans kodunu (CD-Key) girin.";

        public LisansAktivasyonPenceresi(AjansYonetim.Veritabani.AjansModel hedefAjans)
        {
            InitializeComponent();
            _hedefAjans = hedefAjans;
        }

        private async void EtkinlestirTiklandi(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var anahtarMetni = txtLisansAnahtari.Text.Trim();

            if (string.IsNullOrWhiteSpace(anahtarMetni))
            {
                HataMesajiGoster(BOS_ANAHTAR_MESAJI);
                return;
            }

            // Kontrol süresince butonu kilitle ve önceki hatayı temizle
            if (btn != null) btn.IsEnabled = false;
            txtHataMesaji.Visibility = Visibility.Collapsed;

            var lisans = await LisansYoneticisi.KoduKullanVeLisansUzatAsync(
                _hedefAjans.AgencyId, 
                _hedefAjans.AjansAdi, 
                _hedefAjans.Email, 
                anahtarMetni);
            
            // İşlem bitti, butonu aktifleştir
            if (btn != null) btn.IsEnabled = true;

            if (lisans == null)
            {
                HataMesajiGoster(GECERSIZ_ANAHTAR_MESAJI);
                return;
            }

            // Başarılı (Kod kullanıldı, Trash'e atıldı, Firebase'e yazıldı) — dosyaya (önbelleğe) kaydet
            LisansYoneticisi.LisansDosyasiKaydet(lisans);
            DialogResult = true;
            Close();
        }

        private void CikisTiklandi(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void HataMesajiGoster(string mesaj)
        {
            txtHataMesaji.Text = mesaj;
            txtHataMesaji.Visibility = Visibility.Visible;
        }
    }
}
