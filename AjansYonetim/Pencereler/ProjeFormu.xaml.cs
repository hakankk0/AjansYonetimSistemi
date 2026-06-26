using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Proje ekleme ve düzenleme formu.
    /// </summary>
    public partial class ProjeFormu : Window
    {
        /// <summary>
        /// Düzenleme modunda olan proje (null ise yeni ekleme).
        /// </summary>
        private readonly Proje? _duzenlenenProje;

        /// <summary>
        /// Yeni proje ekleme modu.
        /// </summary>
        public ProjeFormu()
        {
            InitializeComponent();
            _duzenlenenProje = null;
            txtFormBaslik.Text = "Yeni Proje Ekle";

            FormuHazirla();

            // Varsayılan değerler
            dpBaslangic.SelectedDate = DateTime.Today;
            dpTeslim.SelectedDate = DateTime.Today.AddDays(7);
            cmbDurum.SelectedIndex = 0;
            cmbKategori.SelectedIndex = 0;
        }

        /// <summary>
        /// Mevcut projeyi düzenleme modu.
        /// </summary>
        public ProjeFormu(Proje proje)
        {
            InitializeComponent();
            _duzenlenenProje = proje;
            txtFormBaslik.Text = "Proje Düzenle";

            FormuHazirla();

            // Alanları mevcut verilerle doldur
            txtProjeAdi.Text = proje.ProjeAdi;
            dpBaslangic.SelectedDate = proje.BaslangicTarihi;
            dpTeslim.SelectedDate = proje.TeslimTarihi;
            txtFiyat.Text = FiyatYardimci.Formatla(proje.Fiyat);

            // Müşteriyi seç
            var musteriler = cmbMusteri.ItemsSource as System.Collections.Generic.List<Musteri>;
            if (musteriler != null)
            {
                cmbMusteri.SelectedItem = musteriler.FirstOrDefault(m => m.MusteriID == proje.MusteriID);
            }

            // Durumu seç
            cmbDurum.SelectedItem = proje.Durum;

            // Kategoriyi seç
            if (!string.IsNullOrEmpty(proje.Kategori))
            {
                cmbKategori.SelectedItem = proje.Kategori;
            }
            else
            {
                cmbKategori.SelectedIndex = 0;
            }

            // Para birimini seç
            cmbParaBirimi.SelectedItem = proje.ParaBirimi;
        }

        /// <summary>
        /// Form ComboBox'larını doldurur.
        /// </summary>
        private void FormuHazirla()
        {
            // Müşteri listesini doldur
            var musteriler = MusteriIslemleri.TumMusterileriGetir();
            cmbMusteri.ItemsSource = musteriler;

            // Durum listesini doldur
            cmbDurum.ItemsSource = ProjeDurumlari.TumDurumlar;

            // Kategori listesini doldur
            cmbKategori.ItemsSource = ProjeKategorileri.TumKategoriler;

            // Para birimi listesini doldur
            cmbParaBirimi.ItemsSource = ParaBirimleri.TumParaBirimleri;
            cmbParaBirimi.SelectedItem = ParaBirimleri.VARSAYILAN;

            // Müşteri seçimi değişince para birimini güncelle
            cmbMusteri.SelectionChanged += MusteriSecildiParaBirimiGuncelle;

            // Şablon listesini veritabanından doldur
            SablonListesiniYenile();
        }

        /// <summary>
        /// Şablon ComboBox'unu veritabanından yeniden yükler.
        /// </summary>
        private void SablonListesiniYenile()
        {
            const string bosSecim = "—  Şablon Seçin  —";
            var sablonListesi = new System.Collections.Generic.List<object> { bosSecim };
            sablonListesi.AddRange(SablonIslemleri.TumSablonlariGetir());
            cmbSablon.ItemsSource = sablonListesi;
            cmbSablon.SelectedIndex = 0;
        }

        /// <summary>
        /// Kaydet butonuna tıklandığında çağrılır.
        /// </summary>
        private async void KaydetTiklandi(object sender, RoutedEventArgs e)
        {
            // Doğrulama kontrolleri
            if (cmbMusteri.SelectedItem is not Musteri secilenMusteri)
            {
                OnayDiyalogu.Uyari("Lütfen bir müşteri seçin.", "Doğrulama Hatası", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtProjeAdi.Text))
            {
                OnayDiyalogu.Uyari("Proje adı zorunludur.", "Doğrulama Hatası", this);
                return;
            }

            if (dpBaslangic.SelectedDate == null || dpTeslim.SelectedDate == null)
            {
                OnayDiyalogu.Uyari("Başlangıç ve teslim tarihleri zorunludur.", "Doğrulama Hatası", this);
                return;
            }

            if (dpTeslim.SelectedDate < dpBaslangic.SelectedDate)
            {
                OnayDiyalogu.Uyari("Teslim tarihi başlangıç tarihinden önce olamaz.", "Doğrulama Hatası", this);
                return;
            }

            if (!FiyatYardimci.Parse(txtFiyat.Text, out var fiyat))
            {
                OnayDiyalogu.Uyari("Geçerli bir fiyat girin.", "Doğrulama Hatası", this);
                return;
            }

            if (cmbDurum.SelectedItem is not string secilenDurum)
            {
                OnayDiyalogu.Uyari("Lütfen bir durum seçin.", "Doğrulama Hatası", this);
                return;
            }

            var secilenParaBirimi = cmbParaBirimi.SelectedItem as string ?? ParaBirimleri.VARSAYILAN;
            var anlasmaKuru = 1.0m;

            // Eğer yeni proje ekleniyorsa veya projeyi düzenlerken para birimi değiştirildiyse taze kur çekilir.
            bool yeniKurGerekli = _duzenlenenProje == null || _duzenlenenProje.ParaBirimi != secilenParaBirimi;

            if (ParaBirimleri.DovizMi(secilenParaBirimi))
            {
                if (yeniKurGerekli)
                {
                    // Arka planda 1 saatte bir güncellenen (DovizKurServisi.BaslatArkaPlanSenkronizasyonu) kur kullanılacağı için bekleme yok.
                    anlasmaKuru = DovizKurServisi.KurGetir(secilenParaBirimi);
                    
                    if (anlasmaKuru <= 0)
                    {
                        // İstisnai durum: ilk açılışta henüz arka plan servisi çekemediyse manuel bekle
                        btnKaydet.IsEnabled = false;
                        var eskiIcerik = btnKaydet.Content;
                        btnKaydet.Content = new System.Windows.Controls.StackPanel
                        {
                            Orientation = System.Windows.Controls.Orientation.Horizontal,
                            Children =
                            {
                                new System.Windows.Controls.TextBlock { Text = "\uE916", FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,6,0) },
                                new System.Windows.Controls.TextBlock { Text = "Kur Çekiliyor...", VerticalAlignment = VerticalAlignment.Center }
                            }
                        };
                        try
                        {
                            await DovizKurServisi.KurlariGuncelleAsync(zorlaGuncelle: true);
                            anlasmaKuru = DovizKurServisi.KurGetir(secilenParaBirimi);
                        }
                        finally
                        {
                            btnKaydet.Content = eskiIcerik;
                            btnKaydet.IsEnabled = true;
                        }
                    }
                }
                else
                {
                    // Sadece var olan projeyi düzenliyorsak ve para birimi değişmediyse, anlaşma kurunu koru! (Aksi takdirde eski muhasebe kayıtları ezilir)
                    anlasmaKuru = _duzenlenenProje!.AnlasmaKuru;
                }
            }

            var proje = new Proje
            {
                MusteriID = secilenMusteri.MusteriID,
                ProjeAdi = txtProjeAdi.Text.Trim(),
                BaslangicTarihi = dpBaslangic.SelectedDate.Value,
                TeslimTarihi = dpTeslim.SelectedDate.Value,
                Fiyat = fiyat,
                Durum = secilenDurum,
                Kategori = cmbKategori.SelectedItem as string ?? string.Empty,
                ParaBirimi = secilenParaBirimi,
                AnlasmaKuru = anlasmaKuru
            };

            bool basarili;

            if (_duzenlenenProje != null)
            {
                var odemeler = OdemeIslemleri.ProjeOdemeleriniGetir(_duzenlenenProje.ProjeID);
                if (odemeler.Any())
                {
                    var odemeToplam = odemeler.Sum(o => o.Tutar);
                    
                    if (fiyat < odemeToplam)
                    {
                        var sembol = ParaBirimleri.SembolGetir(secilenParaBirimi);
                        OnayDiyalogu.Uyari($"Proje fiyatı, bugüne kadar alınan toplam ödemeden ({sembol}{odemeToplam:N2}) daha düşük olamaz.", "Doğrulama Hatası", this);
                        return;
                    }

                    if (secilenParaBirimi != _duzenlenenProje.ParaBirimi)
                    {
                        OnayDiyalogu.Uyari("Bu proje için daha önceden ödeme alındığından para birimi değiştirilemez. Lütfen önce ödemeleri silin.", "Doğrulama Hatası", this);
                        return;
                    }
                }

                proje.ProjeID = _duzenlenenProje.ProjeID;
                basarili = ProjeIslemleri.ProjeGuncelle(proje, _duzenlenenProje.Durum);
            }
            else
            {
                basarili = ProjeIslemleri.ProjeEkle(proje);
            }

            if (!basarili)
            {
                OnayDiyalogu.Hata("Proje kaydedilirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            // Başarılı ise Aktivite Logu at
            if (_duzenlenenProje != null)
            {
                AktiviteIslemleri.AktiviteEkle($"'{proje.ProjeAdi}' projesi güncellendi.", "\uE70F");
            }
            else
            {
                AktiviteIslemleri.AktiviteEkle($"'{proje.ProjeAdi}' adında yeni proje eklendi.", "\uE710");
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// İptal butonuna tıklandığında çağrılır.
        /// </summary>
        private void Pencere_Surukle(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void IptalTiklandi(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Fiyat alanında sadece rakam, virgül ve nokta girilmesine izin verir.
        /// </summary>
        private void FiyatSadeceRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            GirisDogrulama.SadeceParaKarakteri(e);
        }

        /// <summary>
        /// Şablon seçildiğinde form alanlarını otomatik doldurur.
        /// </summary>
        private void SablonSecildi(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (cmbSablon.SelectedItem is not Modeller.ProjeSablonu sablon) return;

            txtProjeAdi.Text = sablon.Ad;
            dpBaslangic.SelectedDate = DateTime.Today;
            dpTeslim.SelectedDate = DateTime.Today.AddDays(sablon.VarsayilanSureGun);
            txtFiyat.Text = FiyatYardimci.Formatla(sablon.TahminiFiyat);
            cmbDurum.SelectedIndex = 0;
        }

        /// <summary>
        /// Yeni şablon ekleme penceresi açar.
        /// </summary>
        private void YeniSablonTiklandi(object sender, RoutedEventArgs e)
        {
            var form = new SablonFormu { Owner = this };
            if (form.ShowDialog() == true)
            {
                SablonListesiniYenile();
            }
        }

        /// <summary>
        /// Seçili şablonu düzenleme penceresi açar.
        /// </summary>
        private void SablonDuzenleTiklandi(object sender, RoutedEventArgs e)
        {
            if (cmbSablon.SelectedItem is not Modeller.ProjeSablonu sablon)
            {
                OnayDiyalogu.Uyari("Lütfen düzenlenecek bir şablon seçin.", "Uyarı", this);
                return;
            }

            var form = new SablonFormu(sablon) { Owner = this };
            if (form.ShowDialog() == true)
            {
                SablonListesiniYenile();
            }
        }

        /// <summary>
        /// Seçili şablonu siler.
        /// </summary>
        private void SablonSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (cmbSablon.SelectedItem is not Modeller.ProjeSablonu sablon)
            {
                OnayDiyalogu.Uyari("Lütfen silinecek bir şablon seçin.", "Uyarı", this);
                return;
            }

            if (OnayDiyalogu.EvetHayir(
                $"\"{sablon.Ad}\" şablonunu silmek istediğinize emin misiniz?",
                "Şablon Silme", this))
            {
                SablonIslemleri.SablonSil(sablon.SablonID);
                SablonListesiniYenile();
            }
        }

        // ═══════════════ PARA BİRİMİ İŞLEMLERİ ═══════════════

        /// <summary>
        /// Müşteri seçildiğinde yurt dışı ise otomatik USD seçer.
        /// </summary>
        private void MusteriSecildiParaBirimiGuncelle(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (cmbMusteri.SelectedItem is not Musteri secilen) return;

            if (secilen.MusteriTuru == MusteriTurleri.YURT_DISI)
            {
                cmbParaBirimi.SelectedItem = ParaBirimleri.USD;
            }
            else
            {
                cmbParaBirimi.SelectedItem = ParaBirimleri.TL;
            }
        }

        /// <summary>
        /// Para birimi değiştiğinde fiyat etiketini ve kur bilgisini günceller.
        /// </summary>
        private void ParaBirimiDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var paraBirimi = cmbParaBirimi.SelectedItem as string ?? ParaBirimleri.VARSAYILAN;
            var sembol = ParaBirimleri.SembolGetir(paraBirimi);

            txtFiyatEtiketi.Text = $"Fiyat ({sembol}) *";

            if (ParaBirimleri.DovizMi(paraBirimi) && DovizKurServisi.KurlarYuklendi)
            {
                var kur = DovizKurServisi.KurGetir(paraBirimi);
                txtKurBilgisi.Text = $"\uE8AB 1{sembol} = {kur:N2}\u20ba (g\u00fcncel kur)";
            }
            else
            {
                txtKurBilgisi.Text = string.Empty;
            }
        }
    }
}
