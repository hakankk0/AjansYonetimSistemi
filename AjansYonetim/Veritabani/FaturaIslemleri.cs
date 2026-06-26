using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Fatura/Teklif veritabanı işlemleri.
    /// </summary>
    public static class FaturaIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni fatura/teklif ekler ve otomatik fatura numarası oluşturur.
        /// </summary>
        public static bool FaturaEkle(Fatura fatura)
        {
            using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

            // Otomatik fatura numarası oluştur
            fatura.FaturaNo = SonrakiFaturaNoGetir(baglanti);

            var komut = baglanti.CreateCommand();
            komut.CommandText = @"
                INSERT INTO Faturalar (MusteriID, ProjeID, FaturaNo, FaturaTuru, Tarih, AraToplam, KDVOrani, ToplamTutar, Aciklama, ParaBirimi)
                VALUES (@musteriID, @projeID, @faturaNo, @faturaTuru, @tarih, @araToplam, @kdvOrani, @toplamTutar, @aciklama, @paraBirimi)";

            komut.Parameters.AddWithValue("@musteriID", fatura.MusteriID);
            komut.Parameters.AddWithValue("@projeID", fatura.ProjeID.HasValue ? (object)fatura.ProjeID.Value : DBNull.Value);
            komut.Parameters.AddWithValue("@faturaNo", fatura.FaturaNo);
            komut.Parameters.AddWithValue("@faturaTuru", fatura.FaturaTuru);
            komut.Parameters.AddWithValue("@tarih", fatura.Tarih.ToString(TarihFormati));
            komut.Parameters.AddWithValue("@araToplam", (double)fatura.AraToplam);
            komut.Parameters.AddWithValue("@kdvOrani", fatura.KDVOrani);
            komut.Parameters.AddWithValue("@toplamTutar", (double)fatura.ToplamTutar);
            komut.Parameters.AddWithValue("@aciklama", string.IsNullOrWhiteSpace(fatura.Aciklama) ? DBNull.Value : fatura.Aciklama);
            komut.Parameters.AddWithValue("@paraBirimi", fatura.ParaBirimi);

            return komut.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Tüm faturaları müşteri ve proje bilgileriyle birlikte getirir.
        /// </summary>
        public static List<Fatura> TumFaturalariGetir()
        {
            var faturalar = new List<Fatura>();

            using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

            var komut = baglanti.CreateCommand();
            komut.CommandText = @"
                SELECT f.FaturaID, f.MusteriID, f.ProjeID, f.FaturaNo, f.FaturaTuru,
                       f.Tarih, f.AraToplam, f.KDVOrani, f.ToplamTutar, f.Aciklama,
                       m.AdSoyad, COALESCE(p.ProjeAdi, ''),
                       COALESCE(f.ParaBirimi, 'TL')
                FROM Faturalar f
                INNER JOIN Musteriler m ON f.MusteriID = m.MusteriID
                LEFT JOIN Projeler p ON f.ProjeID = p.ProjeID
                ORDER BY f.Tarih DESC";

            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                faturalar.Add(OkuyucudanFaturaOlustur(okuyucu));
            }

            return faturalar;
        }

        /// <summary>
        /// Belirli bir müşterinin faturalarını getirir.
        /// </summary>
        public static List<Fatura> MusterininFaturalariniGetir(int musteriID)
        {
            var faturalar = new List<Fatura>();

            using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

            var komut = baglanti.CreateCommand();
            komut.CommandText = @"
                SELECT f.FaturaID, f.MusteriID, f.ProjeID, f.FaturaNo, f.FaturaTuru,
                       f.Tarih, f.AraToplam, f.KDVOrani, f.ToplamTutar, f.Aciklama,
                       m.AdSoyad, COALESCE(p.ProjeAdi, ''),
                       COALESCE(f.ParaBirimi, 'TL')
                FROM Faturalar f
                INNER JOIN Musteriler m ON f.MusteriID = m.MusteriID
                LEFT JOIN Projeler p ON f.ProjeID = p.ProjeID
                WHERE f.MusteriID = @musteriID
                ORDER BY f.Tarih DESC";

            komut.Parameters.AddWithValue("@musteriID", musteriID);

            using var okuyucu = komut.ExecuteReader();
            while (okuyucu.Read())
            {
                faturalar.Add(OkuyucudanFaturaOlustur(okuyucu));
            }

            return faturalar;
        }

        /// <summary>
        /// Fatura siler.
        /// </summary>
        public static bool FaturaSil(int faturaID)
        {
            using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

            var komut = baglanti.CreateCommand();
            komut.CommandText = "DELETE FROM Faturalar WHERE FaturaID = @faturaID";
            komut.Parameters.AddWithValue("@faturaID", faturaID);

            return komut.ExecuteNonQuery() > 0;
        }

        /// <summary>
        /// Sıradaki fatura numarasını oluşturur (FT-2026-0001 formatı).
        /// </summary>
        private static string SonrakiFaturaNoGetir(Microsoft.Data.Sqlite.SqliteConnection baglanti)
        {
            var yil = DateTime.Now.Year;
            var onek = $"{FaturaSabitleri.FATURA_NO_ONEKI}-{yil}-";

            var komut = baglanti.CreateCommand();
            komut.CommandText = @"
                SELECT FaturaNo FROM Faturalar
                WHERE FaturaNo LIKE @onek
                ORDER BY FaturaNo DESC
                LIMIT 1";
            komut.Parameters.AddWithValue("@onek", onek + "%");

            var sonNo = komut.ExecuteScalar() as string;

            int sira = 1;
            if (!string.IsNullOrEmpty(sonNo))
            {
                var parcalar = sonNo.Split('-');
                if (parcalar.Length >= 3 && int.TryParse(parcalar[2], out var mevcutSira))
                {
                    sira = mevcutSira + 1;
                }
            }

            return string.Format(FaturaSabitleri.FATURA_NO_FORMAT, FaturaSabitleri.FATURA_NO_ONEKI, yil, sira);
        }

        /// <summary>
        /// SqliteDataReader'dan Fatura nesnesi oluşturur.
        /// </summary>
        private static Fatura OkuyucudanFaturaOlustur(Microsoft.Data.Sqlite.SqliteDataReader okuyucu)
        {
            return new Fatura
            {
                FaturaID = okuyucu.GetInt32(0),
                MusteriID = okuyucu.GetInt32(1),
                ProjeID = okuyucu.IsDBNull(2) ? null : okuyucu.GetInt32(2),
                FaturaNo = okuyucu.GetString(3),
                FaturaTuru = okuyucu.GetString(4),
                Tarih = DateTime.TryParseExact(okuyucu.GetString(5), TarihFormati,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.Now,
                AraToplam = (decimal)okuyucu.GetDouble(6),
                KDVOrani = okuyucu.GetInt32(7),
                ToplamTutar = (decimal)okuyucu.GetDouble(8),
                Aciklama = okuyucu.IsDBNull(9) ? string.Empty : okuyucu.GetString(9),
                MusteriAdSoyad = okuyucu.GetString(10),
                ProjeAdi = okuyucu.GetString(11),
                ParaBirimi = okuyucu.FieldCount > 12 && !okuyucu.IsDBNull(12) ? okuyucu.GetString(12) : ParaBirimleri.VARSAYILAN
            };
        }
    }
}
