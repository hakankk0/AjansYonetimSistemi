using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AjansYonetim.Modeller;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// CSV dosyalarıyla veri dışa/içe aktarma işlemleri.
    /// </summary>
    public static class CsvIslemleri
    {
        /// <summary>
        /// CSV ayırıcı karakter.
        /// </summary>
        private const char Ayirici = ';';

        /// <summary>
        /// Projeleri CSV dosyasına aktarır.
        /// </summary>
        public static void ProjeleriDisaAktar(List<Proje> projeler, string dosyaYolu)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ProjeID;MusteriAdSoyad;ProjeAdi;BaslangicTarihi;TeslimTarihi;Fiyat;Durum;TamamlanmaYuzdesi;Kategori");

            foreach (var p in projeler)
            {
                sb.AppendLine(string.Join(Ayirici.ToString(),
                    p.ProjeID,
                    CsvKacis(p.MusteriAdSoyad),
                    CsvKacis(p.ProjeAdi),
                    p.BaslangicTarihi.ToString("dd.MM.yyyy"),
                    p.TeslimTarihi.ToString("dd.MM.yyyy"),
                    p.Fiyat.ToString("F2"),
                    CsvKacis(p.Durum),
                    p.TamamlanmaYuzdesi,
                    CsvKacis(p.Kategori)));
            }

            File.WriteAllText(dosyaYolu, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Müşterileri CSV dosyasına aktarır.
        /// </summary>
        public static void MusterileriDisaAktar(List<Musteri> musteriler, string dosyaYolu)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MusteriID;AdSoyad;Telefon;Eposta;SirketAdi;VergiNo;Adres;Notlar");

            foreach (var m in musteriler)
            {
                sb.AppendLine(string.Join(Ayirici.ToString(),
                    m.MusteriID,
                    CsvKacis(m.AdSoyad),
                    CsvKacis(m.Telefon),
                    CsvKacis(m.Eposta),
                    CsvKacis(m.SirketAdi),
                    CsvKacis(m.VergiNo),
                    CsvKacis(m.Adres),
                    CsvKacis(m.Notlar)));
            }

            File.WriteAllText(dosyaYolu, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// CSV'den müşteri verileri içe aktarır. İlk satır başlık olarak kabul edilir.
        /// </summary>
        public static List<Musteri> MusterileriIceAktar(string dosyaYolu)
        {
            var musteriler = new List<Musteri>();
            var satirlar = File.ReadAllLines(dosyaYolu, Encoding.UTF8);

            // İlk satır başlık
            for (int i = 1; i < satirlar.Length; i++)
            {
                var satir = satirlar[i].Trim();
                if (string.IsNullOrEmpty(satir)) continue;

                var alanlar = CsvSatirAyir(satir);
                if (alanlar.Length < 2) continue;

                var musteri = new Musteri
                {
                    AdSoyad = alanlar.Length > 1 ? alanlar[1] : string.Empty,
                    Telefon = alanlar.Length > 2 ? alanlar[2] : string.Empty,
                    Eposta = alanlar.Length > 3 ? alanlar[3] : string.Empty,
                    SirketAdi = alanlar.Length > 4 ? alanlar[4] : string.Empty,
                    VergiNo = alanlar.Length > 5 ? alanlar[5] : string.Empty,
                    Adres = alanlar.Length > 6 ? alanlar[6] : string.Empty,
                    Notlar = alanlar.Length > 7 ? alanlar[7] : string.Empty
                };

                musteriler.Add(musteri);
            }

            return musteriler;
        }

        /// <summary>
        /// CSV için özel karakterleri kaçırır.
        /// </summary>
        private static string CsvKacis(string deger)
        {
            if (string.IsNullOrEmpty(deger)) return string.Empty;

            // CSV Injection koruması: formül enjeksiyonunu engelle
            if (deger[0] == '=' || deger[0] == '+' || deger[0] == '-' || deger[0] == '@')
            {
                deger = "'" + deger;
            }

            if (deger.Contains(Ayirici) || deger.Contains('"') || deger.Contains('\n'))
            {
                return $"\"{deger.Replace("\"", "\"\"")}\"";
            }

            return deger;
        }

        /// <summary>
        /// CSV satırını alanlara ayırır (tırnak içindeki ayırıcıları dikkate alarak).
        /// </summary>
        private static string[] CsvSatirAyir(string satir)
        {
            var alanlar = new List<string>();
            var mevcutAlan = new StringBuilder();
            var tirnakIcinde = false;

            foreach (var karakter in satir)
            {
                if (karakter == '"')
                {
                    tirnakIcinde = !tirnakIcinde;
                }
                else if (karakter == Ayirici && !tirnakIcinde)
                {
                    alanlar.Add(mevcutAlan.ToString());
                    mevcutAlan.Clear();
                }
                else
                {
                    mevcutAlan.Append(karakter);
                }
            }

            alanlar.Add(mevcutAlan.ToString());
            return alanlar.ToArray();
        }
    }
}
