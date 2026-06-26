using System;
using System.Collections.Generic;
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
    /// Fatura/Teklif oluşturma penceresi.
    /// </summary>
    public partial class FaturaOlusturPenceresi : Window
    {
        /// <summary>
        /// "Proje seçilmedi" placeholder metni.
        /// </summary>
        private const string PROJE_YOK_METNI = "(Projesiz)";

        private List<Musteri> _musteriler = new();
        private List<Proje> _projeler = new();

        /// <summary>
        /// Seçili projenin para birimi.
        /// </summary>
        private string _secilenParaBirimi = ParaBirimleri.VARSAYILAN;

        /// <summary>
        /// Opsiyonel: belirli bir müşteri için önceden seçilmiş pencere açar.
        /// </summary>
        private readonly int? _onSeciliMusteriID;

        public FaturaOlusturPenceresi(int? musteriID = null)
        {
            InitializeComponent();
            _onSeciliMusteriID = musteriID;

            // Belge türü
            cmbBelgeTuru.ItemsSource = FaturaSabitleri.TumTurler;
            cmbBelgeTuru.SelectedIndex = 0;

            // KDV oranları
            cmbKDV.ItemsSource = FaturaSabitleri.KDVOranlari.Select(k => $"%{k}").ToList();
            // Varsayılan KDV oranı seçimi
            var varsayilanIndex = Array.IndexOf(FaturaSabitleri.KDVOranlari, FaturaSabitleri.VARSAYILAN_KDV_ORANI);
            cmbKDV.SelectedIndex = varsayilanIndex >= 0 ? varsayilanIndex : 0;

            // Tarih — bugün
            dpTarih.SelectedDate = DateTime.Today;

            // Müşterileri yükle
            _musteriler = MusteriIslemleri.TumMusterileriGetir();
            cmbMusteri.ItemsSource = _musteriler;
            cmbMusteri.DisplayMemberPath = "AdSoyad";

            // Önceden seçili müşteri
            if (_onSeciliMusteriID.HasValue)
            {
                var secilen = _musteriler.FirstOrDefault(m => m.MusteriID == _onSeciliMusteriID.Value);
                if (secilen != null)
                    cmbMusteri.SelectedItem = secilen;
            }

            // Para birimleri
            cmbParaBirimi.ItemsSource = ParaBirimleri.TumParaBirimleri;
            cmbParaBirimi.SelectedItem = ParaBirimleri.VARSAYILAN;
            _secilenParaBirimi = ParaBirimleri.VARSAYILAN;
        }

        // ═══════════════ OLAY YÖNETİCİLERİ ═══════════════

        private void BelgeTuruDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var tur = cmbBelgeTuru.SelectedItem as string ?? FaturaSabitleri.FATURA;
            Title = tur == FaturaSabitleri.TEKLIF ? "📄 Fiyat Teklifi Oluştur" : "📄 Fatura Oluştur";
        }

        private void MusteriDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            cmbProje.ItemsSource = null;
            cmbProje.SelectedIndex = -1;

            if (cmbMusteri.SelectedItem is not Musteri secilen) return;

            // Müşterinin projelerini yükle
            _projeler = ProjeIslemleri.MusterininProjeleriniGetir(secilen.MusteriID);

            var projeListesi = new List<string> { PROJE_YOK_METNI };
            projeListesi.AddRange(_projeler.Select(p => p.ProjeAdi));

            cmbProje.ItemsSource = projeListesi;
            cmbProje.SelectedIndex = 0;

            // İlk projenin fiyatını otomatik doldur
            if (_projeler.Count > 0)
            {
                cmbProje.SelectedIndex = 1; // İlk gerçek projeyi seç
            }

            // Müşteri türüne göre para birimini ayarla
            cmbParaBirimi.SelectedItem = secilen.MusteriTuru == MusteriTurleri.YURT_DISI
                ? ParaBirimleri.USD
                : ParaBirimleri.VARSAYILAN;
        }

        private void ProjeDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (cmbProje.SelectedIndex > 0 && cmbProje.SelectedIndex - 1 < _projeler.Count)
            {
                var secilenProje = _projeler[cmbProje.SelectedIndex - 1];
                cmbParaBirimi.SelectedItem = secilenProje.ParaBirimi;
            }
        }

        private void ParaBirimiDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (cmbParaBirimi.SelectedItem is string secilenBirim)
            {
                _secilenParaBirimi = secilenBirim;
                ToplamHesapla();
            }
        }

        private void TutarDegisti(object sender, TextChangedEventArgs e)
        {
            ToplamHesapla();
        }

        private void KDVDegisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ToplamHesapla();
        }

        private void FiyatSadeceRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            GirisDogrulama.SadeceParaKarakteri(e);
        }

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

        private void PdfOlusturTiklandi(object sender, RoutedEventArgs e)
        {
            // Doğrulama
            if (cmbMusteri.SelectedItem is not Musteri secilenMusteri)
            {
                OnayDiyalogu.Uyari("Lütfen bir müşteri seçin.", "Uyarı", this);
                return;
            }

            if (!decimal.TryParse(txtTutar.Text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var tutar) || tutar <= 0)
            {
                OnayDiyalogu.Uyari("Lütfen geçerli bir tutar girin.", "Uyarı", this);
                return;
            }

            if (dpTarih.SelectedDate == null)
            {
                OnayDiyalogu.Uyari("Lütfen bir tarih seçin.", "Uyarı", this);
                return;
            }

            // Proje seçimi
            int? projeID = null;
            string projeAdi = string.Empty;
            if (cmbProje.SelectedIndex > 0 && cmbProje.SelectedIndex - 1 < _projeler.Count)
            {
                var secilenProje = _projeler[cmbProje.SelectedIndex - 1];
                projeID = secilenProje.ProjeID;
                projeAdi = secilenProje.ProjeAdi;
            }

            // KDV hesapla
            var kdvOrani = SecilenKDVOrani();
            var kdvTutar = tutar * kdvOrani / 100m;
            var toplamTutar = tutar + kdvTutar;

            var faturaTuru = cmbBelgeTuru.SelectedItem as string ?? FaturaSabitleri.FATURA;

            var fatura = new Fatura
            {
                MusteriID = secilenMusteri.MusteriID,
                ProjeID = projeID,
                FaturaTuru = faturaTuru,
                Tarih = dpTarih.SelectedDate.Value,
                AraToplam = tutar,
                KDVOrani = kdvOrani,
                ToplamTutar = toplamTutar,
                Aciklama = txtAciklama.Text.Trim(),
                MusteriAdSoyad = secilenMusteri.AdSoyad,
                ProjeAdi = projeAdi,
                ParaBirimi = _secilenParaBirimi
            };

            // Dosya kaydetme diyaloğu
            var belgeTurMetni = faturaTuru == FaturaSabitleri.TEKLIF ? "Teklif" : "Fatura";
            var tarihDamgasi = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var kaydetDiyalogu = new Microsoft.Win32.SaveFileDialog
            {
                Title = $"{belgeTurMetni} PDF Kaydet",
                FileName = $"{belgeTurMetni}_{secilenMusteri.AdSoyad.Replace(" ", "_")}_{tarihDamgasi}",
                Filter = "PDF Dosyası (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (kaydetDiyalogu.ShowDialog() != true) return;

            try
            {
                // DB'ye kaydet
                if (!FaturaIslemleri.FaturaEkle(fatura))
                {
                    OnayDiyalogu.Hata("Fatura veritabanına kaydedilemedi.", "Kayıt Hatası", this);
                    return;
                }

                // PDF oluştur
                FaturaOlusturucu.PdfOlustur(fatura, secilenMusteri, kaydetDiyalogu.FileName);

                var sembol = ParaBirimleri.SembolGetir(_secilenParaBirimi);
                OnayDiyalogu.Basari(
                    $"{belgeTurMetni} başarıyla oluşturuldu!\n\n" +
                    $"No: {fatura.FaturaNo}\n" +
                    $"Toplam: {sembol}{toplamTutar:N2}\n" +
                    $"Dosya: {kaydetDiyalogu.FileName}",
                    $"{belgeTurMetni} Başarılı", this);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                OnayDiyalogu.Hata($"{belgeTurMetni} oluşturulurken hata:\n{ex.Message}", $"{belgeTurMetni} Hatası", this);
            }
        }

        // ═══════════════ YARDIMCI METOTLAR ═══════════════

        /// <summary>
        /// Seçili KDV oranını döndürür.
        /// </summary>
        private int SecilenKDVOrani()
        {
            if (cmbKDV.SelectedIndex >= 0 && cmbKDV.SelectedIndex < FaturaSabitleri.KDVOranlari.Length)
                return FaturaSabitleri.KDVOranlari[cmbKDV.SelectedIndex];
            return FaturaSabitleri.VARSAYILAN_KDV_ORANI;
        }

        /// <summary>
        /// Toplamı canlı olarak hesaplar ve gösterir.
        /// </summary>
        private void ToplamHesapla()
        {
            if (txtToplamTutar == null || txtKDVDetay == null) return;

            var sembol = ParaBirimleri.SembolGetir(_secilenParaBirimi);

            if (decimal.TryParse(txtTutar?.Text?.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var tutar) && tutar > 0)
            {
                var kdvOrani = SecilenKDVOrani();
                var kdvTutar = tutar * kdvOrani / 100m;
                var toplam = tutar + kdvTutar;

                txtKDVDetay.Text = $"KDV (%{kdvOrani}): {sembol}{kdvTutar:N2}";
                txtToplamTutar.Text = $"{sembol}{toplam:N2}";
            }
            else
            {
                txtKDVDetay.Text = $"KDV: {sembol}0,00";
                txtToplamTutar.Text = $"{sembol}0,00";
            }
        }

        /// <summary>
        /// (Silinmiş Metot) UI üzerinden cmbParaBirimi kullanıldığı için bu metoda gerek kalmamıştır.
        /// </summary>
    }
}
