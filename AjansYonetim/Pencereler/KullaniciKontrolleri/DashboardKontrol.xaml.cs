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
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AjansYonetim.Pencereler.KullaniciKontrolleri
{
    public partial class DashboardKontrol : UserControl
    {
        private const int AramaDebounceMs = 300;
        private System.Windows.Threading.DispatcherTimer? _aramaTimer;
        private int _acilGunEsigi = 2; // Default
        
        // Ana pencere referansını veya yenileme action'ını tetiklemek için kullanılabilir
        public event EventHandler? DashboardGuncellendi;

        public DashboardKontrol()
        {
            InitializeComponent();
            
            cmbFiltre.ItemsSource = ProjeDurumlari.FiltreDurumlari;
            cmbFiltre.SelectedIndex = 0;
            cmbKategoriFiltre.ItemsSource = ProjeKategorileri.FiltreKategorileri;
            cmbKategoriFiltre.SelectedIndex = 0;
        }

        /// <summary>
        /// Dashboad sekmesi gösterildiğinde dışarıdan çağırılır.
        /// </summary>
        public async Task YukleAsync(int acilGunEsigi)
        {
            _acilGunEsigi = acilGunEsigi;
            await DashboardGuncelleAsync();
            await GrafikleriGuncelleAsync();
        }

        private async Task DashboardGuncelleAsync()
        {
            var secilenFiltre = cmbFiltre.SelectedItem as string ?? ProjeDurumlari.FILTRE_TUMU;
            var secilenKategori = cmbKategoriFiltre?.SelectedItem as string ?? ProjeKategorileri.FILTRE_TUMU;
            var aramaMetni = txtArama?.Text?.Trim() ?? string.Empty;

            var projeler = await Task.Run(() =>
            {
                List<Proje> sonuc;

                if (!string.IsNullOrWhiteSpace(aramaMetni))
                {
                    sonuc = ProjeIslemleri.ProjeAra(aramaMetni, null, null);
                    if (secilenFiltre != ProjeDurumlari.FILTRE_TUMU)
                        sonuc = sonuc.Where(p => p.Durum == secilenFiltre).ToList();
                    else
                        sonuc = sonuc.Where(p => p.Durum != ProjeDurumlari.TAMAMLANDI).ToList();
                }
                else if (secilenFiltre == ProjeDurumlari.FILTRE_TUMU)
                {
                    sonuc = ProjeIslemleri.AktifProjeleriGetir();
                }
                else
                {
                    sonuc = ProjeIslemleri.DurumaGoreFiltrele(secilenFiltre);
                }

                if (secilenKategori != ProjeKategorileri.FILTRE_TUMU)
                    sonuc = sonuc.Where(p => p.Kategori == secilenKategori).ToList();

                return sonuc;
            });

            dgProjeler.ItemsSource = projeler;

            pnlDashboardBos.Visibility = projeler.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            dgProjeler.Visibility = projeler.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            await OzetKartlariniGuncelleAsync();
        }

        private async Task OzetKartlariniGuncelleAsync()
        {
            var (tumProjeler, musteriler, toplamGelir, bekleyenOdeme, buAyGelir, gecenAyGelir) =
                await Task.Run(() =>
                {
                    var tp = ProjeIslemleri.TumProjeleriGetir();
                    var ms = MusteriIslemleri.TumMusterileriGetir();
                    var tg = OdemeIslemleri.ToplamGelirGetir();
                    var bo = OdemeIslemleri.BekleyenOdemeGetir();
                    var bag = OdemeIslemleri.BuAyGelirGetir();
                    var gag = OdemeIslemleri.GecenAyGelirGetir();
                    return (tp, ms, tg, bo, bag, gag);
                });

            var aktifProjeler = tumProjeler.Where(p => p.Durum != ProjeDurumlari.TAMAMLANDI).ToList();
            var acilProjeler = aktifProjeler.Where(p => (p.TeslimTarihi - DateTime.Now).TotalDays <= _acilGunEsigi).ToList();

            SayiAnimasyonu.AnimasyonluGuncelle(txtToplamProje, tumProjeler.Count);
            SayiAnimasyonu.AnimasyonluGuncelle(txtAktifProje, aktifProjeler.Count);
            SayiAnimasyonu.AnimasyonluGuncelle(txtAcilProje, acilProjeler.Count);
            SayiAnimasyonu.AnimasyonluGuncelle(txtToplamMusteri, musteriler.Count);

            SayiAnimasyonu.ParaAnimasyonluGuncelle(txtToplamGelir, toplamGelir);
            SayiAnimasyonu.ParaAnimasyonluGuncelle(txtBekleyenOdeme, bekleyenOdeme);

            txtAcilBaslik.Text = $"Acil (≤{_acilGunEsigi} Gün)";

            if (DovizKurServisi.KurlarYuklendi)
            {
                txtKurBilgisiDashboard.Text = $"💱 {DovizKurServisi.KurBilgisiMetni()}";
            }

            if (gecenAyGelir > 0)
            {
                var yuzde = ((buAyGelir - gecenAyGelir) / gecenAyGelir) * 100;
                var ok = yuzde >= 0 ? "▲" : "▼";
                txtBuAyTrend.Text = $"{ok} Bu ay: ₺{buAyGelir:N0} ({yuzde:+0;-0}%)";
            }
            else
            {
                txtBuAyTrend.Text = $"Bu ay: ₺{buAyGelir:N0}";
            }
        }

        private async void FiltreDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            await DashboardGuncelleAsync();
        }

        private async void KategoriFiltreDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            await DashboardGuncelleAsync();
        }

        private void AramaDegisti(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            _aramaTimer?.Stop();
            _aramaTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AramaDebounceMs)
            };
            _aramaTimer.Tick += async (s, args) =>
            {
                _aramaTimer.Stop();
                await DashboardGuncelleAsync();
            };
            _aramaTimer.Start();
        }

        private async Task GrafikleriGuncelleAsync()
        {
            try
            {
                await AylikGelirGrafiginiGuncelleAsync();
                await DurumDagilimiGrafiginiGuncelleAsync();
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }

        private async Task AylikGelirGrafiginiGuncelleAsync()
        {
            var aylikVeriler = await Task.Run(() => OdemeIslemleri.AylikGelirGetir());

            if (aylikVeriler.Count == 0)
            {
                chartAylikGelir.Series = Array.Empty<ISeries>();
                return;
            }

            var degerler = aylikVeriler.Select(v => (double)v.Tutar).ToArray();
            var etiketler = aylikVeriler.Select(v => v.Ay).ToArray();

            chartAylikGelir.Series = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = degerler,
                    Fill = new SolidColorPaint(new SKColor(124, 58, 237)),
                    Name = "Gelir (₺)"
                }
            };

            chartAylikGelir.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = etiketler,
                    LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                    TextSize = 10
                }
            };

            chartAylikGelir.YAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                    TextSize = 10
                }
            };
        }

        private async Task DurumDagilimiGrafiginiGuncelleAsync()
        {
            var tumProjeler = await Task.Run(() => ProjeIslemleri.TumProjeleriGetir());
            var durumGruplari = tumProjeler.GroupBy(p => p.Durum)
                .Select(g => new { Durum = g.Key, Adet = g.Count() })
                .ToList();

            if (durumGruplari.Count == 0)
            {
                chartDurumDagilimi.Series = Array.Empty<ISeries>();
                return;
            }

            var renkler = new SKColor[]
            {
                new(124, 58, 237),
                new(59, 130, 246),
                new(245, 158, 11),
                new(34, 197, 94)
            };

            var seriler = new List<ISeries>();
            for (int i = 0; i < durumGruplari.Count; i++)
            {
                var grup = durumGruplari[i];
                seriler.Add(new PieSeries<int>
                {
                    Values = new[] { grup.Adet },
                    Name = grup.Durum,
                    Fill = new SolidColorPaint(renkler[i % renkler.Length])
                });
            }

            chartDurumDagilimi.Series = seriler;
        }

        private async void YeniProjeTiklandi(object sender, RoutedEventArgs e)
        {
            var form = new ProjeFormu { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await DashboardGuncelleAsync();
                await GrafikleriGuncelleAsync();
                DashboardGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void ProjeDuzenleTiklandi(object sender, RoutedEventArgs e)
        {
            if (dgProjeler.SelectedItem is not Proje secilenProje)
            {
                OnayDiyalogu.Uyari("Lütfen düzenlemek istediğiniz projeyi seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            var form = new ProjeFormu(secilenProje) { Owner = Window.GetWindow(this) };

            if (form.ShowDialog() == true)
            {
                await DashboardGuncelleAsync();
                await GrafikleriGuncelleAsync();
                DashboardGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void ProjeSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (dgProjeler.SelectedItem is not Proje secilenProje)
            {
                OnayDiyalogu.Uyari("Lütfen silmek istediğiniz projeyi seçin.", "Uyarı", Window.GetWindow(this));
                return;
            }

            if (OnayDiyalogu.EvetHayir($"'{secilenProje.ProjeAdi}' projesini silmek istediğinize emin misiniz?", "Silme Onayı", Window.GetWindow(this)))
            {
                ProjeIslemleri.ProjeSil(secilenProje.ProjeID);
                AktiviteIslemleri.AktiviteEkle($"'{secilenProje.ProjeAdi}' projesi (ID: {secilenProje.ProjeID}) silindi.", "\uE74D");
                
                await DashboardGuncelleAsync();
                await GrafikleriGuncelleAsync();
                DashboardGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void ProjeDetayCiftTiklama(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is Proje secilenProje)
            {
                var detayPenceresi = new ProjeDetayPenceresi(secilenProje) { Owner = Window.GetWindow(this) };
                detayPenceresi.ShowDialog();
                
                await DashboardGuncelleAsync();
                await GrafikleriGuncelleAsync();
                DashboardGuncellendi?.Invoke(this, EventArgs.Empty);
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
    }
}
