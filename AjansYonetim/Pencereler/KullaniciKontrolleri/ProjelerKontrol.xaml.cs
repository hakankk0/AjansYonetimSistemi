using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;

namespace AjansYonetim.Pencereler.KullaniciKontrolleri
{
    public partial class ProjelerKontrol : UserControl
    {
        public event EventHandler? ProjelerGuncellendi;

        public ProjelerKontrol()
        {
            InitializeComponent();
        }

        public async Task YukleAsync()
        {
            // Toplu durum ComboBox'unu doldur
            if (cmbTopluDurum.ItemsSource == null)
            {
                cmbTopluDurum.ItemsSource = ProjeDurumlari.TumDurumlar;
            }

            await TumProjeleriYukleAsync();
        }

        private async Task TumProjeleriYukleAsync()
        {
            var projeler = await Task.Run(() => ProjeIslemleri.TumProjeleriGetir());
            dgTumProjeler.ItemsSource = projeler;
        }

        private async void YeniProjeTiklandi(object sender, RoutedEventArgs e)
        {
            var form = new ProjeFormu { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await TumProjeleriYukleAsync();
                ProjelerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        public async void RaporTiklandi(object sender, RoutedEventArgs e)
        {
            var sahipPencere = Window.GetWindow(this);
            var form = new RaporFiltrePenceresi { Owner = sahipPencere };

            if (form.ShowDialog() != true) return;

            var projeler = form.FiltrelenmisProjecteler;
            if (projeler.Count == 0)
            {
                OnayDiyalogu.Uyari("Seçilen filtrelere uygun proje bulunamadı.", "Uyarı", sahipPencere);
                return;
            }

            var sonuc = OnayDiyalogu.Secim(
                "Hangi formatta rapor oluşturmak istersiniz?",
                "Rapor Formatı", "\uE9D9",
                "Excel (.xlsx)", "PDF (.pdf)", sahipPencere);

            if (sonuc == DiyalogSonuc.Iptal) return;

            var tarihDamgasi = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var excelMi = sonuc == DiyalogSonuc.Evet;

            var kaydetDiyalogu = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Rapor Dosyasını Kaydet",
                FileName = $"ProjeRaporu_{tarihDamgasi}",
                Filter = excelMi
                    ? "Excel Dosyası (*.xlsx)|*.xlsx"
                    : "PDF Dosyası (*.pdf)|*.pdf",
                DefaultExt = excelMi ? ".xlsx" : ".pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (kaydetDiyalogu.ShowDialog() != true) return;

            var dosyaYolu = kaydetDiyalogu.FileName;

            try
            {
                var musteriler = await Task.Run(() => MusteriIslemleri.TumMusterileriGetir());
                var odemeler = await Task.Run(() => OdemeIslemleri.TumOdemeleriGetir());

                await Task.Run(() =>
                {
                    if (excelMi)
                        RaporOlusturucu.ExcelRaporOlustur(projeler, musteriler, odemeler, dosyaYolu);
                    else
                        RaporOlusturucu.PdfRaporOlustur(projeler, musteriler, odemeler, dosyaYolu);
                });

                OnayDiyalogu.Basari($"Rapor oluşturuldu:\n{dosyaYolu}", "Rapor Başarılı", sahipPencere);
            }
            catch (Exception ex)
            {
                OnayDiyalogu.Hata($"Rapor oluşturulurken hata:\n{ex.Message}", "Rapor Hatası", sahipPencere);
            }
        }

        private async void TumProjelerDuzenleTiklandi(object sender, RoutedEventArgs e)
        {
            await ProjeDuzenleAsync(dgTumProjeler);
        }

        private async void TumProjelerSilTiklandi(object sender, RoutedEventArgs e)
        {
            await ProjeSilAsync(dgTumProjeler);
        }

        private async Task ProjeDuzenleAsync(DataGrid hedefGrid)
        {
            if (hedefGrid.SelectedItem is not Proje secilenProje)
            {
                OnayDiyalogu.Uyari("Lütfen düzenlemek istediğiniz projeyi seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            var form = new ProjeFormu(secilenProje) { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await TumProjeleriYukleAsync();
                ProjelerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task ProjeSilAsync(DataGrid hedefGrid)
        {
            if (hedefGrid.SelectedItem is not Proje secilenProje)
            {
                OnayDiyalogu.Uyari("Lütfen silmek istediğiniz projeyi seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            if (OnayDiyalogu.EvetHayir($"'{secilenProje.ProjeAdi}' projesini silmek istediğinize emin misiniz?", "Silme Onayı", Window.GetWindow(this)))
            {
                ProjeIslemleri.ProjeSil(secilenProje.ProjeID);
                AktiviteIslemleri.AktiviteEkle($"'{secilenProje.ProjeAdi}' projesi (ID: {secilenProje.ProjeID}) silindi.", "\uE74D");
                
                await TumProjeleriYukleAsync();
                ProjelerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void ProjeDetayCiftTiklama(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is Proje secilenProje)
            {
                var detayPenceresi = new ProjeDetayPenceresi(secilenProje) { Owner = Window.GetWindow(this) };
                detayPenceresi.ShowDialog();
                
                await TumProjeleriYukleAsync();
                ProjelerGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void TopluDurumGuncelleTiklandi(object sender, RoutedEventArgs e)
        {
            if (cmbTopluDurum.SelectedItem is not string yeniDurum)
            {
                OnayDiyalogu.Uyari("Lütfen bir durum seçin.", "Toplu Güncelleme", Window.GetWindow(this));
                return;
            }

            var seciliProjeler = dgTumProjeler.SelectedItems.Cast<Proje>().ToList();
            if (seciliProjeler.Count == 0)
            {
                OnayDiyalogu.Uyari("Lütfen en az bir proje seçin.\n(Ctrl+Click ile çoklu seçim yapabilirsiniz)", "Toplu Güncelleme", Window.GetWindow(this));
                return;
            }

            var basarili = 0;
            foreach (var proje in seciliProjeler)
            {
                var eskiDurum = proje.Durum;
                proje.Durum = yeniDurum;

                // %100 ise tamamlanma durumu otomatik
                const int tamIlerlemeYuzdesi = 100;
                if (yeniDurum == ProjeDurumlari.TAMAMLANDI)
                {
                    proje.TamamlanmaYuzdesi = tamIlerlemeYuzdesi;
                }

                if (ProjeIslemleri.ProjeGuncelle(proje, eskiDurum))
                {
                    basarili++;
                }
            }

            OnayDiyalogu.Basari($"{basarili}/{seciliProjeler.Count} proje '{yeniDurum}' olarak güncellendi.", "Toplu Güncelleme", Window.GetWindow(this));
            
            await TumProjeleriYukleAsync();
            ProjelerGuncellendi?.Invoke(this, EventArgs.Empty);
        }
    }
}
