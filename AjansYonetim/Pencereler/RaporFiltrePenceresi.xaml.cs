using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler
{
    /// <summary>
    /// Rapor filtre penceresi — tarih, durum ve müşteri bazlı filtreleme.
    /// </summary>
    public partial class RaporFiltrePenceresi : Window
    {
        /// <summary>
        /// Filtrelenmiş proje listesi sonucu.
        /// </summary>
        public List<Proje> FiltrelenmisProjecteler { get; private set; } = new();

        /// <summary>
        /// Tüm filtre seçenekleri sabit metni.
        /// </summary>
        private const string TUMU = "Tümü";

        public RaporFiltrePenceresi()
        {
            InitializeComponent();

            // Tarih filtreleri varsayılan olarak boş — tüm projeler gelir
            dpFiltreBaslangic.SelectedDate = null;
            dpFiltreBitis.SelectedDate = null;

            // Durum filtresi
            var durumlar = new List<string> { TUMU };
            durumlar.AddRange(ProjeDurumlari.TumDurumlar);
            cmbFiltreDurum.ItemsSource = durumlar;
            cmbFiltreDurum.SelectedIndex = 0;

            // Müşteri filtresi
            var musteriIsimleri = new List<string> { TUMU };
            musteriIsimleri.AddRange(
                MusteriIslemleri.TumMusterileriGetir().Select(m => m.AdSoyad));
            cmbFiltreMusteri.ItemsSource = musteriIsimleri;
            cmbFiltreMusteri.SelectedIndex = 0;
        }

        private void OlusturTiklandi(object sender, RoutedEventArgs e)
        {
            var projeler = ProjeIslemleri.TumProjeleriGetir();

            // Tarih filtresi
            if (dpFiltreBaslangic.SelectedDate.HasValue)
            {
                projeler = projeler.Where(p => p.BaslangicTarihi >= dpFiltreBaslangic.SelectedDate.Value).ToList();
            }

            if (dpFiltreBitis.SelectedDate.HasValue)
            {
                projeler = projeler.Where(p => p.TeslimTarihi <= dpFiltreBitis.SelectedDate.Value.AddDays(1)).ToList();
            }

            // Durum filtresi
            if (cmbFiltreDurum.SelectedItem is string durum && durum != TUMU)
            {
                projeler = projeler.Where(p => p.Durum == durum).ToList();
            }

            // Müşteri filtresi
            if (cmbFiltreMusteri.SelectedItem is string musteriAdi && musteriAdi != TUMU)
            {
                projeler = projeler.Where(p => p.MusteriAdSoyad == musteriAdi).ToList();
            }

            FiltrelenmisProjecteler = projeler;
            DialogResult = true;
            Close();
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
    }
}
