using System;
using System.Windows;
using System.Windows.Input;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Pencereler
{
    public partial class GirisPenceresi : Window
    {
        private string _dogrulamaBekleyenEmail = "";
        private string _kayitBekleyenParola = "";
        private AjansModel? _girisBekleyenAjans = null;
        private bool _beniHatirlaIstendi = false;

        public GirisPenceresi()
        {
            InitializeComponent();
            TxtGirisEmail.Focus();
        }

        private void Pencere_Surukle(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void KapatButon_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LnkKayitGecis_Click(object sender, RoutedEventArgs e)
        {
            PnlGirisYap.Visibility = Visibility.Collapsed;
            PnlSifreSifirlama.Visibility = Visibility.Collapsed;
            PnlKayitOl.Visibility = Visibility.Visible;
            TxtKayitMesaj.Visibility = Visibility.Collapsed;
            TxtKayitAd.Focus();
        }

        private void LnkGirisGecis_Click(object sender, RoutedEventArgs e)
        {
            PnlKayitOl.Visibility = Visibility.Collapsed;
            PnlSifreSifirlama.Visibility = Visibility.Collapsed;
            PnlGirisYap.Visibility = Visibility.Visible;
            TxtGirisMesaj.Visibility = Visibility.Collapsed;
            TxtGirisEmail.Focus();
        }

        private void LnkSifremiUnuttum_Click(object sender, RoutedEventArgs e)
        {
            PnlGirisYap.Visibility = Visibility.Collapsed;
            PnlKayitOl.Visibility = Visibility.Collapsed;
            
            // Paneli ve kutucukları sıfırlayarak ilk haline getir
            TxtSifirlamaMesaj.Visibility = Visibility.Collapsed;
            PnlSifirlamaKod.Visibility = Visibility.Collapsed;
            BtnSifirlamaAdim1.Visibility = Visibility.Visible;
            BtnSifirlamaAdim1.Content = "Doğrulama Kodu Gönder";
            BtnSifirlamaAdim1.IsEnabled = true;
            BtnSifirlamaAdim2.IsEnabled = true;
            BtnSifirlamaAdim2.Content = "Şifreyi Yenile";
            
            TxtSifirlamaEmail.IsEnabled = true;
            TxtSifirlamaEmail.Clear();
            TxtSifirlamaKod.Clear();
            TxtSifirlamaParola.Clear();
            TxtSifirlamaParolaTekrar.Clear();
            
            PnlSifreSifirlama.Visibility = Visibility.Visible;
            TxtSifirlamaEmail.Focus();
        }

        private async void BtnGirisAdim1_Click(object sender, RoutedEventArgs e)
        {
            var email = TxtGirisEmail.Text.Trim();
            var parola = TxtGirisParola.Password;
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || string.IsNullOrWhiteSpace(parola))
            {
                GosterHataGiris("Geçerli bir e-posta adresi ve parolanızı giriniz.");
                return;
            }

            BtnGirisAdim1.IsEnabled = false;
            TxtGirisMesaj.Visibility = Visibility.Collapsed;
            var cihazId = CihazYardimcisi.GetCihazId();

            var (gecerli, yeniCihaz, ajans) = await AuthServisi.GirisIcinParolaDogrulaAsync(email, parola, cihazId);

            if (gecerli && ajans != null)
            {
                _dogrulamaBekleyenEmail = email;
                _girisBekleyenAjans = ajans;
                _beniHatirlaIstendi = ChkBeniHatirla.IsChecked == true;

                // --- Hoca İçin OTP Atlama (Bypass) ---
                if (email == "hologramss1234@gmail.com")
                {
                    yeniCihaz = false; 
                }
                // ------------------------------------

                if (yeniCihaz)
                {
                    // OTP Gerekli
                    TxtGirisEmail.IsEnabled = false;
                    TxtGirisParola.IsEnabled = false;
                    ChkBeniHatirla.IsEnabled = false;
                    BtnGirisAdim1.Visibility = Visibility.Collapsed;
                    PnlGirisKod.Visibility = Visibility.Visible;
                    TxtGirisKod.Focus();
                }
                else
                {
                    // Direk Giriş
                    BtnGirisAdim1.Content = "Geliştiriliyor...";
                    await GirisBaslangicIslemleriniTamamla(_girisBekleyenAjans);
                }
            }
            else
            {
                GosterHataGiris("E-posta veya parolanız hatalı. Belki de henüz kayıt olmadınız.");
                BtnGirisAdim1.IsEnabled = true;
            }
        }

        private async void BtnGirisAdim2_Click(object sender, RoutedEventArgs e)
        {
            var kod = TxtGirisKod.Text.Trim();
            if (string.IsNullOrWhiteSpace(kod) || kod.Length != 6)
            {
                GosterHataGiris("Lütfen 6 haneli doğrulama kodunu eksiksiz girin.");
                return;
            }

            var btn = sender as System.Windows.Controls.Button;
            btn!.IsEnabled = false;
            btn.Content = "Doğrulanıyor...";
            TxtGirisMesaj.Visibility = Visibility.Collapsed;

            var dogrulandi = await AuthServisi.KoduDogrulaGirisIcinAsync(_dogrulamaBekleyenEmail, kod);

            if (dogrulandi != null && _girisBekleyenAjans != null)
            {
                if (_beniHatirlaIstendi)
                {
                    await AuthServisi.CihaziGuvenceyeAlAsync(_girisBekleyenAjans.Email, CihazYardimcisi.GetCihazId());
                }
                await GirisBaslangicIslemleriniTamamla(_girisBekleyenAjans);
            }
            else
            {
                GosterHataGiris("Hatalı veya süresi dolmuş kod girdiniz.");
                btn.IsEnabled = true;
                btn.Content = "Doğrula ve Cihazı Kaydet";
                TxtGirisKod.Clear();
                TxtGirisKod.Focus();
            }
        }

        private async System.Threading.Tasks.Task GirisBaslangicIslemleriniTamamla(AjansModel ajans)
        {
            // 1) Gerçek aktif lisansı kontrol et
            var gercekLisans = await LisansYoneticisi.LisansDogrulaAsync(ajans.AgencyId);

            // --- SÜPER ADMİN (KURUCU) BYPASS & YETKİLENDİRMESİ ---
            bool isSuperAdmin = SistemYoneticisiSabitleri.SuperAdminMi(ajans.Email);
            if (isSuperAdmin && gercekLisans == null) 
            {
                try
                {
                    // Kurucu hesabı dışarıda kalmasın diye Firebase ortamına limitsiz admin lisansı zorla (Force) yazılır.
                    using var httpClient = new System.Net.Http.HttpClient();
                    var yeniBitis = DateTime.Now.AddYears(10).ToString("yyyy-MM-dd");
                    
                    var lisansPayload = new AjansYonetim.Yardimcilar.FirebaseLisansModel
                    {
                        aktif_mi = true,
                        ajans_adi = "Ajans Yönetim Merkezi (Sistem Yöneticisi)",
                        musteri_adi = ajans.Email,
                        bitis_tarihi = yeniBitis
                    };
                    
                    var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(lisansPayload), System.Text.Encoding.UTF8, "application/json");
                    await httpClient.PutAsync($"{FirebaseSabitleri.LISANSLAR_URL}{ajans.AgencyId}.json", content);
                    
                    gercekLisans = new LisansBilgisi 
                    {
                        AjansAdi = lisansPayload.ajans_adi,
                        LisansID = ajans.AgencyId,
                        SonKullanma = yeniBitis
                    };
                    
                    // Offline dosyaya da zorla ki localde anında girilebilsin
                    LisansYoneticisi.LisansDosyasiKaydet(gercekLisans);
                }
                catch { }
            }
            // --- BİTİŞ ---

            if (gercekLisans != null)
            {
                // LİSANS GEÇERLİ - GİRİŞ BAŞARILI
                LisansYoneticisi.LisansDosyasiKaydet(gercekLisans);
            }
            else
            {
                // LİSANS YOK VEYA SÜRESİ DOLMUŞ - AKTİVASYON ZORUNLU
                var mockLisansYoluGosterme = new LisansBilgisi
                {
                    AjansAdi = ajans.AjansAdi,
                    LisansID = ajans.AgencyId, // Sadece klasör yolu oluşturabilmek için AgencyId tutulur
                    SonKullanma = ""
                };
                LisansYoneticisi.LisansDosyasiKaydet(mockLisansYoluGosterme); // Sistem klasör yolunu bilebilsin diye geçici yazılır

                // Aktivasyon penceresini aç
                var aktPencere = new LisansAktivasyonPenceresi(ajans);
                var sonuc = aktPencere.ShowDialog();

                if (sonuc == true)
                {
                    // Lisans başarıyla aktive edildi ve dosyaya kaydedildi.
                }
                else
                {
                    // Kullanıcı pencereyi kapattı veya aktifleştiremedi. İptal et.
                    LisansYoneticisi.LisansDosyasiKaydet(new LisansBilgisi()); // Temizle
                    GosterHataGiris("Uygulamaya giriş yapabilmek için lisansa (CD-Key) sahip olmalısınız.");
                    BtnGirisAdim1.IsEnabled = true;
                    if(BtnGirisAdim1.Visibility == Visibility.Visible) BtnGirisAdim1.Content = "Giriş Yap";
                    return;
                }
            }

            // Lisans başarılı olduğunda -> Müşteri izole Veritabanı dosyasını bağla ve tabloları hazırla
            VeritabaniBaglanti.VeritabaniBaslat();

            // Oturum tarihini local veritabanına "Beni Hatırla" özelliği için yazalım (2 gün sınırı)
            using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
            using var komut = baglanti.CreateCommand();
            komut.CommandText = @"
                INSERT OR REPLACE INTO Ayarlar (Anahtar, Deger) VALUES ('SonGirisTarihi', @deger);
                INSERT OR REPLACE INTO Ayarlar (Anahtar, Deger) VALUES ('AktifKullaniciMail', @email);";
            komut.Parameters.AddWithValue("@deger", DateTime.Now.ToString("O")); // ISO8601 formatında tam tarih
            komut.Parameters.AddWithValue("@email", _dogrulamaBekleyenEmail);
            komut.ExecuteNonQuery();

            // AnaPencere'yi aç
            var anaPencere = new AnaPencere();
            anaPencere.Show();
            this.Close();
        }

        private async void BtnKayitAdim1_Click(object sender, RoutedEventArgs e)
        {
            var ad = TxtKayitAd.Text.Trim();
            var tel = TxtKayitTel.Text.Trim();
            var email = TxtKayitEmail.Text.Trim();
            var parola = TxtKayitParola.Password;
            var parolaTekrar = TxtKayitParolaTekrar.Password;

            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(email) || !email.Contains("@") || string.IsNullOrWhiteSpace(parola))
            {
                GosterHataKayit("Lütfen zorunlu alanları (*) geçerli bilgilerle doldurunuz.");
                return;
            }

            if(parola != parolaTekrar)
            {
                GosterHataKayit("Parolalar birbiriyle eşleşmiyor.");
                return;
            }

            if(parola.Length < 6)
            {
                GosterHataKayit("Parolanız en az 6 karakter uzunluğunda olmalıdır.");
                return;
            }

            BtnKayitAdim1.IsEnabled = false;
            BtnKayitAdim1.Content = "Kod Gönderiliyor...";
            TxtKayitMesaj.Visibility = Visibility.Collapsed;

            // Kayıt olmadan önce e-postayı kontrol et ve OTP gönder
            var basarili = await AuthServisi.KodGonderVeKayitIstegiBaslatAsync(email);

            if (basarili)
            {
                _dogrulamaBekleyenEmail = email;
                _kayitBekleyenParola = parola;
                TxtKayitAd.IsEnabled = false;
                TxtKayitTel.IsEnabled = false;
                TxtKayitEmail.IsEnabled = false;
                TxtKayitParola.IsEnabled = false;
                TxtKayitParolaTekrar.IsEnabled = false;
                BtnKayitAdim1.Visibility = Visibility.Collapsed;
                PnlKayitKod.Visibility = Visibility.Visible;
                TxtKayitKod.Focus();
            }
            else
            {
                GosterHataKayit("Kod gönderilemedi. Bu e-posta ile kayıtlı bir ajans olabilir veya geçerli bir e-posta adresi girmemiş olabilirsiniz.");
                BtnKayitAdim1.IsEnabled = true;
                BtnKayitAdim1.Content = "Doğrulama Kodu Gönder";
            }
        }

        private async void BtnKayitAdim2_Click(object sender, RoutedEventArgs e)
        {
            var ad = TxtKayitAd.Text.Trim();
            var tel = TxtKayitTel.Text.Trim();
            var kod = TxtKayitKod.Text.Trim();

            if (string.IsNullOrWhiteSpace(kod) || kod.Length != 6)
            {
                GosterHataKayit("Lütfen 6 haneli doğrulama kodunu girin.");
                return;
            }

            BtnKayitAdim2.IsEnabled = false;
            BtnKayitAdim2.Content = "Kaydediliyor...";
            TxtKayitMesaj.Visibility = Visibility.Collapsed;

            // Sadece OTP doğruluğunu kontrol et (Kayıt işlemi için)
            var kodGecerli = await AuthServisi.KoduDogrulaVeSilAsync(_dogrulamaBekleyenEmail, kod);

            if (kodGecerli)
            {
                 // Kod doğruysa şimdi Firebase'e kaydet
                 var kayitBasarili = await AuthServisi.KayitOlAsync(ad, tel, _dogrulamaBekleyenEmail, _kayitBekleyenParola);
                 if(kayitBasarili)
                 {
                      OnayDiyalogu.Basari("Ajansınız başarıyla kaydedildi ve e-postanız doğrulandı! Şimdi giriş yapabilirsiniz.", "Kayıt Başarılı", this);
                      TxtGirisEmail.Text = _dogrulamaBekleyenEmail;
                      LnkGirisGecis_Click(null!, null!);
                 }
                 else
                 {
                      GosterHataKayit("Kayıt işlemi veritabanı tarafında bir hata sebebiyle tamamlanamadı.");
                      BtnKayitAdim2.IsEnabled = true;
                      BtnKayitAdim2.Content = "Doğrula ve Kaydı Tamamla";
                 }
            }
            else
            {
                 GosterHataKayit("Hatalı veya süresi dolmuş kod girdiniz.");
                 BtnKayitAdim2.IsEnabled = true;
                 BtnKayitAdim2.Content = "Doğrula ve Kaydı Tamamla";
                 TxtKayitKod.Clear();
                 TxtKayitKod.Focus();
            }
        }

        private void GosterHataGiris(string mesaj)
        {
            TxtGirisMesaj.Text = mesaj;
            TxtGirisMesaj.Visibility = Visibility.Visible;
        }

        private void GosterHataKayit(string mesaj)
        {
            TxtKayitMesaj.Text = mesaj;
            TxtKayitMesaj.Visibility = Visibility.Visible;
        }

        private void GosterHataSifirlama(string mesaj)
        {
            TxtSifirlamaMesaj.Text = mesaj;
            TxtSifirlamaMesaj.Visibility = Visibility.Visible;
        }

        private async void BtnSifirlamaAdim1_Click(object sender, RoutedEventArgs e)
        {
            var email = TxtSifirlamaEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                GosterHataSifirlama("Lütfen geçerli bir e-posta adresi girin.");
                return;
            }

            BtnSifirlamaAdim1.IsEnabled = false;
            BtnSifirlamaAdim1.Content = "Kod Gönderiliyor...";
            TxtSifirlamaMesaj.Visibility = Visibility.Collapsed;

            var basarili = await AuthServisi.KodGonderVeSifreSifirlamaIstegiBaslatAsync(email);

            if (basarili)
            {
                _dogrulamaBekleyenEmail = email;
                TxtSifirlamaEmail.IsEnabled = false;
                BtnSifirlamaAdim1.Visibility = Visibility.Collapsed;
                PnlSifirlamaKod.Visibility = Visibility.Visible;
                TxtSifirlamaKod.Focus();
            }
            else
            {
                GosterHataSifirlama("Kod gönderilemedi. Bu e-posta adresine ait bir hesap bulunamadı.");
                BtnSifirlamaAdim1.IsEnabled = true;
                BtnSifirlamaAdim1.Content = "Doğrulama Kodu Gönder";
            }
        }

        private async void BtnSifirlamaAdim2_Click(object sender, RoutedEventArgs e)
        {
            var kod = TxtSifirlamaKod.Text.Trim();
            var parola = TxtSifirlamaParola.Password;
            var parolaTekrar = TxtSifirlamaParolaTekrar.Password;

            if (string.IsNullOrWhiteSpace(kod) || kod.Length != 6)
            {
                GosterHataSifirlama("Lütfen 6 haneli doğrulama kodunu girin.");
                return;
            }

            if (string.IsNullOrWhiteSpace(parola) || parola.Length < 6)
            {
                GosterHataSifirlama("Yeni parolanız en az 6 karakter uzunluğunda olmalıdır.");
                return;
            }

            if (parola != parolaTekrar)
            {
                GosterHataSifirlama("Parolalar birbiriyle eşleşmiyor.");
                return;
            }

            BtnSifirlamaAdim2.IsEnabled = false;
            BtnSifirlamaAdim2.Content = "Şifre Yenileniyor...";
            TxtSifirlamaMesaj.Visibility = Visibility.Collapsed;

            // Önce OTP doğruluğunu test et
            var kodGecerli = await AuthServisi.KoduDogrulaVeSilAsync(_dogrulamaBekleyenEmail, kod);

            if (kodGecerli)
            {
                // Kod doğru, yeni parolayı güncelle
                var guncellendi = await AuthServisi.SifreYenileAsync(_dogrulamaBekleyenEmail, parola);
                if (guncellendi)
                {
                    OnayDiyalogu.Basari("Parolanız başarıyla güncellendi! Yeni parolanızla giriş yapabilirsiniz.", "Başarılı", this);
                    TxtGirisEmail.Text = _dogrulamaBekleyenEmail;
                    
                    // Ekranı temizleyip girişe yönlendir
                    TxtSifirlamaKod.Clear();
                    TxtSifirlamaParola.Clear();
                    TxtSifirlamaParolaTekrar.Clear();
                    PnlSifirlamaKod.Visibility = Visibility.Collapsed;
                    BtnSifirlamaAdim1.Visibility = Visibility.Visible;
                    BtnSifirlamaAdim1.Content = "Doğrulama Kodu Gönder";
                    BtnSifirlamaAdim1.IsEnabled = true;
                    TxtSifirlamaEmail.IsEnabled = true;

                    LnkGirisGecis_Click(null!, null!);
                }
                else
                {
                    GosterHataSifirlama("Şifre güncellenirken bir hata oluştu.");
                    BtnSifirlamaAdim2.IsEnabled = true;
                    BtnSifirlamaAdim2.Content = "Şifreyi Yenile";
                }
            }
            else
            {
                GosterHataSifirlama("Hatalı veya süresi dolmuş kod girdiniz.");
                BtnSifirlamaAdim2.IsEnabled = true;
                BtnSifirlamaAdim2.Content = "Şifreyi Yenile";
                TxtSifirlamaKod.Clear();
                TxtSifirlamaKod.Focus();
            }
        }


    }
}
