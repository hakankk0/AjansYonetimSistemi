using System;
using System.Collections.Generic;
using System.Linq;
using AjansYonetim.Modeller;
using AjansYonetim.Veritabani;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Excel ve PDF rapor oluşturma yardımcı sınıfı.
    /// </summary>
    public static class RaporOlusturucu
    {
        /// <summary>
        /// Rapor başlık arka plan rengi (mor).
        /// </summary>
        private const string BASLIK_ARKAPLAN_RENK = "#7C3AED";

        /// <summary>
        /// Excel rapor toplamı için sayı formatı.
        /// </summary>
        private const string PARA_FORMAT = "#,##0.00";

        /// <summary>
        /// Proje ve müşteri listesini Excel dosyasına aktarır.
        /// </summary>
        public static void ExcelRaporOlustur(List<Proje> projeler, List<Musteri> musteriler, List<Odeme> odemeler, string dosyaYolu)
        {
            using var calismaKitabi = new XLWorkbook();

            // ── Projeler Sayfası ──
            ExcelProjeSayfasiOlustur(calismaKitabi, projeler);

            // ── Müşteriler Sayfası ──
            ExcelMusteriSayfasiOlustur(calismaKitabi, musteriler);

            // ── Ödemeler Sayfası ──
            ExcelOdemeSayfasiOlustur(calismaKitabi, odemeler);

            calismaKitabi.SaveAs(dosyaYolu);
        }

        /// <summary>
        /// Proje ve müşteri listesini PDF dosyasına aktarır.
        /// </summary>
        public static void PdfRaporOlustur(List<Proje> projeler, List<Musteri> musteriler, List<Odeme> odemeler, string dosyaYolu)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                // ── Projeler Sayfası ──
                PdfProjeSayfasiOlustur(container, projeler);

                // ── Müşteriler Sayfası ──
                PdfMusteriSayfasiOlustur(container, musteriler);

                // ── Ödemeler Sayfası ──
                PdfOdemeSayfasiOlustur(container, odemeler);

            }).GeneratePdf(dosyaYolu);
        }

        // ═══════════════ EXCEL YARDIMCI METOTLAR ═══════════════

        /// <summary>
        /// Excel başlık satırı stilini uygular.
        /// </summary>
        private static void ExcelBaslikStiliUygula(IXLWorksheet sayfa, string[] basliklar)
        {
            for (int i = 0; i < basliklar.Length; i++)
            {
                var hucre = sayfa.Cell(1, i + 1);
                hucre.Value = basliklar[i];
                hucre.Style.Font.Bold = true;
                hucre.Style.Fill.BackgroundColor = XLColor.FromHtml(BASLIK_ARKAPLAN_RENK);
                hucre.Style.Font.FontColor = XLColor.White;
            }
        }

        /// <summary>
        /// Excel'de Projeler sayfasını oluşturur.
        /// </summary>
        private static void ExcelProjeSayfasiOlustur(XLWorkbook calismaKitabi, List<Proje> projeler)
        {
            var sayfa = calismaKitabi.Worksheets.Add("Projeler");

            var basliklar = new[] { "Proje Adı", "Müşteri", "Başlangıç", "Teslim", "Fiyat (₺)", "Durum", "Kategori" };
            ExcelBaslikStiliUygula(sayfa, basliklar);

            for (int satir = 0; satir < projeler.Count; satir++)
            {
                var proje = projeler[satir];
                sayfa.Cell(satir + 2, 1).Value = proje.ProjeAdi;
                sayfa.Cell(satir + 2, 2).Value = proje.MusteriAdSoyad;
                sayfa.Cell(satir + 2, 3).Value = proje.BaslangicTarihi.ToString("dd.MM.yyyy");
                sayfa.Cell(satir + 2, 4).Value = proje.TeslimTarihi.ToString("dd.MM.yyyy");
                sayfa.Cell(satir + 2, 5).Value = (double)proje.Fiyat;
                sayfa.Cell(satir + 2, 6).Value = proje.Durum;
                sayfa.Cell(satir + 2, 7).Value = proje.Kategori;
            }

            sayfa.Columns().AdjustToContents();

            // Toplam satırı
            var toplamSatir = projeler.Count + 3;
            sayfa.Cell(toplamSatir, 5).Value = "TOPLAM:";
            sayfa.Cell(toplamSatir, 5).Style.Font.Bold = true;
            sayfa.Cell(toplamSatir, 6).Value = (double)projeler.Sum(p => p.Fiyat);
            sayfa.Cell(toplamSatir, 6).Style.Font.Bold = true;
            sayfa.Cell(toplamSatir, 6).Style.NumberFormat.Format = PARA_FORMAT;
        }

        /// <summary>
        /// Excel'de Müşteriler sayfasını oluşturur.
        /// </summary>
        private static void ExcelMusteriSayfasiOlustur(XLWorkbook calismaKitabi, List<Musteri> musteriler)
        {
            var sayfa = calismaKitabi.Worksheets.Add("Müşteriler");

            var basliklar = new[] { "Ad Soyad", "Telefon", "E-posta", "Şirket Adı", "Vergi No", "Adres", "Notlar" };
            ExcelBaslikStiliUygula(sayfa, basliklar);

            for (int satir = 0; satir < musteriler.Count; satir++)
            {
                var musteri = musteriler[satir];
                sayfa.Cell(satir + 2, 1).Value = musteri.AdSoyad;
                sayfa.Cell(satir + 2, 2).Value = musteri.Telefon;
                sayfa.Cell(satir + 2, 3).Value = musteri.Eposta;
                sayfa.Cell(satir + 2, 4).Value = musteri.SirketAdi;
                sayfa.Cell(satir + 2, 5).Value = musteri.VergiNo;
                sayfa.Cell(satir + 2, 6).Value = musteri.Adres;
                sayfa.Cell(satir + 2, 7).Value = musteri.Notlar;
            }

            sayfa.Columns().AdjustToContents();

            // Toplam müşteri sayısı
            var toplamSatir = musteriler.Count + 3;
            sayfa.Cell(toplamSatir, 1).Value = "TOPLAM MÜŞTERİ:";
            sayfa.Cell(toplamSatir, 1).Style.Font.Bold = true;
            sayfa.Cell(toplamSatir, 2).Value = musteriler.Count;
            sayfa.Cell(toplamSatir, 2).Style.Font.Bold = true;
        }

        /// <summary>
        /// Excel'de Ödemeler sayfasını oluşturur.
        /// </summary>
        private static void ExcelOdemeSayfasiOlustur(XLWorkbook calismaKitabi, List<Odeme> odemeler)
        {
            var sayfa = calismaKitabi.Worksheets.Add("Ödemeler");

            var basliklar = new[] { "Müşteri", "Proje", "Tutar (₺)", "Ödeme Tarihi", "Açıklama" };
            ExcelBaslikStiliUygula(sayfa, basliklar);

            for (int satir = 0; satir < odemeler.Count; satir++)
            {
                var odeme = odemeler[satir];
                sayfa.Cell(satir + 2, 1).Value = odeme.MusteriAdSoyad;
                sayfa.Cell(satir + 2, 2).Value = odeme.ProjeAdi;
                sayfa.Cell(satir + 2, 3).Value = (double)odeme.Tutar;
                sayfa.Cell(satir + 2, 3).Style.NumberFormat.Format = PARA_FORMAT;
                sayfa.Cell(satir + 2, 4).Value = odeme.OdemeTarihi.ToString("dd.MM.yyyy");
                sayfa.Cell(satir + 2, 5).Value = odeme.Aciklama;
            }

            sayfa.Columns().AdjustToContents();

            // Toplam ödeme tutarı
            var toplamSatir = odemeler.Count + 3;
            sayfa.Cell(toplamSatir, 2).Value = "TOPLAM ÖDEME:";
            sayfa.Cell(toplamSatir, 2).Style.Font.Bold = true;
            sayfa.Cell(toplamSatir, 3).Value = (double)odemeler.Sum(o => o.Tutar);
            sayfa.Cell(toplamSatir, 3).Style.Font.Bold = true;
            sayfa.Cell(toplamSatir, 3).Style.NumberFormat.Format = PARA_FORMAT;
        }

        // ═══════════════ PDF YARDIMCI METOTLAR ═══════════════

        /// <summary>
        /// PDF'de Projeler sayfasını oluşturur.
        /// </summary>
        private static void PdfProjeSayfasiOlustur(IDocumentContainer container, List<Proje> projeler)
        {
            container.Page(sayfa =>
            {
                sayfa.Size(PageSizes.A4.Landscape());
                sayfa.Margin(30);
                sayfa.DefaultTextStyle(x => x.FontSize(10));

                sayfa.Header().Column(col =>
                {
                    col.Item().Text("Proje Raporu").FontSize(20).Bold().FontColor(Colors.Purple.Medium);
                    col.Item().Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                sayfa.Content().Table(tablo =>
                {
                    tablo.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);    // Proje Adı
                        columns.RelativeColumn(2);    // Müşteri
                        columns.RelativeColumn(1.5f); // Başlangıç
                        columns.RelativeColumn(1.5f); // Teslim
                        columns.RelativeColumn(1.5f); // Fiyat
                        columns.RelativeColumn(1.5f); // Durum
                        columns.RelativeColumn(1.5f); // Kategori
                    });

                    var basliklar = new[] { "Proje Adı", "Müşteri", "Başlangıç", "Teslim", "Fiyat (₺)", "Durum", "Kategori" };
                    foreach (var baslik in basliklar)
                    {
                        tablo.Cell().Background(Colors.Purple.Medium).Padding(6)
                            .Text(baslik).FontColor(Colors.White).Bold();
                    }

                    for (int i = 0; i < projeler.Count; i++)
                    {
                        var proje = projeler[i];
                        var arkaPlan = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        tablo.Cell().Background(arkaPlan).Padding(5).Text(proje.ProjeAdi);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(proje.MusteriAdSoyad);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(proje.BaslangicTarihi.ToString("dd.MM.yyyy"));
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(proje.TeslimTarihi.ToString("dd.MM.yyyy"));
                        tablo.Cell().Background(arkaPlan).Padding(5).Text($"{proje.Fiyat:N2}");
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(proje.Durum);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(proje.Kategori);
                    }
                });

                sayfa.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }

        /// <summary>
        /// PDF'de Müşteriler sayfasını oluşturur.
        /// </summary>
        private static void PdfMusteriSayfasiOlustur(IDocumentContainer container, List<Musteri> musteriler)
        {
            container.Page(sayfa =>
            {
                sayfa.Size(PageSizes.A4.Landscape());
                sayfa.Margin(30);
                sayfa.DefaultTextStyle(x => x.FontSize(10));

                sayfa.Header().Column(col =>
                {
                    col.Item().Text("Müşteri Listesi").FontSize(20).Bold().FontColor(Colors.Purple.Medium);
                    col.Item().Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}  |  Toplam: {musteriler.Count} müşteri")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                sayfa.Content().Table(tablo =>
                {
                    tablo.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.5f); // Ad Soyad
                        columns.RelativeColumn(1.5f); // Telefon
                        columns.RelativeColumn(2);    // E-posta
                        columns.RelativeColumn(2);    // Şirket Adı
                        columns.RelativeColumn(1.5f); // Vergi No
                        columns.RelativeColumn(3);    // Adres
                    });

                    var basliklar = new[] { "Ad Soyad", "Telefon", "E-posta", "Şirket Adı", "Vergi No", "Adres" };
                    foreach (var baslik in basliklar)
                    {
                        tablo.Cell().Background(Colors.Purple.Medium).Padding(6)
                            .Text(baslik).FontColor(Colors.White).Bold();
                    }

                    for (int i = 0; i < musteriler.Count; i++)
                    {
                        var musteri = musteriler[i];
                        var arkaPlan = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        tablo.Cell().Background(arkaPlan).Padding(5).Text(musteri.AdSoyad);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(musteri.Telefon);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(musteri.Eposta);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(musteri.SirketAdi);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(musteri.VergiNo);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(musteri.Adres);
                    }
                });

                sayfa.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }

        /// <summary>
        /// PDF'de Ödemeler sayfasını oluşturur.
        /// </summary>
        private static void PdfOdemeSayfasiOlustur(IDocumentContainer container, List<Odeme> odemeler)
        {
            container.Page(sayfa =>
            {
                sayfa.Size(PageSizes.A4.Landscape());
                sayfa.Margin(30);
                sayfa.DefaultTextStyle(x => x.FontSize(10));

                sayfa.Header().Column(col =>
                {
                    col.Item().Text("Ödeme Detayları").FontSize(20).Bold().FontColor(Colors.Purple.Medium);
                    col.Item().Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}  |  Toplam Ödeme: ₺{odemeler.Sum(o => o.Tutar):N2}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                sayfa.Content().Table(tablo =>
                {
                    tablo.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.5f); // Müşteri
                        columns.RelativeColumn(2.5f); // Proje
                        columns.RelativeColumn(1.5f); // Tutar
                        columns.RelativeColumn(1.5f); // Ödeme Tarihi
                        columns.RelativeColumn(3);    // Açıklama
                    });

                    var basliklar = new[] { "Müşteri", "Proje", "Tutar (₺)", "Ödeme Tarihi", "Açıklama" };
                    foreach (var baslik in basliklar)
                    {
                        tablo.Cell().Background(Colors.Purple.Medium).Padding(6)
                            .Text(baslik).FontColor(Colors.White).Bold();
                    }

                    for (int i = 0; i < odemeler.Count; i++)
                    {
                        var odeme = odemeler[i];
                        var arkaPlan = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        tablo.Cell().Background(arkaPlan).Padding(5).Text(odeme.MusteriAdSoyad);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(odeme.ProjeAdi);
                        tablo.Cell().Background(arkaPlan).Padding(5).Text($"{odeme.Tutar:N2}");
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(odeme.OdemeTarihi.ToString("dd.MM.yyyy"));
                        tablo.Cell().Background(arkaPlan).Padding(5).Text(odeme.Aciklama);
                    }
                });

                sayfa.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }
    }
}

