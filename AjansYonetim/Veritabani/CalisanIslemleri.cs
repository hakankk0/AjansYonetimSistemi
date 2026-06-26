using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Çalışan tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// Tüm sorgularda SQL Parameters kullanılır (SQL Injection önlemi).
    /// </summary>
    public static class CalisanIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni çalışan ekler.
        /// </summary>
        public static bool CalisanEkle(Calisan calisan)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO Calisanlar (AdSoyad, Telefon, Eposta, Departman, Pozisyon, CalisanTuru, IseBaslamaTarihi, Durum, Notlar)
                    VALUES (@adSoyad, @telefon, @eposta, @departman, @pozisyon, @calisanTuru, @iseBaslamaTarihi, @durum, @notlar);";

                komut.Parameters.AddWithValue("@adSoyad", calisan.AdSoyad);
                komut.Parameters.AddWithValue("@telefon", calisan.Telefon);
                komut.Parameters.AddWithValue("@eposta", calisan.Eposta);
                komut.Parameters.AddWithValue("@departman", calisan.Departman);
                komut.Parameters.AddWithValue("@pozisyon", (object?)calisan.Pozisyon ?? DBNull.Value);
                komut.Parameters.AddWithValue("@calisanTuru", calisan.CalisanTuru);
                komut.Parameters.AddWithValue("@iseBaslamaTarihi", calisan.IseBaslamaTarihi.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@durum", calisan.Durum);
                komut.Parameters.AddWithValue("@notlar", (object?)calisan.Notlar ?? DBNull.Value);

                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
                return true;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return false;
            }
        }

        /// <summary>
        /// Mevcut çalışanı günceller.
        /// </summary>
        public static bool CalisanGuncelle(Calisan calisan)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    UPDATE Calisanlar
                    SET AdSoyad          = @adSoyad,
                        Telefon          = @telefon,
                        Eposta           = @eposta,
                        Departman        = @departman,
                        Pozisyon         = @pozisyon,
                        CalisanTuru      = @calisanTuru,
                        IseBaslamaTarihi = @iseBaslamaTarihi,
                        Durum            = @durum,
                        Notlar           = @notlar
                    WHERE CalisanID = @calisanID;";

                komut.Parameters.AddWithValue("@calisanID", calisan.CalisanID);
                komut.Parameters.AddWithValue("@adSoyad", calisan.AdSoyad);
                komut.Parameters.AddWithValue("@telefon", calisan.Telefon);
                komut.Parameters.AddWithValue("@eposta", calisan.Eposta);
                komut.Parameters.AddWithValue("@departman", calisan.Departman);
                komut.Parameters.AddWithValue("@pozisyon", (object?)calisan.Pozisyon ?? DBNull.Value);
                komut.Parameters.AddWithValue("@calisanTuru", calisan.CalisanTuru);
                komut.Parameters.AddWithValue("@iseBaslamaTarihi", calisan.IseBaslamaTarihi.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@durum", calisan.Durum);
                komut.Parameters.AddWithValue("@notlar", (object?)calisan.Notlar ?? DBNull.Value);

                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
                return true;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return false;
            }
        }

        /// <summary>
        /// Çalışanı siler. İlişkili proje atamaları da silinir (CASCADE).
        /// </summary>
        public static bool CalisanSil(int calisanID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM Calisanlar WHERE CalisanID = @calisanID;";
                komut.Parameters.AddWithValue("@calisanID", calisanID);

                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
                return true;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return false;
            }
        }

        /// <summary>
        /// Tüm çalışanları getirir.
        /// </summary>
        public static List<Calisan> TumCalisanlariGetir()
        {
            var calisanlar = new List<Calisan>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT CalisanID, AdSoyad, Telefon, Eposta, Departman, Pozisyon,
                           CalisanTuru, IseBaslamaTarihi, Durum, Notlar
                    FROM Calisanlar ORDER BY AdSoyad;";

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    calisanlar.Add(OkuyucudanCalisanOlustur(okuyucu));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return calisanlar;
        }

        /// <summary>
        /// Tek bir çalışanı ID ile getirir.
        /// </summary>
        public static Calisan? CalisanGetir(int calisanID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT CalisanID, AdSoyad, Telefon, Eposta, Departman, Pozisyon,
                           CalisanTuru, IseBaslamaTarihi, Durum, Notlar
                    FROM Calisanlar
                    WHERE CalisanID = @calisanID;";
                komut.Parameters.AddWithValue("@calisanID", calisanID);

                using var okuyucu = komut.ExecuteReader();
                if (okuyucu.Read())
                {
                    return OkuyucudanCalisanOlustur(okuyucu);
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return null;
        }

        /// <summary>
        /// Aktif çalışanları getirir.
        /// </summary>
        public static List<Calisan> AktifCalisanlariGetir()
        {
            var calisanlar = new List<Calisan>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT CalisanID, AdSoyad, Telefon, Eposta, Departman, Pozisyon,
                           CalisanTuru, IseBaslamaTarihi, Durum, Notlar
                    FROM Calisanlar
                    WHERE Durum = @durum
                    ORDER BY AdSoyad;";
                komut.Parameters.AddWithValue("@durum", CalisanDurumlari.AKTIF);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    calisanlar.Add(OkuyucudanCalisanOlustur(okuyucu));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return calisanlar;
        }

        /// <summary>
        /// Çalışan adı, departman veya türe göre arama yapar.
        /// </summary>
        public static List<Calisan> CalisanAra(string aramaMetni)
        {
            var calisanlar = new List<Calisan>();

            if (string.IsNullOrWhiteSpace(aramaMetni))
                return TumCalisanlariGetir();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT CalisanID, AdSoyad, Telefon, Eposta, Departman, Pozisyon,
                           CalisanTuru, IseBaslamaTarihi, Durum, Notlar
                    FROM Calisanlar
                    WHERE AdSoyad LIKE @arama OR Departman LIKE @arama OR CalisanTuru LIKE @arama OR Pozisyon LIKE @arama
                    ORDER BY AdSoyad;";

                komut.Parameters.AddWithValue("@arama", $"%{aramaMetni}%");

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    calisanlar.Add(OkuyucudanCalisanOlustur(okuyucu));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return calisanlar;
        }

        /// <summary>
        /// Çalışan performans metriklerini hesaplar.
        /// ProjeCalisanlari ara tablosundan proje istatistiklerini getirir.
        /// </summary>
        public static Calisan PerformansGetir(Calisan calisan)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                // Tek sorguyla tüm performans metriklerini hesapla
                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT
                        COUNT(*) AS ToplamProje,
                        SUM(CASE WHEN p.Durum = @tamamlandi THEN 1 ELSE 0 END) AS Tamamlanan,
                        SUM(CASE WHEN p.Durum != @tamamlandi THEN 1 ELSE 0 END) AS Aktif,
                        SUM(CASE WHEN p.Durum != @tamamlandi AND p.TeslimTarihi < @simdi THEN 1 ELSE 0 END) AS Geciken
                    FROM ProjeCalisanlari pc
                    INNER JOIN Projeler p ON pc.ProjeID = p.ProjeID
                    WHERE pc.CalisanID = @calisanID;";

                komut.Parameters.AddWithValue("@calisanID", calisan.CalisanID);
                komut.Parameters.AddWithValue("@tamamlandi", ProjeDurumlari.TAMAMLANDI);
                komut.Parameters.AddWithValue("@simdi", DateTime.Now.ToString(TarihFormati));

                using var okuyucu = komut.ExecuteReader();
                if (okuyucu.Read())
                {
                    calisan.ToplamProjeSayisi = okuyucu.GetInt32(0);
                    calisan.TamamlananProjeSayisi = okuyucu.GetInt32(1);
                    calisan.AktifProjeSayisi = okuyucu.GetInt32(2);
                    calisan.GecikenProjeSayisi = okuyucu.GetInt32(3);
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return calisan;
        }

        /// <summary>
        /// Tüm çalışanları performans metrikleriyle birlikte getirir.
        /// </summary>
        public static List<Calisan> TumCalisanlariPerformansIleGetir()
        {
            var calisanlar = TumCalisanlariGetir();
            foreach (var calisan in calisanlar)
            {
                PerformansGetir(calisan);
            }
            return calisanlar;
        }

        /// <summary>
        /// SqliteDataReader'dan Calisan nesnesi oluşturur (kod tekrarını önler).
        /// </summary>
        private static Calisan OkuyucudanCalisanOlustur(SqliteDataReader okuyucu)
        {
            DateTime.TryParseExact(
                okuyucu.IsDBNull(7) ? string.Empty : okuyucu.GetString(7),
                TarihFormati, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var iseBaslama);

            return new Calisan
            {
                CalisanID = okuyucu.GetInt32(0),
                AdSoyad = okuyucu.GetString(1),
                Telefon = okuyucu.IsDBNull(2) ? string.Empty : okuyucu.GetString(2),
                Eposta = okuyucu.IsDBNull(3) ? string.Empty : okuyucu.GetString(3),
                Departman = okuyucu.IsDBNull(4) ? string.Empty : okuyucu.GetString(4),
                Pozisyon = okuyucu.IsDBNull(5) ? string.Empty : okuyucu.GetString(5),
                CalisanTuru = okuyucu.IsDBNull(6) ? string.Empty : okuyucu.GetString(6),
                IseBaslamaTarihi = iseBaslama,
                Durum = okuyucu.IsDBNull(8) ? CalisanDurumlari.AKTIF : okuyucu.GetString(8),
                Notlar = okuyucu.IsDBNull(9) ? string.Empty : okuyucu.GetString(9)
            };
        }
    }
}
