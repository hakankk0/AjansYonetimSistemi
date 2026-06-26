using System;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;
using AjansYonetim.Yardimcilar;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// QuestPDF ile profesyonel fatura/teklif PDF oluşturucu.
    /// </summary>
    public static class FaturaOlusturucu
    {
        /// <summary>
        /// Ana tema rengi (mor).
        /// </summary>
        private const string TEMA_RENK = "#7C3AED";

        /// <summary>
        /// Açık arka plan rengi.
        /// </summary>
        private const string ACIK_ARKAPLAN = "#F5F3FF";

        /// <summary>
        /// Fatura/Teklif PDF'i oluşturur ve dosyaya kaydeder.
        /// </summary>
        public static void PdfOlustur(Fatura fatura, Musteri musteri, string dosyaYolu)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Ajans bilgilerini yükle
            var ajansAdi = LisansYoneticisi.MevcutLisans?.AjansAdi ?? "Ajans";
            var ajansTelefon = AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_AJANS_TELEFON);
            var ajansEposta = AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_AJANS_EPOSTA);
            var ajansAdres = AyarIslemleri.AyarGetir(AyarIslemleri.ANAHTAR_AJANS_ADRES);

            var baslik = fatura.FaturaTuru == FaturaSabitleri.TEKLIF ? "FİYAT TEKLİFİ" : "FATURA";
            var paraSembol = ParaBirimleri.SembolGetir(fatura.ParaBirimi);

            Document.Create(container =>
            {
                container.Page(sayfa =>
                {
                    sayfa.Size(PageSizes.A4);
                    sayfa.Margin(40);
                    sayfa.DefaultTextStyle(x => x.FontSize(10));

                    // ═══════════════ HEADER ═══════════════
                    sayfa.Header().Column(col =>
                    {
                        // Üst bölüm: Ajans (sol) | Fatura bilgisi (sağ)
                        col.Item().Row(row =>
                        {
                            // Sol — Ajans bilgileri
                            row.RelativeItem().Column(ajansCol =>
                            {
                                ajansCol.Item().Text(ajansAdi)
                                    .FontSize(22).Bold().FontColor(TEMA_RENK);

                                if (!string.IsNullOrWhiteSpace(ajansAdres))
                                    ajansCol.Item().Text(ajansAdres).FontSize(9).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(ajansTelefon))
                                    ajansCol.Item().Text($"📞 {ajansTelefon}").FontSize(9).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(ajansEposta))
                                    ajansCol.Item().Text($"📧 {ajansEposta}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            // Sağ — Fatura detayları
                            row.ConstantItem(200).Column(faturaCol =>
                            {
                                faturaCol.Item().AlignRight()
                                    .Text(baslik).FontSize(24).Bold().FontColor(TEMA_RENK);
                                faturaCol.Item().AlignRight()
                                    .Text($"No: {fatura.FaturaNo}").FontSize(11).Bold();
                                faturaCol.Item().AlignRight()
                                    .Text($"Tarih: {fatura.Tarih:dd.MM.yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        // Ayırıcı çizgi
                        col.Item().PaddingVertical(12).LineHorizontal(2).LineColor(TEMA_RENK);

                        // Müşteri bilgileri
                        col.Item().Background(ACIK_ARKAPLAN).Padding(12).Column(musteriCol =>
                        {
                            musteriCol.Item().Text("MÜŞTERİ BİLGİLERİ").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            musteriCol.Item().PaddingTop(4).Text(musteri.AdSoyad).FontSize(12).Bold();

                            if (!string.IsNullOrWhiteSpace(musteri.SirketAdi))
                                musteriCol.Item().Text(musteri.SirketAdi).FontSize(10).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(musteri.VergiNo))
                                musteriCol.Item().Text($"Vergi No: {musteri.VergiNo}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(musteri.Adres))
                                musteriCol.Item().Text(musteri.Adres).FontSize(9).FontColor(Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(musteri.Telefon))
                                musteriCol.Item().Text($"📞 {musteri.Telefon}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        col.Item().PaddingBottom(16);
                    });

                    // ═══════════════ CONTENT ═══════════════
                    sayfa.Content().Column(icerik =>
                    {
                        // Kalem tablosu
                        icerik.Item().Table(tablo =>
                        {
                            tablo.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);   // #
                                columns.RelativeColumn(5);    // Açıklama
                                columns.ConstantColumn(120);  // Tutar
                            });

                            // Başlık satırı
                            tablo.Cell().Background(TEMA_RENK).Padding(8)
                                .Text("#").FontColor(Colors.White).Bold();
                            tablo.Cell().Background(TEMA_RENK).Padding(8)
                                .Text("Açıklama").FontColor(Colors.White).Bold();
                            tablo.Cell().Background(TEMA_RENK).Padding(8)
                                .AlignRight().Text($"Tutar ({paraSembol})").FontColor(Colors.White).Bold();

                            // Kalem satırı
                            var kalemAciklama = !string.IsNullOrWhiteSpace(fatura.ProjeAdi)
                                ? fatura.ProjeAdi
                                : (!string.IsNullOrWhiteSpace(fatura.Aciklama) ? fatura.Aciklama : "Hizmet bedeli");

                            tablo.Cell().Background(ACIK_ARKAPLAN).Padding(8).Text("1");
                            tablo.Cell().Background(ACIK_ARKAPLAN).Padding(8).Text(kalemAciklama);
                            tablo.Cell().Background(ACIK_ARKAPLAN).Padding(8)
                                .AlignRight().Text($"{fatura.AraToplam:N2}");
                        });

                        // Boşluk
                        icerik.Item().PaddingTop(16);

                        // Toplam bölümü — sağa hizalı
                        icerik.Item().AlignRight().Width(250).Column(toplamCol =>
                        {
                            // Ara Toplam
                            toplamCol.Item().Row(row =>
                            {
                                row.RelativeItem().Padding(6).Text("Ara Toplam:").FontSize(10);
                                row.ConstantItem(100).Padding(6).AlignRight()
                                    .Text($"{paraSembol}{fatura.AraToplam:N2}").FontSize(10);
                            });

                            // KDV
                            var kdvTutar = fatura.ToplamTutar - fatura.AraToplam;
                            toplamCol.Item().Row(row =>
                            {
                                row.RelativeItem().Padding(6)
                                    .Text($"KDV (%{fatura.KDVOrani}):").FontSize(10).FontColor(Colors.Grey.Darken1);
                                row.ConstantItem(100).Padding(6).AlignRight()
                                    .Text($"{paraSembol}{kdvTutar:N2}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });

                            // Ayırıcı
                            toplamCol.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            // GENEL TOPLAM
                            toplamCol.Item().Background(TEMA_RENK).Row(row =>
                            {
                                row.RelativeItem().Padding(8)
                                    .Text("GENEL TOPLAM:").FontSize(13).Bold().FontColor(Colors.White);
                                row.ConstantItem(120).Padding(8).AlignRight()
                                    .Text($"{paraSembol}{fatura.ToplamTutar:N2}").FontSize(13).Bold().FontColor(Colors.White);
                            });
                        });

                        // Açıklama (varsa)
                        if (!string.IsNullOrWhiteSpace(fatura.Aciklama) && !string.IsNullOrWhiteSpace(fatura.ProjeAdi))
                        {
                            icerik.Item().PaddingTop(24).Column(notCol =>
                            {
                                notCol.Item().Text("NOTLAR").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                                notCol.Item().PaddingTop(4).Text(fatura.Aciklama).FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });

                    // ═══════════════ FOOTER ═══════════════
                    sayfa.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        footer.Item().PaddingTop(8).AlignCenter()
                            .Text(FaturaSabitleri.PDF_FOOTER_METNI)
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(dosyaYolu);
        }
    }
}
