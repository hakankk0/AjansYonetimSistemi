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
    /// Proje detay penceresi - Notlar, Yapılacaklar, Ödemeler ve Durum Geçmişi.
    /// </summary>
    public partial class ProjeDetayPenceresi : Window
    {
        private readonly Proje _proje;

        /// <summary>
        /// Çalışan ComboBox'ında boş seçim metni.
        /// </summary>
        private const string CalisanBosSecim = "— Atanmamış —";

        public ProjeDetayPenceresi(Proje proje)
        {
            InitializeComponent();
            _proje = proje;

            // Proje bilgilerini doldur
            txtProjeAdi.Text = proje.ProjeAdi;
            txtMusteriAdi.Text = proje.MusteriAdSoyad;
            txtTarihAralik.Text = $"{proje.BaslangicTarihi:dd.MM.yyyy} — {proje.TeslimTarihi:dd.MM.yyyy}";
            txtFiyat.Text = proje.FiyatGosterim;
            txtDurum.Text = proje.Durum;

            // Kur detay bilgisi (dövizli projeler için)
            if (ParaBirimleri.DovizMi(proje.ParaBirimi))
            {
                var sembol = ParaBirimleri.SembolGetir(proje.ParaBirimi);
                txtKurDetay.Text = $"💱 Anlaşma kuru: 1{sembol} = {proje.AnlasmaKuru:N2}₺ | TL karşılığı: ₺{proje.FiyatTL:N2}";

                if (DovizKurServisi.KurlarYuklendi)
                {
                    var fark = DovizKurServisi.KurFarkiYuzdesi(proje.AnlasmaKuru, proje.ParaBirimi);
                    if (Math.Abs(fark) > 1)
                    {
                        var ok = fark >= 0 ? "▲" : "▼";
                        txtKurDetay.Text += $" | Kur farkı: {ok}{Math.Abs(fark):N1}%";
                    }
                }
            }

            // Verileri yükle
            NotlariYukle();
            GorevleriYukle();
            OdemeleriYukle();
            EkipYukle();
            DurumGecmisiniYukle();

            // İlerleme yüzdesi
            sldIlerleme.Value = proje.TamamlanmaYuzdesi;
            pbIlerleme.Value = proje.TamamlanmaYuzdesi;
            txtIlerlemeYuzde.Text = $"%{proje.TamamlanmaYuzdesi}";

            // Kategori badge
            if (!string.IsNullOrWhiteSpace(proje.Kategori))
            {
                txtKategori.Text = proje.Kategori;
                brdKategori.Visibility = Visibility.Visible;
            }
            else
            {
                brdKategori.Visibility = Visibility.Collapsed;
            }

            // Ödeme DatePicker varsayılanı bugün
            dpOdemeTarihi.SelectedDate = DateTime.Now;

            // Görev çalışan ComboBox'ını doldur
            GorevCalisanListesiniDoldur();
        }

        // ═══════════════ NOT İŞLEMLERİ ═══════════════

        private void NotlariYukle()
        {
            lstNotlar.ItemsSource = ProjeNotuIslemleri.ProjeNotlariniGetir(_proje.ProjeID);
        }

        private void NotEkleTiklandi(object sender, RoutedEventArgs e)
        {
            var notMetni = txtYeniNot.Text.Trim();
            if (string.IsNullOrWhiteSpace(notMetni))
            {
                OnayDiyalogu.Uyari("Not metni boş olamaz.", "Uyarı", this);
                return;
            }

            var basarili = ProjeNotuIslemleri.NotEkle(new ProjeNotu
            {
                ProjeID = _proje.ProjeID,
                NotMetni = notMetni,
                OlusturmaTarihi = DateTime.Now
            });

            if (!basarili)
            {
                OnayDiyalogu.Hata("Not eklenirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            txtYeniNot.Text = string.Empty;
            NotlariYukle();
        }

        private void NotSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int notID)
            {
                if (!ProjeNotuIslemleri.NotSil(notID))
                {
                    OnayDiyalogu.Hata("Not silinirken bir hata oluştu.", "Silme Hatası", this);
                    return;
                }
                NotlariYukle();
            }
        }

        // ═══════════════ GÖREV (YAPILACAKLAR) İŞLEMLERİ ═══════════════

        /// <summary>
        /// Görev çalışan ComboBox'ını aktif çalışanlarla doldurur.
        /// </summary>
        private void GorevCalisanListesiniDoldur()
        {
            var calisanlar = CalisanIslemleri.AktifCalisanlariGetir();
            var liste = new List<object> { CalisanBosSecim };
            liste.AddRange(calisanlar);
            cmbGorevCalisan.ItemsSource = liste;
            cmbGorevCalisan.SelectedIndex = 0;
        }

        /// <summary>
        /// Görev listesini ve ilerleme özetini günceller.
        /// </summary>
        private void GorevleriYukle()
        {
            var gorevler = GorevIslemleri.ProjeGorevleriniGetir(_proje.ProjeID);
            lstGorevler.ItemsSource = gorevler;

            // İlerleme özeti
            var toplam = gorevler.Count;
            var tamamlanan = gorevler.Count(g => g.Tamamlandi);

            txtGorevOzet.Text = $"{tamamlanan} / {toplam} görev tamamlandı";

            const int yuzdeTam = 100;
            var yuzde = toplam > 0 ? (int)((double)tamamlanan / toplam * yuzdeTam) : 0;
            pbGorevIlerleme.Value = yuzde;
            txtGorevYuzde.Text = $"%{yuzde}";
        }

        /// <summary>
        /// Yeni görev ekler.
        /// </summary>
        private void GorevEkleTiklandi(object sender, RoutedEventArgs e)
        {
            var baslik = txtGorevBaslik.Text.Trim();
            if (string.IsNullOrWhiteSpace(baslik))
            {
                OnayDiyalogu.Uyari("Görev başlığı boş olamaz.", "Uyarı", this);
                return;
            }

            int? calisanID = null;
            if (cmbGorevCalisan.SelectedItem is Calisan secilenCalisan)
            {
                calisanID = secilenCalisan.CalisanID;
            }

            var aciklama = txtGorevAciklama.Text.Trim();

            var basarili = GorevIslemleri.GorevEkle(new Gorev
            {
                ProjeID = _proje.ProjeID,
                CalisanID = calisanID,
                Baslik = baslik,
                Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama,
                OlusturmaTarihi = DateTime.Now
            });

            if (!basarili)
            {
                OnayDiyalogu.Hata("Görev eklenirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            txtGorevBaslik.Text = string.Empty;
            txtGorevAciklama.Text = string.Empty;
            cmbGorevCalisan.SelectedIndex = 0;
            GorevleriYukle();
        }

        /// <summary>
        /// Görev tamamlanma durumunu değiştirir (CheckBox toggle).
        /// </summary>
        private void GorevDurumuDegisti(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.Tag is int gorevID)
            {
                var yeniDurum = chk.IsChecked == true;
                GorevIslemleri.GorevTamamlaDurumuDegistir(gorevID, yeniDurum);
                GorevleriYukle();
            }
        }

        /// <summary>
        /// Seçili görevi siler.
        /// </summary>
        private void GorevSilTiklandi(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int gorevID)
            {
                if (!GorevIslemleri.GorevSil(gorevID))
                {
                    OnayDiyalogu.Hata("Görev silinirken bir hata oluştu.", "Silme Hatası", this);
                    return;
                }
                GorevleriYukle();
            }
        }

        // ═══════════════ ÖDEME İŞLEMLERİ ═══════════════

        private void OdemeleriYukle()
        {
            var odemeler = OdemeIslemleri.ProjeOdemeleriniGetir(_proje.ProjeID);
            dgOdemeler.ItemsSource = odemeler;

            var odemeToplam = odemeler.Sum(o => o.Tutar);
            var kalan = _proje.Fiyat - odemeToplam;

            var sembol = ParaBirimleri.SembolGetir(_proje.ParaBirimi);
            txtDetayFiyat.Text = $"{sembol}{_proje.Fiyat:N2}";
            txtOdenen.Text = $"{sembol}{odemeToplam:N2}";
            txtKalan.Text = $"{sembol}{(kalan < 0 ? 0 : kalan):N2}";
            
            // Sütun başlığını da doğru para birimiyle güncelle
            if (colOdemeTutar != null)
            {
                colOdemeTutar.Header = $"Tutar ({sembol})";
            }
        }

        private void OdemeEkleTiklandi(object sender, RoutedEventArgs e)
        {
            if (!FiyatYardimci.Parse(txtOdemeTutar.Text, out var girilenTutar) || girilenTutar <= 0)
            {
                OnayDiyalogu.Uyari("Geçerli bir tutar girin.", "Doğrulama Hatası", this);
                return;
            }

            var odemeler = OdemeIslemleri.ProjeOdemeleriniGetir(_proje.ProjeID);
            var odemeToplam = odemeler.Sum(o => o.Tutar);
            var kalan = _proje.Fiyat - odemeToplam;
            var sembol = ParaBirimleri.SembolGetir(_proje.ParaBirimi);
            
            var iadeMi = cmbOdemeTuru.SelectedIndex == 1;
            var islemTutar = iadeMi ? -girilenTutar : girilenTutar;

            if (iadeMi)
            {
                if (girilenTutar > odemeToplam)
                {
                    OnayDiyalogu.Uyari($"İade edilen tutar, bugüne kadar alınan toplam ödemeden ({sembol}{odemeToplam:N2}) daha fazla olamaz.", "Hatalı İade", this);
                    return;
                }
            }
            else
            {
                if (girilenTutar > kalan)
                {
                    OnayDiyalogu.Uyari($"Ödenen tutar kalan borçtan fazla olamaz.\nMaksimum ödenebilir tutar: {sembol}{kalan:N2}", "Limit Aşımı", this);
                    return;
                }
            }

            var odemeTarihi = dpOdemeTarihi.SelectedDate ?? DateTime.Now;
            
            // Eğer iade ise açıklamaya [İADE] etiketi ekleyelim (tercihen).
            var aciklama = txtOdemeAciklama.Text.Trim();
            if (iadeMi && !aciklama.StartsWith("[İADE]", StringComparison.OrdinalIgnoreCase))
            {
                aciklama = $"[İADE] {aciklama}".Trim();
            }

            var basarili = OdemeIslemleri.OdemeEkle(new Odeme
            {
                ProjeID = _proje.ProjeID,
                Tutar = islemTutar,
                OdemeTarihi = odemeTarihi,
                Aciklama = aciklama,
                ParaBirimi = _proje.ParaBirimi,
                OdemeKuru = DovizKurServisi.KurGetir(_proje.ParaBirimi)
            });

            if (!basarili)
            {
                OnayDiyalogu.Hata("Ödeme eklenirken bir hata oluştu.", "Kayıt Hatası", this);
                return;
            }

            txtOdemeTutar.Text = string.Empty;
            txtOdemeAciklama.Text = string.Empty;
            OdemeleriYukle();
        }

        /// <summary>
        /// Fiyat alanında sadece rakam, virgül ve nokta girilmesine izin verir.
        /// </summary>
        private void FiyatSadeceRakam(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            GirisDogrulama.SadeceParaKarakteri(e);
        }

        // ═══════════════ EKİP YÖNETİMİ ═══════════════

        /// <summary>
        /// Projeye atanmış çalışanları yükler ve ComboBox'ı günceller.
        /// </summary>
        private void EkipYukle()
        {
            var atanmisCalisanlar = ProjeCalisanIslemleri.ProjeninCalisanlariniGetir(_proje.ProjeID);
            lstEkip.ItemsSource = atanmisCalisanlar;
            txtEkipOzet.Text = $"{atanmisCalisanlar.Count} çalışan atanmış";

            // ComboBox: sadece henüz atanmamış aktif çalışanları göster
            var tumAktifCalisanlar = CalisanIslemleri.AktifCalisanlariGetir();
            var atanmisIDler = new System.Collections.Generic.HashSet<int>(atanmisCalisanlar.Select(c => c.CalisanID));
            var atanabilirler = tumAktifCalisanlar.Where(c => !atanmisIDler.Contains(c.CalisanID)).ToList();

            cmbEkipCalisan.ItemsSource = atanabilirler;
            if (atanabilirler.Count > 0)
                cmbEkipCalisan.SelectedIndex = 0;
        }

        /// <summary>
        /// Seçili çalışanı projeye dahil eder.
        /// </summary>
        private void EkibeCalisanEkleTiklandi(object sender, RoutedEventArgs e)
        {
            if (cmbEkipCalisan.SelectedItem is not Calisan secilenCalisan)
            {
                OnayDiyalogu.Uyari("Lütfen ekibe eklemek istediğiniz çalışanı seçin.", "Uyarı", this);
                return;
            }

            if (!ProjeCalisanIslemleri.CalisanAta(_proje.ProjeID, secilenCalisan.CalisanID))
            {
                OnayDiyalogu.Hata("Çalışan atanırken bir hata oluştu.", "Atama Hatası", this);
                return;
            }

            // Ekibe eklenen çalışan için genel hareket logu at
            AktiviteIslemleri.AktiviteEkle(
                metin: $"{secilenCalisan.AdSoyad}, '{_proje.ProjeAdi}' projesi ekibine dahil edildi.",
                ikon: "\uE716"
            );

            EkipYukle();
        }

        /// <summary>
        /// Çalışanı projeden çıkarır.
        /// </summary>
        private void EkiptenCalisanCikarTiklandi(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int calisanID)
            {
                if (!ProjeCalisanIslemleri.CalisanCikar(_proje.ProjeID, calisanID))
                {
                    OnayDiyalogu.Hata("Çalışan çıkarılırken bir hata oluştu.", "İşlem Hatası", this);
                    return;
                }

                // Çıkarılan çalışan için log
                AktiviteIslemleri.AktiviteEkle(
                    metin: $"Çalışan, '{_proje.ProjeAdi}' projesi ekibinden çıkarıldı.",
                    ikon: "\uE7E8"
                );

                EkipYukle();
            }
        }

        // ═══════════════ DURUM GEÇMİŞİ ═══════════════

        private void DurumGecmisiniYukle()
        {
            lstDurumGecmisi.ItemsSource = DurumGecmisiIslemleri.ProjeGecmisiniGetir(_proje.ProjeID);
        }

        // ═══════════════ İLERLEME ═══════════════

        private void IlerlemeDegisti(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            var yuzde = (int)sldIlerleme.Value;
            pbIlerleme.Value = yuzde;
            txtIlerlemeYuzde.Text = $"%{yuzde}";

            // İlerleme %100'e ulaştığında durumu otomatik Tamamlandı yap
            const int tamIlerlemeYuzdesi = 100;
            if (yuzde == tamIlerlemeYuzdesi && _proje.Durum != ProjeDurumlari.TAMAMLANDI)
            {
                _proje.Durum = ProjeDurumlari.TAMAMLANDI;
                txtDurum.Text = ProjeDurumlari.TAMAMLANDI;
            }

            // DB'ye kaydet
            _proje.TamamlanmaYuzdesi = yuzde;
            ProjeIslemleri.ProjeGuncelle(_proje);
        }
    }
}

