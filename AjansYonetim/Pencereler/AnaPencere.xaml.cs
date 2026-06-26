using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Donusturuculer;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Ana pencere - Dashboard, Müşteri, Proje ve Ayarlar yönetim ekranları.
    /// </summary>
    public partial class AnaPencere : Window
    {
        /// <summary>
        /// Teslim tarihi acil kabul edilen gün eşiği.
        /// </summary>
        private int _acilGunEsigi;

        /// <summary>
        /// Varsayılan acil gün eşiği.
        /// </summary>
        private const int VarsayilanAcilGunEsigi = 2;

        /// <summary>
        /// Varsayılan ajans adı.
        /// </summary>
        private const string VarsayilanAjansAdi = "Ajans Yönetim Sistemi";

        /// <summary>
        /// Hatırlatma servisi.
        /// </summary>
        private HatirlatmaServisi? _hatirlatmaServisi;

        /// <summary>
        /// Arama debounce süresi (milisaniye).
        /// </summary>
        private const int AramaDebounceMs = 300;
        /// <summary>
        /// Bildirim sayısı kontrol timer.
        /// </summary>
        private System.Windows.Threading.DispatcherTimer? _bildirimTimer;

        /// <summary>
        /// Yedekleme aralığı seçenekleri.
        /// </summary>
        private static readonly string[] YedeklemeAraliklari = new[]
        {
            "Her Açılışta",
            "Günlük",
            "Haftalık",
            "Aylık",
            "Kapalı"
        };

        private const string VarsayilanYedeklemeAraligi = "Her Açılışta";

        public AnaPencere()
        {
            InitializeComponent();

            // Ayarları veritabanından yükle
            AyarlariYukle();

            // Loaded event'inde async veri yükleme başlat
            Loaded += AnaPencere_Loaded;
            Unloaded += AnaPencere_Unloaded;

            // Arka plan senkronizasyon eventini dinle
            ArkaPlanSenkronizasyon.SenkronDurumDegisti += SenkronDurumDegisti_Handler;

            // Otomatik yedekleme kontrolü
            OtomatikYedekKontrol();
        }

        private async void AnaPencere_Loaded(object sender, RoutedEventArgs e)
        {
            SessionYonetimi.OturumZamanAsimiOldu += OnOturumZamanAsimiOldu;
            SessionYonetimi.Baslat(30);

            // Döviz kurlarını arka planda güncelle
            await DovizKurServisi.KurlariGuncelleAsync();

            // Dashboard ekranını aç ve verilerini yükle
            await DashboardGoruntusunuGosterAsync();
            ctrlDashboard.DashboardGuncellendi += async (s, args) => await SayfayiYenileAsync();

            // Projeler kontrol eventini dinle
            ctrlProjeler.ProjelerGuncellendi += async (s, args) => await SayfayiYenileAsync();

            // Müşteriler kontrol eventini dinle
            ctrlMusteriler.MusterilerGuncellendi += async (s, args) => await SayfayiYenileAsync();

            // Çalışanlar kontrol eventini dinle
            ctrlCalisanlar.CalisanlarGuncellendi += async (s, args) => await SayfayiYenileAsync();

            // Kanban kontrol eventini dinle
            ctrlKanban.KanbanGuncellendi += async (s, args) => await SayfayiYenileAsync();

            // Ayarlar kontrol delegelerini bağla
            ctrlAyarlar.Yukleyici = async (mesaj, islem) => await YuklemeIleAsync(mesaj, islem);
            ctrlAyarlar.VerilerGuncellendi = async () => await SayfayiYenileAsync();
            ctrlAyarlar.LisansGuncellendi = (lisans) => 
            {
                Title = lisans.AjansAdi;
                txtSidebarAjansAdi.Text = lisans.AjansAdi;
            };
            ctrlAyarlar.AcilEsikGuncellendi = (esik) =>
            {
                _acilGunEsigi = esik;
                _hatirlatmaServisi?.EsigiGuncelle(esik);
            };

            // Acil proje bildirimini göster
            BildirimYonetici.AcilProjeleriKontrolEt(this, _acilGunEsigi);

            // Periyodik hatırlatma servisini başlat
            _hatirlatmaServisi = new HatirlatmaServisi(this, _acilGunEsigi);
            _hatirlatmaServisi.Baslat();

            // Bildirimleri periyodik kontrol et (Her 1 dakikada bir)
            BildirimRozetiniGuncelle();
            _bildirimTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _bildirimTimer.Tick += async (s, args) => 
            {
                BildirimRozetiniGuncelle();
                SenkronZamanMetniniGuncelle(); // Her dakika (otomatik mod)
                await HesapSilindiMiKontrolEtAsync();
            };
            _bildirimTimer.Start();

            // Açılışta da hemen ilk kontrolü saniyesinde yap (Kimse 1 dakika beklemesin)
            _ = HesapSilindiMiKontrolEtAsync();


            
            // İlk açılışta buluttaki son yedek tarihini sorgula
            _ = Task.Run(async () => await ArkaPlanSenkronizasyon.IlkAclistaBulutDurumunuSorgulaAsync());
        }

        private void AnaPencere_Unloaded(object sender, RoutedEventArgs e)
        {
            // Pencere kapanırken memory leak olmaması için event'ten çık
            ArkaPlanSenkronizasyon.SenkronDurumDegisti -= SenkronDurumDegisti_Handler;
            SessionYonetimi.OturumZamanAsimiOldu -= OnOturumZamanAsimiOldu;
            SessionYonetimi.Durdur();
        }

        private void SenkronDurumDegisti_Handler(bool isSyncing, string mesaj, DateTime? sonSenkron)
        {
            // Background thread'den UI elementlerine erişmek için Dispatcher kullanılır
            Dispatcher.Invoke(() =>
            {
                if (isSyncing)
                {
                    txtSenkronIkon.Text = "\xE895"; // CloudRefresh icon
                    txtSenkronIkon.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("VurguAcikFirca");
                    txtSonSenkron.Text = mesaj;
                }
                else
                {
                    txtSenkronIkon.Text = "\xE753"; // Cloud icon
                    txtSenkronIkon.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("MetinSonikFirca");
                    
                    if (mesaj == "Senkronizasyon Başarısız!")
                    {
                        txtSonSenkron.Text = "Hata: Senkronizasyon Başarısız";
                        txtSenkronIkon.Foreground = (System.Windows.Media.SolidColorBrush)FindResource("TehlikeFirca");
                    }
                    else if (sonSenkron.HasValue)
                    {
                        SenkronZamanMetniniGuncelle(zorlaGuncelle: true);
                    }
                    else
                    {
                        txtSonSenkron.Text = mesaj == "Hazır" ? "Bulut Yedeği Yok" : $"Dikkat: {mesaj}";
                    }
                }
            });
        }

        private void SenkronZamanMetniniGuncelle(bool zorlaGuncelle = false)
        {
            if (ArkaPlanSenkronizasyon.SonBasariliSenkronZamani.HasValue)
            {
                // Eğer manuel tetiklenmediyse ve ekranda Hata veya Buluta... yazısı varsa ezme
                if (!zorlaGuncelle && 
                    (txtSonSenkron.Text.StartsWith("Hata") || txtSonSenkron.Text.StartsWith("Buluta")))
                {
                    return;
                }
                var sonSenkron = ArkaPlanSenkronizasyon.SonBasariliSenkronZamani.Value;
                var span = DateTime.Now - sonSenkron;
                string zamanMetni;
                
                if (span.TotalHours < 24 && sonSenkron.Date == DateTime.Now.Date)
                    zamanMetni = sonSenkron.ToString("HH:mm"); // İşlem bittiği an direkt saati göster (Örn: 21:42)
                else 
                    zamanMetni = sonSenkron.ToString("dd MMM HH:mm");

                txtSonSenkron.Text = $"Son Senkron: {zamanMetni}";
            }
        }

        // ═══════════════ ASYNC YARDIMCI ═══════════════

        /// <summary>
        /// Loading overlay göstererek async işlem çalıştırır.
        /// </summary>
        private async Task YuklemeIleAsync(string mesaj, Func<Task> islem)
        {
            txtYukleniyorMesaj.Text = mesaj;
            pnlYukleniyor.Visibility = Visibility.Visible;
            try
            {
                await islem();
            }
            finally
            {
                pnlYukleniyor.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Kaydedilmiş ayarları veritabanından yükler.
        /// </summary>
        private void AyarlariYukle()
        {
            _acilGunEsigi = int.TryParse(
                AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_ACIL_GUN_ESIGI, VarsayilanAcilGunEsigi.ToString()),
                out var esik) ? esik : VarsayilanAcilGunEsigi;

            var ajansAdi = VarsayilanAjansAdi;
            var lisans = LisansYoneticisi.MevcutLisans;
            if (lisans != null)
            {
                ajansAdi = lisans.AjansAdi;
            }

            if (!string.IsNullOrWhiteSpace(ajansAdi))
            {
                Title = ajansAdi;
                txtSidebarAjansAdi.Text = ajansAdi;
            }
        }

        // ═══════════════ NAVİGASYON ═══════════════

        private async void DashboardTiklandi(object sender, RoutedEventArgs e)
        {
            await DashboardGoruntusunuGosterAsync();
        }

        private async void MusterilerTiklandi(object sender, RoutedEventArgs e)
        {
            await MusteriGoruntusunuGosterAsync();
        }

        private async void ProjelerTiklandi(object sender, RoutedEventArgs e)
        {
            await ProjelerGoruntusunuGosterAsync();
        }

        private void AyarlarTiklandi(object sender, RoutedEventArgs e) => AyarlarGoruntusunuGoster();
        private async void KanbanTiklandi(object sender, RoutedEventArgs e) => await KanbanGoruntusunuGosterAsync();
        private async void CalisanlarTiklandi(object sender, RoutedEventArgs e) => await CalisanlarGoruntusunuGosterAsync();
        private async void HareketlerTiklandi(object sender, RoutedEventArgs e) => await HareketlerGoruntusunuGosterAsync();



        // ═══════════════ AKTİF MENÜ YÖNETİMİ ═══════════════

        /// <summary>
        /// Tüm menü butonlarını normal stile döndürür, seçili olanı aktif stile geçirir.
        /// </summary>
        private void AktifMenuAyarla(Button aktifButon)
        {
            var menuButonlari = new[] { btnDashboard, btnMusteriler, btnCalisanlar, btnProjeler, btnKanban, btnHareketler, btnAyarlar };

            foreach (var buton in menuButonlari)
            {
                buton.Style = (Style)FindResource("MenuButonStil");
            }

            aktifButon.Style = (Style)FindResource("AktifMenuButonStil");
        }

        // ═══════════════ GÖRÜNÜM YÖNETİMİ ═══════════════

        /// <summary>
        /// Tüm görünümleri gizler.
        /// </summary>
        private void TumGorunumleriGizle()
        {
            ctrlDashboard.Visibility = Visibility.Collapsed;
            ctrlProjeler.Visibility = Visibility.Collapsed;
            ctrlMusteriler.Visibility = Visibility.Collapsed;
            ctrlCalisanlar.Visibility = Visibility.Collapsed;
            ctrlAyarlar.Visibility = Visibility.Collapsed;
            ctrlKanban.Visibility = Visibility.Collapsed;
            ctrlHareketler.Visibility = Visibility.Collapsed;
        }

        private async Task DashboardGoruntusunuGosterAsync()
        {
            TumGorunumleriGizle();
            ctrlDashboard.Visibility = Visibility.Visible;

            txtSayfaBasligi.Text = "Dashboard";
            txtSayfaAltBaslik.Text = "Aktif projelerinize genel bakış";

            AktifMenuAyarla(btnDashboard);
            await ctrlDashboard.YukleAsync(_acilGunEsigi);
        }

        private async Task HareketlerGoruntusunuGosterAsync()
        {
            TumGorunumleriGizle();
            ctrlHareketler.Visibility = Visibility.Visible;

            txtSayfaBasligi.Text = "Aktivite Geçmişi";
            txtSayfaAltBaslik.Text = "Sistem üzerinde yapılan tüm işlemlerin log kayıtları";

            AktifMenuAyarla(btnHareketler);
            await ctrlHareketler.YukleAsync();
        }

        private async Task ProjelerGoruntusunuGosterAsync()
        {
            TumGorunumleriGizle();
            ctrlProjeler.Visibility = Visibility.Visible;

            txtSayfaBasligi.Text = "Projeler";
            txtSayfaAltBaslik.Text = "Tamamlananlar dahil tüm projeleri yönetin";

            AktifMenuAyarla(btnProjeler);
            await ctrlProjeler.YukleAsync();
        }

        private async Task MusteriGoruntusunuGosterAsync()
        {
            TumGorunumleriGizle();
            ctrlMusteriler.Visibility = Visibility.Visible;

            txtSayfaBasligi.Text = "Müşteriler";
            txtSayfaAltBaslik.Text = "Müşteri kayıtlarını yönetin";

            AktifMenuAyarla(btnMusteriler);
            await ctrlMusteriler.YukleAsync();
        }

        /// <summary>
        /// Ayarlar görünümünü gösterir ve veritabanından yüklenen değerleri doldurur.
        /// </summary>
        private async void AyarlarGoruntusunuGoster()
        {
            TumGorunumleriGizle();
            ctrlAyarlar.Visibility = Visibility.Visible;

            txtSayfaBasligi.Text = "Ayarlar";
            txtSayfaAltBaslik.Text = "Uygulama ve ajans ayarlarını yönetin";

            AktifMenuAyarla(btnAyarlar);
            await ctrlAyarlar.YukleAsync(_acilGunEsigi);
        }

        private async Task KanbanGoruntusunuGosterAsync()
        {
            TumGorunumleriGizle();
            ctrlKanban.Visibility = Visibility.Visible;

            txtSayfaBasligi.Text = "Kanban Board";
            txtSayfaAltBaslik.Text = "Proje durumlarını görsel olarak takip edin";

            AktifMenuAyarla(btnKanban);
            await ctrlKanban.YukleAsync();
        }

        private async Task CalisanlarGoruntusunuGosterAsync()
        {
            TumGorunumleriGizle();
            ctrlCalisanlar.Visibility = Visibility.Visible;

            txtSayfaBasligi.Text = "Çalışanlar";
            txtSayfaAltBaslik.Text = "Çalışan kayıtlarını yönetin";

            AktifMenuAyarla(btnCalisanlar);
            await ctrlCalisanlar.YukleAsync();
        }

        private async Task SayfayiYenileAsync()
        {
            if (ctrlDashboard.Visibility == Visibility.Visible)
            {
                await ctrlDashboard.YukleAsync(_acilGunEsigi);
            }
            else if (ctrlProjeler.Visibility == Visibility.Visible)
            {
                await ctrlProjeler.YukleAsync();
            }
            else if (ctrlKanban.Visibility == Visibility.Visible)
            {
                await ctrlKanban.YukleAsync();
            }
            else if (ctrlMusteriler.Visibility == Visibility.Visible)
            {
                await ctrlMusteriler.YukleAsync();
            }
            else if (ctrlCalisanlar.Visibility == Visibility.Visible)
            {
                await ctrlCalisanlar.YukleAsync();
            }
            else if (ctrlHareketler.Visibility == Visibility.Visible)
            {
                await ctrlHareketler.YukleAsync();
            }
            else if (ctrlAyarlar.Visibility == Visibility.Visible)
            {
                await ctrlAyarlar.YukleAsync(_acilGunEsigi);
            }
        }





        // ═══════════════ RAPOR İŞLEMLERİ ═══════════════

        private async void RaporTiklandi(object sender, RoutedEventArgs e)
        {
            // Filtre penceresi göster
            var filtrePenceresi = new RaporFiltrePenceresi();
            filtrePenceresi.Owner = this;

            List<Proje> projeler;
            if (filtrePenceresi.ShowDialog() == true)
            {
                projeler = filtrePenceresi.FiltrelenmisProjecteler;
            }
            else
            {
                return;
            }

            if (projeler.Count == 0)
            {
                OnayDiyalogu.Uyari("Seçilen filtrelere uygun proje bulunamadı.", "Uyarı", this);
                return;
            }

            var sonuc = OnayDiyalogu.Secim(
                "Hangi formatta rapor oluşturmak istersiniz?",
                "Rapor Formatı", "\uE9D9",
                "\uE9D9 Excel (.xlsx)", "\uE8A5 PDF (.pdf)", this);

            if (sonuc == DiyalogSonuc.Iptal) return;

            var tarihDamgasi = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var excelMi = sonuc == DiyalogSonuc.Evet;

            // Dosya kaydetme diyaloğu
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
                await YuklemeIleAsync("Rapor oluşturuluyor...", async () =>
                {
                    var musteriler = await Task.Run(() => MusteriIslemleri.TumMusterileriGetir());
                    var odemeler = await Task.Run(() => OdemeIslemleri.TumOdemeleriGetir());

                    await Task.Run(() =>
                    {
                        if (excelMi)
                        {
                            RaporOlusturucu.ExcelRaporOlustur(projeler, musteriler, odemeler, dosyaYolu);
                        }
                        else
                        {
                            RaporOlusturucu.PdfRaporOlustur(projeler, musteriler, odemeler, dosyaYolu);
                        }
                    });
                });

                OnayDiyalogu.Basari($"Rapor oluşturuldu:\n{dosyaYolu}", "Rapor Başarılı", this);
            }
            catch (Exception ex)
            {
                OnayDiyalogu.Hata($"Rapor oluşturulurken hata:\n{ex.Message}", "Rapor Hatası", this);
            }
        }



        // ═══════════════ OTOMATİK YEDEKLEME ═══════════════

        /// <summary>
        /// Otomatik yedekleme sabit anahtarı.
        /// </summary>
        private const string ANAHTAR_SON_YEDEK_TARIHI = "SonYedekTarihi";

        /// <summary>
        /// Otomatik yedekleme aralığı (gün).
        /// </summary>
        private const int OtomatikYedekAraligi = 3;

        /// <summary>
        /// Saklanacak maksimum yedek sayısı.
        /// </summary>
        private const int MaksimumYedekSayisi = 5;

        private void OtomatikYedekKontrol()
        {
            try
            {
                var sonYedekMetni = AyarIslemleri.AyarGetir(ANAHTAR_SON_YEDEK_TARIHI, string.Empty);

                if (string.IsNullOrEmpty(sonYedekMetni) ||
                    (DateTime.TryParse(sonYedekMetni, out var sonYedek) &&
                     (DateTime.Now - sonYedek).TotalDays >= OtomatikYedekAraligi))
                {
                    var kaynakDosya = VeritabaniBaglanti.VeritabaniYolu;
                    if (!File.Exists(kaynakDosya)) return;

                    var yedekDizini = Path.Combine(DosyaYollari.UygulamaVeriDizini, VeritabaniSabitleri.YedekDizinAdi);
                    Directory.CreateDirectory(yedekDizini);

                    var tarihDamgasi = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var yedekDosya = Path.Combine(yedekDizini, $"{VeritabaniSabitleri.OtomatikYedekOneki}{tarihDamgasi}.db");

                    File.Copy(kaynakDosya, yedekDosya);

                    // Son yedek tarihini kaydet
                    AyarIslemleri.AyarKaydet(ANAHTAR_SON_YEDEK_TARIHI, DateTime.Now.ToString("o"));

                    // Eski yedekleri temizle (son N tane tut)
                    var yedekDosyalari = Directory.GetFiles(yedekDizini, $"{VeritabaniSabitleri.OtomatikYedekOneki}*.db")
                        .OrderByDescending(f => f)
                        .Skip(MaksimumYedekSayisi)
                        .ToList();

                    foreach (var eskiYedek in yedekDosyalari)
                    {
                        File.Delete(eskiYedek);
                    }
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }

        // ═══════════════ TOPLU DURUM GÜNCELLEME ═══════════════

        /// <summary>
        /// Seçili projelerin durumunu toplu olarak değiştirir.
        /// </summary>




        // ═══════════════ OTURUM KİLİDİ YÖNETİMİ ═══════════════

        private void OnOturumZamanAsimiOldu()
        {
            Dispatcher.Invoke(() =>
            {
                pnlOturumKilidi.Visibility = Visibility.Visible;
                TxtKilitParola.Clear();
                TxtKilitParola.Focus();
                TxtKilitHata.Visibility = Visibility.Collapsed;
            });
        }

        private async void BtnKilitAc_Click(object sender, RoutedEventArgs e)
        {
            var parola = TxtKilitParola.Password;
            if (string.IsNullOrWhiteSpace(parola)) return;

            BtnKilitAc.IsEnabled = false;
            BtnKilitAc.Content = "Doğrulanıyor...";
            TxtKilitHata.Visibility = Visibility.Collapsed;

            var aktifMail = AyarIslemleri.AyarGetir("AktifKullaniciMail", "");
            if (string.IsNullOrEmpty(aktifMail))
            {
                BtnKilitCikis_Click(null!, null!);
                return;
            }

            var cihazId = CihazYardimcisi.GetCihazId();
            var (gecerli, yeniCihaz, ajans) = await AuthServisi.GirisIcinParolaDogrulaAsync(aktifMail, parola, cihazId);

            if (gecerli)
            {
                pnlOturumKilidi.Visibility = Visibility.Collapsed;
                TxtKilitParola.Clear();
                SessionYonetimi.Baslat(30); // Süreyi yeniden başlat
            }
            else
            {
                TxtKilitHata.Text = "Hatalı parola girdiniz.";
                TxtKilitHata.Visibility = Visibility.Visible;
            }

            BtnKilitAc.IsEnabled = true;
            BtnKilitAc.Content = "Kilidi Aç";
        }

        private void BtnKilitCikis_Click(object sender, RoutedEventArgs e)
        {
            using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
            using var komut = baglanti.CreateCommand();
            komut.CommandText = @"
                UPDATE Ayarlar SET Deger = '' WHERE Anahtar = 'SonGirisTarihi';
                UPDATE Ayarlar SET Deger = '' WHERE Anahtar = 'AktifKullaniciMail';";
            komut.ExecuteNonQuery();

            SistemYoneticisiSabitleri.YerelAdminSertifikasiSil();
            SessionYonetimi.Durdur();

            var giris = new GirisPenceresi();
            giris.Show();
            this.Close();
        }

        private void CikisYapTiklandi(object sender, RoutedEventArgs e)
        {
            var onay = OnayDiyalogu.EvetHayir("Hesabınızdan çıkış yapmak istediğinize emin misiniz?", "Çıkış Yap", this);
            if (onay)
            {
                HesabiZorlaKapatVeGirisEkraninaDon();
            }
        }

        /// <summary>
        /// Arka planda çalışırken sistem yöneticisi bu hesabı silerse, anında oturumu düşür.
        /// </summary>
        private async Task HesapSilindiMiKontrolEtAsync()
        {
            var aktifMail = AyarIslemleri.AyarGetir("AktifKullaniciMail", "");
            if (string.IsNullOrEmpty(aktifMail)) return; // Zaten giriş yapmamış veya sistemde bozuk

            bool aktifMi = await AuthServisi.HesapHalaVarMiAsync(aktifMail);
            if (!aktifMi)
            {
                // Admin silmiş! Kov onu.
                _bildirimTimer?.Stop();
                OnayDiyalogu.Hata($"Sayın {aktifMail}, hesabınız sistem yöneticisi tarafından sistemden kalıcı olarak silinmiştir. Erişiminizi kaybediyorsunuz.", "Hesap Sonlandırıldı", this);
                HesabiZorlaKapatVeGirisEkraninaDon();
            }
        }

        private void HesabiZorlaKapatVeGirisEkraninaDon()
        {
            // Son giriş tarihini ve aktif emaili sıfırla (Böylece otomatik giriş iptal olur)
            using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
            using var komut = baglanti.CreateCommand();
            komut.CommandText = @"
                UPDATE Ayarlar SET Deger = '' WHERE Anahtar = 'SonGirisTarihi';
                UPDATE Ayarlar SET Deger = '' WHERE Anahtar = 'AktifKullaniciMail';";
            komut.ExecuteNonQuery();

            // Yerel Admin Sertifikasını Sil (Ayrıcalık Düşürme)
            SistemYoneticisiSabitleri.YerelAdminSertifikasiSil();
            SessionYonetimi.Durdur();

            // Giriş penceresini aç
            var giris = new GirisPenceresi();
            giris.Show();

            // Mevcut pencereyi kapat
            this.Close();
        }

        // ═══════════════ BİLDİRİMLER DİYALOG VE SİSTEMİ ═══════════════

        private void BildirimRozetiniGuncelle()
        {
            var sayi = BildirimIslemleri.OkunmayanBildirimSayisi();
            if (sayi > 0)
            {
                bdgBildirimSayisi.Visibility = Visibility.Visible;
                txtBildirimSayisi.Text = sayi > 99 ? "99+" : sayi.ToString();
            }
            else
            {
                bdgBildirimSayisi.Visibility = Visibility.Collapsed;
            }
        }

        private void BildirimlerTiklandi(object sender, RoutedEventArgs e)
        {
            popBildirimler.IsOpen = !popBildirimler.IsOpen;
            
            if (popBildirimler.IsOpen)
            {
                var sonBildirimler = BildirimIslemleri.SonBildirimleriGetir(20);
                lstBildirimler.ItemsSource = sonBildirimler;
                
                txtBildirimYok.Visibility = sonBildirimler.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                lstBildirimler.Visibility = sonBildirimler.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void TumBildirimleriOkunduIsaretle(object sender, RoutedEventArgs e)
        {
            BildirimIslemleri.TümünüOkunduIsaretle();
            BildirimRozetiniGuncelle();
            
            // Re-render
            if (popBildirimler.IsOpen)
            {
                var sonBildirimler = BildirimIslemleri.SonBildirimleriGetir(20);
                lstBildirimler.ItemsSource = sonBildirimler;
            }
        }

        private void TumBildirimleriSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (OnayDiyalogu.EvetHayir("Tüm bildirimleri kalıcı olarak silmek istediğinize emin misiniz?", "Tüm Bildirimleri Sil", this))
            {
                BildirimIslemleri.TumBildirimleriSil();
                BildirimRozetiniGuncelle();
                
                if (popBildirimler.IsOpen)
                {
                    lstBildirimler.ItemsSource = new List<Bildirim>();
                    txtBildirimYok.Visibility = Visibility.Visible;
                    lstBildirimler.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BildirimSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (sender is Button silButon && silButon.Tag is int bildirimId)
            {
                BildirimIslemleri.BildirimSil(bildirimId);
                BildirimRozetiniGuncelle();

                if (popBildirimler.IsOpen)
                {
                    var sonBildirimler = BildirimIslemleri.SonBildirimleriGetir(20);
                    lstBildirimler.ItemsSource = sonBildirimler;
                    
                    txtBildirimYok.Visibility = sonBildirimler.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    lstBildirimler.Visibility = sonBildirimler.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }
}
