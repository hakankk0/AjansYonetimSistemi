using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;
using AjansYonetim.Donusturuculer;

namespace AjansYonetim.Pencereler.KullaniciKontrolleri
{
    public partial class AyarlarKontrol : UserControl
    {
        public Func<string, Func<Task>, Task>? Yukleyici { get; set; }
        public Action? VerilerGuncellendi { get; set; }
        public Action<LisansBilgisi>? LisansGuncellendi { get; set; }
        public Action<int>? AcilEsikGuncellendi { get; set; }
        public int GuncelAcilEsigi { get; set; } = 2;

        public AyarlarKontrol()
        {
            InitializeComponent();
        }

        public async Task YukleAsync(int acilGunEsigi)
        {
            this.GuncelAcilEsigi = acilGunEsigi;
            
            var lisans = LisansYoneticisi.MevcutLisans;
            if (lisans != null)
            {
                txtAjansAdi.Text = lisans.AjansAdi;
                if (DateTime.TryParse(lisans.SonKullanma, out var sonKullanma))
                {
                    txtLisansDurumu.Text = $"✅ Lisans aktif — {sonKullanma:dd.MM.yyyy} tarihine kadar geçerli";
                }
                else
                {
                    txtLisansDurumu.Text = "✅ Lisans aktif";
                }
            }
            txtAjansTelefon.Text = AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_AJANS_TELEFON);
            txtAjansEposta.Text = AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_AJANS_EPOSTA);
            txtAjansAdres.Text = AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_AJANS_ADRES);
            txtAcilGunEsigi.Text = GuncelAcilEsigi.ToString();

            // Yedekleme aralığı ComboBox'unu doldur
            cmbYedeklemeAraligi.ItemsSource = new[] { "Her gün", "3 günde bir", "Haftada bir", "Ayda bir", "Yedekleme" };
            var kayitliAralik = AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_YEDEKLEME_ARALIGI, "Her gün");
            cmbYedeklemeAraligi.SelectedItem = kayitliAralik;
            if (cmbYedeklemeAraligi.SelectedItem == null)
                cmbYedeklemeAraligi.SelectedIndex = 0;

            // Bulut Yedekleme Durumunu Sorgula
            txtBulutYedekDurumu.Text = "Bulut durumu kontrol ediliyor...";
            txtBulutYedekDurumu.Foreground = (System.Windows.Media.Brush)FindResource("BilgiFirca");
            
            var sonYedekTarihi = await BulutServisi.BulutYedekTarihiSorgulaAsync();
            if (string.IsNullOrEmpty(sonYedekTarihi))
            {
                txtBulutYedekDurumu.Text = "Bulutta kayıtlı bir yedeğiniz bulunmuyor.";
                txtBulutYedekDurumu.Foreground = (System.Windows.Media.Brush)FindResource("UyariFirca");
            }
            else
            {
                txtBulutYedekDurumu.Text = $"Buluttaki Son Yedek: {sonYedekTarihi}";
                txtBulutYedekDurumu.Foreground = (System.Windows.Media.Brush)FindResource("BasariFirca");
            }
        }

        private void AjansBilgileriKaydetTiklandi(object sender, RoutedEventArgs e)
        {
            AyarIslemleri.AyarKaydet(AyarIslemleri.ANAHTAR_AJANS_TELEFON, txtAjansTelefon.Text.Trim());
            AyarIslemleri.AyarKaydet(AyarIslemleri.ANAHTAR_AJANS_EPOSTA, txtAjansEposta.Text.Trim());
            AyarIslemleri.AyarKaydet(AyarIslemleri.ANAHTAR_AJANS_ADRES, txtAjansAdres.Text.Trim());

            OnayDiyalogu.Basari("Ajans bilgileri veritabanına kaydedildi.", "Bilgi", Window.GetWindow(this));
        }

        private void LisansDegistirTiklandi(object sender, RoutedEventArgs e)
        {
            var mevcutLisans = LisansYoneticisi.MevcutLisans;
            if (mevcutLisans == null) return;
            
            var ajansModel = new AjansModel 
            {
                AgencyId = mevcutLisans.LisansID,
                AjansAdi = mevcutLisans.AjansAdi,
                Email = AyarIslemleri.AyarGetir("AktifKullaniciMail") ?? ""
            };

            var aktivasyonPenceresi = new LisansAktivasyonPenceresi(ajansModel) { Owner = Window.GetWindow(this) };
            if (aktivasyonPenceresi.ShowDialog() == true)
            {
                var lisans = LisansYoneticisi.MevcutLisans;
                if (lisans != null)
                {
                    txtAjansAdi.Text = lisans.AjansAdi;
                    if (DateTime.TryParse(lisans.SonKullanma, out var sonKullanma))
                    {
                        txtLisansDurumu.Text = $"✅ Lisans aktif — {sonKullanma:dd.MM.yyyy} tarihine kadar geçerli";
                    }
                    else
                    {
                        txtLisansDurumu.Text = "✅ Lisans aktif";
                    }

                    OnayDiyalogu.Basari($"Lisans başarıyla değiştirildi!\nAjans: {lisans.AjansAdi}", "Lisans", Window.GetWindow(this));
                    LisansGuncellendi?.Invoke(lisans);
                }
            }
        }

        private void UygulamaAyarlariKaydetTiklandi(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtAcilGunEsigi.Text, out var yeniEsik) && yeniEsik > 0)
            {
                AyarIslemleri.AyarKaydet(AyarIslemleri.ANAHTAR_ACIL_GUN_ESIGI, yeniEsik.ToString());
                if (cmbYedeklemeAraligi.SelectedItem != null)
                {
                    AyarIslemleri.AyarKaydet(AyarIslemleri.ANAHTAR_YEDEKLEME_ARALIGI, cmbYedeklemeAraligi.SelectedItem.ToString() ?? "Her gün");
                }

                OnayDiyalogu.Basari($"Acil proje uyarı eşiği {yeniEsik} gün olarak kaydedildi.", "Bilgi", Window.GetWindow(this));
                TeslimTarihiRenkDonusturucu.OnbellegiTemizle();
                
                AcilEsikGuncellendi?.Invoke(yeniEsik);
            }
            else
            {
                OnayDiyalogu.Uyari("Geçerli bir gün sayısı girin (1 veya daha büyük).", "Doğrulama Hatası", Window.GetWindow(this));
            }
        }

        private void VeritabaniKonumuTiklandi(object sender, RoutedEventArgs e)
        {
            var veritabaniDizini = DosyaYollari.UygulamaVeriDizini;
            System.Diagnostics.Process.Start("explorer.exe", veritabaniDizini);
        }

        private void YedekAlTiklandi(object sender, RoutedEventArgs e)
        {
            var kaynakDosya = VeritabaniBaglanti.VeritabaniYolu;

            if (!File.Exists(kaynakDosya))
            {
                OnayDiyalogu.Hata("Veritabanı dosyası bulunamadı.", "Hata", Window.GetWindow(this));
                return;
            }

            try
            {
                var yedekDizini = Path.Combine(DosyaYollari.UygulamaVeriDizini, VeritabaniSabitleri.YedekDizinAdi);
                Directory.CreateDirectory(yedekDizini);

                var tarihDamgasi = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var yedekDosya = Path.Combine(yedekDizini, $"{VeritabaniSabitleri.ManuelYedekOneki}{tarihDamgasi}.db");

                File.Copy(kaynakDosya, yedekDosya);
                OnayDiyalogu.Basari($"Veritabanı yedeği alındı:\n{yedekDosya}", "Yedekleme Başarılı", Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                OnayDiyalogu.Hata($"Yedekleme sırasında hata oluştu:\n{ex.Message}", "Yedekleme Hatası", Window.GetWindow(this));
            }
        }

        private void YedekGeriYukleTiklandi(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Yedek Dosyası Seç",
                Filter = "SQLite Veritabanı (*.db)|*.db",
                DefaultExt = ".db",
                InitialDirectory = Path.Combine(DosyaYollari.UygulamaVeriDizini, VeritabaniSabitleri.YedekDizinAdi)
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

            if (!OnayDiyalogu.EvetHayir(
                "⚠️ Dikkat: Mevcut veritabanı seçtiğiniz yedekle değiştirilecektir.\n\n" +
                "Bu işlem geri alınamaz. Devam etmek istiyor musunuz?\n\n" +
                $"Seçilen yedek: {Path.GetFileName(dialog.FileName)}",
                "Geri Yükleme Onayı", Window.GetWindow(this)))
            {
                return;
            }

            try
            {
                var hedefDosya = VeritabaniBaglanti.VeritabaniYolu;
                File.Copy(dialog.FileName, hedefDosya, overwrite: true);

                OnayDiyalogu.Basari(
                    "Veritabanı başarıyla geri yüklendi.\n\nDeğişikliklerin etkili olması için uygulamayı yeniden başlatın.",
                    "Geri Yükleme Başarılı", Window.GetWindow(this));
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                OnayDiyalogu.Hata($"Geri yükleme sırasında hata oluştu:\n{ex.Message}", "Geri Yükleme Hatası", Window.GetWindow(this));
            }
        }

        private async void BulutaYedekleTiklandi(object sender, RoutedEventArgs e)
        {
            if (LisansYoneticisi.MevcutLisans == null)
            {
                OnayDiyalogu.Uyari("Lütfen önce lisansınızı aktifleştirin.", "Lisans Bulunamadı", Window.GetWindow(this));
                return;
            }

            if (Yukleyici != null)
            {
                await Yukleyici("Veritabanı buluta yükleniyor. Lütfen bekleyin...", async () =>
                {
                    var basariliMi = await BulutServisi.YedekleAsync();
                    if (basariliMi)
                    {
                        OnayDiyalogu.Basari("Harika! Verileriniz başarıyla buluta kopyalandı.", "Bulut Yedekleme", Window.GetWindow(this));
                        await YukleAsync(this.GuncelAcilEsigi);
                    }
                    else
                    {
                        OnayDiyalogu.Hata("Yedekleme sırasında bir sorun oluştu. İnternet bağlantınızı kontrol edin.", "Hata", Window.GetWindow(this));
                    }
                });
            }
        }

        private async void BuluttanKurtarTiklandi(object sender, RoutedEventArgs e)
        {
             if (LisansYoneticisi.MevcutLisans == null)
            {
                OnayDiyalogu.Uyari("Lütfen önce lisansınızı aktifleştirin.", "Lisans Bulunamadı", Window.GetWindow(this));
                return;
            }

            if (!OnayDiyalogu.EvetHayir(
                "⚠️ EMİN MİSİNİZ?\n\n" +
                "Bu bilgisayardaki tüm mevcut müşteri ve proje verileriniz silinip, tamamen buluttaki yedeğiniz ile değiştirilecek.\n\n" +
                "Devam etmek istediğinize emin misiniz?",
                "BULUTTAN GERİ YÜKLEME ONAYI", Window.GetWindow(this)))
            {
                return;
            }

            if (Yukleyici != null)
            {
                await Yukleyici("Veritabanı buluttan indiriliyor. Lütfen bekleyin...", async () =>
                {
                    var basariliMi = await BulutServisi.YedektenDonAsync();
                    if (basariliMi)
                    {
                        OnayDiyalogu.Basari("İşlem Başarılı! Verileriniz buluttan çekildi. Değişikliklerin aktif olması için program yeniden başlatılacak.", "Geri Yükleme Tamamlandı", Window.GetWindow(this));
                        System.Diagnostics.Process.Start(Environment.ProcessPath ?? "AjansYonetim.exe");
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        OnayDiyalogu.Hata("Buluttan geri yükleme başarısız oldu. Kayıtlı bir yedeğiniz olmayabilir veya internet bağlantınız kopmuş olabilir.", "Hata", Window.GetWindow(this));
                    }
                });
            }
        }

        private void AcilEsikSadeceRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            GirisDogrulama.SadeceRakam(e);
        }

        private void TelefonSadeceRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            GirisDogrulama.SadeceRakam(e);
        }

        private void MusteriDisaAktarTiklandi(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Müşteri Verilerini Dışa Aktar",
                Filter = "CSV Dosyası (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = $"Musteriler_{DateTime.Now:yyyyMMdd}",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            {
                var musteriler = MusteriIslemleri.TumMusterileriGetir();
                CsvIslemleri.MusterileriDisaAktar(musteriler, dialog.FileName);
                OnayDiyalogu.Basari($"Müşteri verileri başarıyla dışa aktarıldı.\n{dialog.FileName}", "Dışa Aktarma", Window.GetWindow(this));
            }
        }

        private void ProjeDisaAktarTiklandi(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Proje Verilerini Dışa Aktar",
                Filter = "CSV Dosyası (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = $"Projeler_{DateTime.Now:yyyyMMdd}",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            {
                var projeler = ProjeIslemleri.TumProjeleriGetir();
                CsvIslemleri.ProjeleriDisaAktar(projeler, dialog.FileName);
                OnayDiyalogu.Basari($"Proje verileri başarıyla dışa aktarıldı.\n{dialog.FileName}", "Dışa Aktarma", Window.GetWindow(this));
            }
        }

        private void MusteriIceAktarTiklandi(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Müşteri Verilerini İçe Aktar",
                Filter = "CSV Dosyası (*.csv)|*.csv",
                DefaultExt = ".csv"
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            {
                var musteriler = CsvIslemleri.MusterileriIceAktar(dialog.FileName);

                if (musteriler.Count == 0)
                {
                    OnayDiyalogu.Uyari("CSV dosyasında geçerli müşteri verisi bulunamadı.", "İçe Aktarma", Window.GetWindow(this));
                    return;
                }

                if (OnayDiyalogu.EvetHayir($"{musteriler.Count} müşteri verisi bulundu. İçe aktarmak istiyor musunuz?", "İçe Aktarma", Window.GetWindow(this)))
                {
                    var basarili = 0;
                    foreach (var musteri in musteriler)
                    {
                        if (MusteriIslemleri.MusteriEkle(musteri)) basarili++;
                    }

                    OnayDiyalogu.Basari($"{basarili}/{musteriler.Count} müşteri başarıyla içe aktarıldı.", "İçe Aktarma", Window.GetWindow(this));
                    VerilerGuncellendi?.Invoke();
                }
            }
        }
    }
}
