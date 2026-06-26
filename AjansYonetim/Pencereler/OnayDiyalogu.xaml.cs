using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Tema uyumlu özel diyalog penceresi — MessageBox yerine kullanılır.
    /// Tip bazlı renk kodlaması ve giriş animasyonu içerir.
    /// </summary>
    public partial class OnayDiyalogu : Window
    {
        // ═══════════════ SABİTLER ═══════════════

        /// <summary>Bilgi mesajı ikonu (Segoe MDL2 Assets).</summary>
        private const string IKON_BILGI = "\uE946";
        /// <summary>Uyarı mesajı ikonu.</summary>
        private const string IKON_UYARI = "\uE7BA";
        /// <summary>Hata mesajı ikonu.</summary>
        private const string IKON_HATA = "\uE711";
        /// <summary>Başarı mesajı ikonu.</summary>
        private const string IKON_BASARI = "\uE73E";
        /// <summary>Soru mesajı ikonu.</summary>
        private const string IKON_SORU = "\uE9CE";

        // Renk Sabitleri — Bilgi (Mavi)
        private static readonly Color BILGI_RENK_1 = (Color)ColorConverter.ConvertFromString("#3B82F6");
        private static readonly Color BILGI_RENK_2 = (Color)ColorConverter.ConvertFromString("#60A5FA");

        // Renk Sabitleri — Uyarı (Amber)
        private static readonly Color UYARI_RENK_1 = (Color)ColorConverter.ConvertFromString("#F59E0B");
        private static readonly Color UYARI_RENK_2 = (Color)ColorConverter.ConvertFromString("#FBBF24");

        // Renk Sabitleri — Hata (Kırmızı)
        private static readonly Color HATA_RENK_1 = (Color)ColorConverter.ConvertFromString("#EF4444");
        private static readonly Color HATA_RENK_2 = (Color)ColorConverter.ConvertFromString("#F87171");

        // Renk Sabitleri — Başarı (Yeşil)
        private static readonly Color BASARI_RENK_1 = (Color)ColorConverter.ConvertFromString("#22C55E");
        private static readonly Color BASARI_RENK_2 = (Color)ColorConverter.ConvertFromString("#4ADE80");

        // Renk Sabitleri — Soru / Varsayılan (Mor)
        private static readonly Color SORU_RENK_1 = (Color)ColorConverter.ConvertFromString("#7C3AED");
        private static readonly Color SORU_RENK_2 = (Color)ColorConverter.ConvertFromString("#A78BFA");

        /// <summary>
        /// Kullanıcının verdiği cevap.
        /// </summary>
        public DiyalogSonuc Sonuc { get; private set; } = DiyalogSonuc.Iptal;

        private OnayDiyalogu()
        {
            InitializeComponent();
        }

        // ═══════════════ STATİK FABRİKA METOTLARI ═══════════════

        /// <summary>
        /// Bilgi mesajı gösterir (tek Tamam butonu).
        /// </summary>
        public static void Bilgi(string mesaj, string baslik = "Bilgi", Window? sahip = null)
        {
            var diyalog = Olustur(mesaj, baslik, IKON_BILGI, sahip);
            diyalog.RenkTemaasiUygula(BILGI_RENK_1, BILGI_RENK_2);
            diyalog.btnTamam.Content = "Tamam";
            diyalog.btnIptal.Visibility = Visibility.Collapsed;
            diyalog.ShowDialog();
        }

        /// <summary>
        /// Uyarı mesajı gösterir (tek Tamam butonu, uyarı ikonu).
        /// </summary>
        public static void Uyari(string mesaj, string baslik = "Uyarı", Window? sahip = null)
        {
            var diyalog = Olustur(mesaj, baslik, IKON_UYARI, sahip);
            diyalog.RenkTemaasiUygula(UYARI_RENK_1, UYARI_RENK_2);
            diyalog.btnTamam.Content = "Tamam";
            diyalog.btnIptal.Visibility = Visibility.Collapsed;
            diyalog.ShowDialog();
        }

        /// <summary>
        /// Hata mesajı gösterir (tek Tamam butonu, hata ikonu).
        /// </summary>
        public static void Hata(string mesaj, string baslik = "Hata", Window? sahip = null)
        {
            var diyalog = Olustur(mesaj, baslik, IKON_HATA, sahip);
            diyalog.RenkTemaasiUygula(HATA_RENK_1, HATA_RENK_2);
            diyalog.btnTamam.Content = "Tamam";
            diyalog.btnIptal.Visibility = Visibility.Collapsed;
            diyalog.ShowDialog();
        }

        /// <summary>
        /// Başarı mesajı gösterir (tek Tamam butonu, başarı ikonu).
        /// </summary>
        public static void Basari(string mesaj, string baslik = "Başarılı", Window? sahip = null)
        {
            var diyalog = Olustur(mesaj, baslik, IKON_BASARI, sahip);
            diyalog.RenkTemaasiUygula(BASARI_RENK_1, BASARI_RENK_2);
            diyalog.btnTamam.Content = "Tamam";
            diyalog.btnIptal.Visibility = Visibility.Collapsed;
            diyalog.ShowDialog();
        }

        /// <summary>
        /// Evet / Hayır onay sorusu sorar.
        /// </summary>
        public static bool EvetHayir(string mesaj, string baslik = "Onay", Window? sahip = null)
        {
            var diyalog = Olustur(mesaj, baslik, IKON_SORU, sahip);
            diyalog.RenkTemaasiUygula(SORU_RENK_1, SORU_RENK_2);
            diyalog.btnTamam.Content = "✓ Evet";
            diyalog.btnIptal.Content = "Hayır";
            diyalog.btnIptal.Foreground = Brushes.White;
            diyalog.btnIptal.Visibility = Visibility.Visible;
            diyalog.ShowDialog();
            return diyalog.Sonuc == DiyalogSonuc.Evet;
        }

        /// <summary>
        /// Evet / Hayır / İptal üçlü soru sorar.
        /// </summary>
        public static DiyalogSonuc EvetHayirIptal(string mesaj, string baslik = "Soru", Window? sahip = null)
        {
            var diyalog = Olustur(mesaj, baslik, IKON_SORU, sahip);
            diyalog.RenkTemaasiUygula(SORU_RENK_1, SORU_RENK_2);
            diyalog.btnTamam.Content = "Evet";
            diyalog.btnIptal.Content = "Hayır";
            diyalog.btnIptal.Foreground = Brushes.White;
            diyalog.btnIptal.Visibility = Visibility.Visible;
            diyalog.btnUcuncu.Content = "İptal";
            diyalog.btnUcuncu.Foreground = Brushes.White;
            diyalog.btnUcuncu.Visibility = Visibility.Visible;
            diyalog.ShowDialog();
            return diyalog.Sonuc;
        }

        /// <summary>
        /// İki özel etiketli buton ile seçim sorar.
        /// Evet → birinci seçenek, Hayır → ikinci seçenek, İptal → kapatma/X.
        /// </summary>
        public static DiyalogSonuc Secim(string mesaj, string baslik, string ikon,
            string birinciEtiket, string ikinciEtiket, Window? sahip = null)
        {
            var diyalog = Olustur(mesaj, baslik, ikon, sahip);
            diyalog.RenkTemaasiUygula(SORU_RENK_1, SORU_RENK_2);
            diyalog.btnTamam.Content = birinciEtiket;
            diyalog.btnIptal.Content = ikinciEtiket;
            diyalog.btnIptal.Foreground = Brushes.White;
            diyalog.btnIptal.Visibility = Visibility.Visible;
            diyalog.btnUcuncu.Visibility = Visibility.Collapsed;
            diyalog.ShowDialog();
            return diyalog.Sonuc;
        }

        // ═══════════════ YARDIMCI ═══════════════

        private static OnayDiyalogu Olustur(string mesaj, string baslik, string ikon, Window? sahip)
        {
            var diyalog = new OnayDiyalogu
            {
                Owner = sahip
            };
            diyalog.txtMesaj.Text = mesaj;
            diyalog.txtBaslik.Text = baslik;
            diyalog.txtIkon.Text = ikon;
            diyalog.Title = baslik;
            return diyalog;
        }

        /// <summary>
        /// Mesaj tipine göre ikon dairesi, sol aksan ve ana buton renklerini uygular.
        /// </summary>
        private void RenkTemaasiUygula(Color renk1, Color renk2)
        {
            // İkon dairesi gradyanı
            IkonGradyan1.Color = renk1;
            IkonGradyan2.Color = renk2;

            // Sol aksan gradyanı
            AksanRenk1.Color = renk1;
            AksanRenk2.Color = renk2;

            // Ana buton gradyanı — XAML'deki GradientStop'lara named erişim
            // olmadığı için template'i dinamik güncelleme yerine
            // butonun template'ini yeniden oluşturmak gerekiyor.
            // Basit çözüm: Template'deki renkleri doğrudan değiştir
            AnaButonRenkGuncelle(renk1, renk2);
        }

        /// <summary>
        /// Ana butonun (Tamam/Evet) gradient renklerini dinamik olarak günceller.
        /// </summary>
        private void AnaButonRenkGuncelle(Color renk1, Color renk2)
        {
            // Daha koyu (pressed) ve daha açık (hover) varyantlar oluştur
            var hoverRenk1 = AcikVaryant(renk1);
            var hoverRenk2 = renk1;
            var pressRenk1 = KoyuVaryant(renk1);
            var pressRenk2 = KoyuVaryant(renk2);

            btnTamam.Template = OlusturButonTemplate(renk1, renk2, hoverRenk1, hoverRenk2, pressRenk1, pressRenk2);
        }

        /// <summary>
        /// Dinamik gradient buton template'i oluşturur.
        /// </summary>
        private static System.Windows.Controls.ControlTemplate OlusturButonTemplate(
            Color normalR1, Color normalR2,
            Color hoverR1, Color hoverR2,
            Color pressR1, Color pressR2)
        {
            var template = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));

            // Ana border
            var borderFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Border), "bd");
            borderFactory.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(10));
            borderFactory.SetValue(System.Windows.Controls.Border.PaddingProperty, new Thickness(24, 10, 24, 10));

            var normalGradient = new LinearGradientBrush(normalR1, normalR2, 45);
            normalGradient.Freeze();
            borderFactory.SetValue(System.Windows.Controls.Border.BackgroundProperty, normalGradient);

            var contentPresenter = new FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenter);

            template.VisualTree = borderFactory;

            // Hover trigger
            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            var hoverGradient = new LinearGradientBrush(hoverR1, hoverR2, 45);
            hoverGradient.Freeze();
            hoverTrigger.Setters.Add(new Setter(System.Windows.Controls.Border.BackgroundProperty, hoverGradient, "bd"));
            template.Triggers.Add(hoverTrigger);

            // Pressed trigger
            var pressTrigger = new Trigger { Property = System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, Value = true };
            var pressGradient = new LinearGradientBrush(pressR1, pressR2, 45);
            pressGradient.Freeze();
            pressTrigger.Setters.Add(new Setter(System.Windows.Controls.Border.BackgroundProperty, pressGradient, "bd"));
            template.Triggers.Add(pressTrigger);

            return template;
        }

        /// <summary>Rengi daha açık bir varyantına dönüştürür.</summary>
        private static Color AcikVaryant(Color renk)
        {
            const byte artis = 30;
            return Color.FromArgb(
                renk.A,
                (byte)System.Math.Min(renk.R + artis, 255),
                (byte)System.Math.Min(renk.G + artis, 255),
                (byte)System.Math.Min(renk.B + artis, 255));
        }

        /// <summary>Rengi daha koyu bir varyantına dönüştürür.</summary>
        private static Color KoyuVaryant(Color renk)
        {
            const byte azalis = 25;
            return Color.FromArgb(
                renk.A,
                (byte)System.Math.Max(renk.R - azalis, 0),
                (byte)System.Math.Max(renk.G - azalis, 0),
                (byte)System.Math.Max(renk.B - azalis, 0));
        }

        // ═══════════════ EVENT HANDLER'LAR ═══════════════

        private void Pencere_Yuklendi(object sender, RoutedEventArgs e)
        {
            // Açılış animasyonunu başlat
            var storyboard = (Storyboard)FindResource("AcilisAnimasyonu");
            storyboard.Begin(this);
        }

        private void TamamTiklandi(object sender, RoutedEventArgs e)
        {
            Sonuc = DiyalogSonuc.Evet;
            Close();
        }

        private void IptalTiklandi(object sender, RoutedEventArgs e)
        {
            Sonuc = DiyalogSonuc.Hayir;
            Close();
        }

        private void UcuncuTiklandi(object sender, RoutedEventArgs e)
        {
            Sonuc = DiyalogSonuc.Iptal;
            Close();
        }

        private void KapatTiklandi(object sender, RoutedEventArgs e)
        {
            Sonuc = DiyalogSonuc.Iptal;
            Close();
        }

        private void BaslikSurukle(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }

    /// <summary>
    /// Diyalog sonuç enum'u.
    /// </summary>
    public enum DiyalogSonuc
    {
        Evet,
        Hayir,
        Iptal
    }
}
