using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;

namespace AjansYonetim.Pencereler.KullaniciKontrolleri
{
    public partial class CalisanlarKontrol : UserControl
    {
        public event EventHandler? CalisanlarGuncellendi;

        private DispatcherTimer? _calisanAramaTimer;
        private const int AramaDebounceMs = 300;

        public CalisanlarKontrol()
        {
            InitializeComponent();
        }

        public async Task YukleAsync()
        {
            // Yüklemede combobox'ı doldur
            if (cmbCalisanTurFiltre.ItemsSource == null)
            {
                cmbCalisanTurFiltre.ItemsSource = CalisanTurleri.FiltreTurleri;
                cmbCalisanTurFiltre.SelectedIndex = 0; // Tümü
            }

            txtCalisanArama.Text = string.Empty;
            await CalisanListesiniYukleAsync();
        }

        private async Task CalisanListesiniYukleAsync()
        {
            var secilenTur = cmbCalisanTurFiltre?.SelectedItem as string ?? CalisanTurleri.FILTRE_TUMU;
            var calisanlar = await Task.Run(() => CalisanIslemleri.TumCalisanlariGetir());

            if (secilenTur != CalisanTurleri.FILTRE_TUMU)
                calisanlar = calisanlar.Where(c => c.CalisanTuru == secilenTur).ToList();

            dgCalisanlar.ItemsSource = calisanlar;

            pnlCalisanBos.Visibility = calisanlar.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            dgCalisanlar.Visibility = calisanlar.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void CalisanTurFiltreDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            await CalisanListesiniYukleAsync();
        }

        private void CalisanAramaDegisti(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            _calisanAramaTimer?.Stop();
            _calisanAramaTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AramaDebounceMs)
            };
            _calisanAramaTimer.Tick += async (s, args) =>
            {
                _calisanAramaTimer.Stop();
                var aramaMetni = txtCalisanArama.Text.Trim();
                if (string.IsNullOrWhiteSpace(aramaMetni))
                {
                    await CalisanListesiniYukleAsync();
                }
                else
                {
                    var sonuclar = await Task.Run(() => CalisanIslemleri.CalisanAra(aramaMetni));
                    dgCalisanlar.ItemsSource = sonuclar;
                    pnlCalisanBos.Visibility = sonuclar.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    dgCalisanlar.Visibility = sonuclar.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            };
            _calisanAramaTimer.Start();
        }

        private async void YeniCalisanTiklandi(object sender, RoutedEventArgs e)
        {
            var form = new CalisanFormu { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await CalisanListesiniYukleAsync();
                CalisanlarGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void CalisanDuzenleTiklandi(object sender, RoutedEventArgs e)
        {
            if (dgCalisanlar.SelectedItem is not Calisan secilenCalisan)
            {
                OnayDiyalogu.Uyari("Lütfen düzenlemek istediğiniz çalışanı seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            var form = new CalisanFormu(secilenCalisan) { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await CalisanListesiniYukleAsync();
                CalisanlarGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void CalisanSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (dgCalisanlar.SelectedItem is not Calisan secilenCalisan)
            {
                OnayDiyalogu.Uyari("Lütfen silmek istediğiniz çalışanı seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            if (OnayDiyalogu.EvetHayir($"'{secilenCalisan.AdSoyad}' çalışanını silmek istediğinize emin misiniz?\n\nDikkat: Bu çalışana ait proje atamaları da silinecektir!", "Silme Onayı", Window.GetWindow(this)))
            {
                CalisanIslemleri.CalisanSil(secilenCalisan.CalisanID);
                await CalisanListesiniYukleAsync();
                CalisanlarGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void CalisanDetayCiftTiklama(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is Calisan secilenCalisan)
            {
                var detayPenceresi = new CalisanDetayPenceresi(secilenCalisan) { Owner = Window.GetWindow(this) };
                detayPenceresi.ShowDialog();
                
                await CalisanListesiniYukleAsync();
                CalisanlarGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
