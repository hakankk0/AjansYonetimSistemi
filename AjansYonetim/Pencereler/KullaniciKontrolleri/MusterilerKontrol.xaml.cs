using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AjansYonetim.Modeller;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;

namespace AjansYonetim.Pencereler.KullaniciKontrolleri
{
    public partial class MusterilerKontrol : UserControl
    {
        public event EventHandler? MusterilerGuncellendi;

        private DispatcherTimer? _musteriAramaTimer;
        private const int AramaDebounceMs = 300;

        public MusterilerKontrol()
        {
            InitializeComponent();
        }

        public async Task YukleAsync()
        {
            txtMusteriArama.Text = string.Empty;
            await MusteriListesiniYukleAsync();
        }

        private async Task MusteriListesiniYukleAsync()
        {
            var musteriler = await Task.Run(() => MusteriIslemleri.TumMusterileriGetir());
            dgMusteriler.ItemsSource = musteriler;

            // Boş durum mesajı
            pnlMusteriBos.Visibility = musteriler.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            dgMusteriler.Visibility = musteriler.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void YeniMusteriTiklandi(object sender, RoutedEventArgs e)
        {
            var form = new MusteriFormu { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await MusteriListesiniYukleAsync();
                MusterilerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void MusteriDuzenleTiklandi(object sender, RoutedEventArgs e)
        {
            if (dgMusteriler.SelectedItem is not Musteri secilenMusteri)
            {
                OnayDiyalogu.Uyari("Lütfen düzenlemek istediğiniz müşteriyi seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            var form = new MusteriFormu(secilenMusteri) { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await MusteriListesiniYukleAsync();
                MusterilerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void MusteriSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (dgMusteriler.SelectedItem is not Musteri secilenMusteri)
            {
                OnayDiyalogu.Uyari("Lütfen silmek istediğiniz müşteriyi seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            if (OnayDiyalogu.EvetHayir($"'{secilenMusteri.AdSoyad}' müşterisini silmek istediğinize emin misiniz?\n\nDikkat: Bu müşteriye ait projeler de silinecektir!", "Silme Onayı", Window.GetWindow(this)))
            {
                MusteriIslemleri.MusteriSil(secilenMusteri.MusteriID);
                await MusteriListesiniYukleAsync();
                MusterilerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private void MusteriAramaDegisti(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            _musteriAramaTimer?.Stop();
            _musteriAramaTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AramaDebounceMs)
            };
            _musteriAramaTimer.Tick += async (s, args) =>
            {
                _musteriAramaTimer.Stop();
                var aramaMetni = txtMusteriArama.Text.Trim();
                if (string.IsNullOrWhiteSpace(aramaMetni))
                {
                    await MusteriListesiniYukleAsync();
                }
                else
                {
                    var sonuclar = await Task.Run(() => MusteriIslemleri.MusteriAra(aramaMetni));
                    dgMusteriler.ItemsSource = sonuclar;
                    pnlMusteriBos.Visibility = sonuclar.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    dgMusteriler.Visibility = sonuclar.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            };
            _musteriAramaTimer.Start();
        }

        private async void MusteriDetayCiftTiklama(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is Musteri secilenMusteri)
            {
                var detayPenceresi = new MusteriDetayPenceresi(secilenMusteri) { Owner = Window.GetWindow(this) };
                detayPenceresi.ShowDialog();
                
                await MusteriListesiniYukleAsync();
                MusterilerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
