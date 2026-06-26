using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Proje tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// Tüm sorgularda SQL Parameters kullanılır (SQL Injection önlemi).
    /// </summary>
    public static class ProjeIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni proje ekler.
        /// </summary>
        public static bool ProjeEkle(Proje proje)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO Projeler (MusteriID, ProjeAdi, BaslangicTarihi, TeslimTarihi, Fiyat, Durum, TamamlanmaYuzdesi, Kategori, ParaBirimi, AnlasmaKuru)
                    VALUES (@musteriID, @projeAdi, @baslangicTarihi, @teslimTarihi, @fiyat, @durum, @tamamlanmaYuzdesi, @kategori, @paraBirimi, @anlasmaKuru);";

                komut.Parameters.AddWithValue("@musteriID", proje.MusteriID);
                komut.Parameters.AddWithValue("@projeAdi", proje.ProjeAdi);
                komut.Parameters.AddWithValue("@baslangicTarihi", proje.BaslangicTarihi.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@teslimTarihi", proje.TeslimTarihi.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@fiyat", (double)proje.Fiyat);
                komut.Parameters.AddWithValue("@durum", proje.Durum);
                komut.Parameters.AddWithValue("@tamamlanmaYuzdesi", proje.TamamlanmaYuzdesi);
                komut.Parameters.AddWithValue("@kategori", proje.Kategori);
                komut.Parameters.AddWithValue("@paraBirimi", proje.ParaBirimi);
                komut.Parameters.AddWithValue("@anlasmaKuru", (double)proje.AnlasmaKuru);

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
        /// Mevcut projeyi günceller. Durum değiştiyse geçmişe kaydeder.
        /// </summary>
        public static bool ProjeGuncelle(Proje proje, string? eskiDurum = null)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    UPDATE Projeler
                    SET MusteriID       = @musteriID,
                        ProjeAdi        = @projeAdi,
                        BaslangicTarihi = @baslangicTarihi,
                        TeslimTarihi    = @teslimTarihi,
                        Fiyat           = @fiyat,
                        Durum           = @durum,
                        TamamlanmaYuzdesi = @tamamlanmaYuzdesi,
                        Kategori        = @kategori,
                        ParaBirimi      = @paraBirimi,
                        AnlasmaKuru     = @anlasmaKuru
                    WHERE ProjeID = @projeID;";

                komut.Parameters.AddWithValue("@projeID", proje.ProjeID);
                komut.Parameters.AddWithValue("@musteriID", proje.MusteriID);
                komut.Parameters.AddWithValue("@projeAdi", proje.ProjeAdi);
                komut.Parameters.AddWithValue("@baslangicTarihi", proje.BaslangicTarihi.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@teslimTarihi", proje.TeslimTarihi.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@fiyat", (double)proje.Fiyat);
                komut.Parameters.AddWithValue("@durum", proje.Durum);
                komut.Parameters.AddWithValue("@tamamlanmaYuzdesi", proje.TamamlanmaYuzdesi);
                komut.Parameters.AddWithValue("@kategori", proje.Kategori);
                komut.Parameters.AddWithValue("@paraBirimi", proje.ParaBirimi);
                komut.Parameters.AddWithValue("@anlasmaKuru", (double)proje.AnlasmaKuru);

                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();

                // Durum değişiklik geçmişi kaydı
                if (!string.IsNullOrEmpty(eskiDurum) && eskiDurum != proje.Durum)
                {
                    DurumGecmisiIslemleri.GecmisEkle(new DurumGecmisi
                    {
                        ProjeID = proje.ProjeID,
                        EskiDurum = eskiDurum,
                        YeniDurum = proje.Durum,
                        DegisimTarihi = DateTime.Now
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return false;
            }
        }

        /// <summary>
        /// Projeyi siler.
        /// </summary>
        public static bool ProjeSil(int projeID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM Projeler WHERE ProjeID = @projeID;";
                komut.Parameters.AddWithValue("@projeID", projeID);

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
        /// Aktif projeleri getirir (Tamamlandı olmayan).
        /// Müşteri adı ile birlikte JOIN yapılır.
        /// </summary>
        public static List<Proje> AktifProjeleriGetir()
        {
            return ProjeleriSorgula(
                @"SELECT p.ProjeID, p.MusteriID, p.ProjeAdi, p.BaslangicTarihi,
                         p.TeslimTarihi, p.Fiyat, p.Durum, m.AdSoyad,
                         COALESCE(p.TamamlanmaYuzdesi, 0), COALESCE(p.Kategori, ''),
                         COALESCE((SELECT GROUP_CONCAT(c.AdSoyad, ', ')
                                   FROM ProjeCalisanlari pc
                                   INNER JOIN Calisanlar c ON pc.CalisanID = c.CalisanID
                                   WHERE pc.ProjeID = p.ProjeID), ''),
                         COALESCE(p.ParaBirimi, 'TL'), COALESCE(p.AnlasmaKuru, 1.0)
                  FROM Projeler p
                  INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                  WHERE p.Durum != @durum
                  ORDER BY p.TeslimTarihi ASC;",
                ("@durum", ProjeDurumlari.TAMAMLANDI));
        }

        /// <summary>
        /// Tüm projeleri getirir (filtre yok).
        /// </summary>
        public static List<Proje> TumProjeleriGetir()
        {
            return ProjeleriSorgula(
                @"SELECT p.ProjeID, p.MusteriID, p.ProjeAdi, p.BaslangicTarihi,
                         p.TeslimTarihi, p.Fiyat, p.Durum, m.AdSoyad,
                         COALESCE(p.TamamlanmaYuzdesi, 0), COALESCE(p.Kategori, ''),
                         COALESCE((SELECT GROUP_CONCAT(c.AdSoyad, ', ')
                                   FROM ProjeCalisanlari pc
                                   INNER JOIN Calisanlar c ON pc.CalisanID = c.CalisanID
                                   WHERE pc.ProjeID = p.ProjeID), ''),
                         COALESCE(p.ParaBirimi, 'TL'), COALESCE(p.AnlasmaKuru, 1.0)
                  FROM Projeler p
                  INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                  ORDER BY p.TeslimTarihi ASC;");
        }

        /// <summary>
        /// Belirli bir müşterinin projelerini getirir (SQL düzeyinde filtreleme).
        /// </summary>
        public static List<Proje> MusterininProjeleriniGetir(int musteriID)
        {
            return ProjeleriSorgula(
                @"SELECT p.ProjeID, p.MusteriID, p.ProjeAdi, p.BaslangicTarihi,
                         p.TeslimTarihi, p.Fiyat, p.Durum, m.AdSoyad,
                         COALESCE(p.TamamlanmaYuzdesi, 0), COALESCE(p.Kategori, ''),
                         COALESCE((SELECT GROUP_CONCAT(c.AdSoyad, ', ')
                                   FROM ProjeCalisanlari pc
                                   INNER JOIN Calisanlar c ON pc.CalisanID = c.CalisanID
                                   WHERE pc.ProjeID = p.ProjeID), ''),
                         COALESCE(p.ParaBirimi, 'TL'), COALESCE(p.AnlasmaKuru, 1.0)
                  FROM Projeler p
                  INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                  WHERE p.MusteriID = @musteriID
                  ORDER BY p.TeslimTarihi ASC;",
                ("@musteriID", musteriID));
        }

        /// <summary>
        /// Belirli bir duruma göre projeleri filtreler.
        /// </summary>
        public static List<Proje> DurumaGoreFiltrele(string durum)
        {
            // "Tümü" seçilirse aktif projeleri göster
            if (durum == ProjeDurumlari.FILTRE_TUMU)
            {
                return AktifProjeleriGetir();
            }

            return ProjeleriSorgula(
                @"SELECT p.ProjeID, p.MusteriID, p.ProjeAdi, p.BaslangicTarihi,
                         p.TeslimTarihi, p.Fiyat, p.Durum, m.AdSoyad,
                         COALESCE(p.TamamlanmaYuzdesi, 0), COALESCE(p.Kategori, ''),
                         COALESCE((SELECT GROUP_CONCAT(c.AdSoyad, ', ')
                                   FROM ProjeCalisanlari pc
                                   INNER JOIN Calisanlar c ON pc.CalisanID = c.CalisanID
                                   WHERE pc.ProjeID = p.ProjeID), ''),
                         COALESCE(p.ParaBirimi, 'TL'), COALESCE(p.AnlasmaKuru, 1.0)
                  FROM Projeler p
                  INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                  WHERE p.Durum = @durum
                  ORDER BY p.TeslimTarihi ASC;",
                ("@durum", durum));
        }

        /// <summary>
        /// Proje adı, müşteri adı veya tarih aralığına göre arama yapar.
        /// </summary>
        public static List<Proje> ProjeAra(string? kelime, DateTime? baslangic, DateTime? bitis)
        {
            var projeler = new List<Proje>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                var sql = @"
                    SELECT p.ProjeID, p.MusteriID, p.ProjeAdi, p.BaslangicTarihi,
                           p.TeslimTarihi, p.Fiyat, p.Durum, m.AdSoyad,
                           COALESCE(p.TamamlanmaYuzdesi, 0), COALESCE(p.Kategori, ''),
                           COALESCE((SELECT GROUP_CONCAT(c.AdSoyad, ', ')
                                     FROM ProjeCalisanlari pc
                                     INNER JOIN Calisanlar c ON pc.CalisanID = c.CalisanID
                                     WHERE pc.ProjeID = p.ProjeID), ''),
                           COALESCE(p.ParaBirimi, 'TL'), COALESCE(p.AnlasmaKuru, 1.0)
                    FROM Projeler p
                    INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                    WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(kelime))
                {
                    sql += " AND (p.ProjeAdi LIKE @kelime OR m.AdSoyad LIKE @kelime)";
                    komut.Parameters.AddWithValue("@kelime", $"%{kelime}%");
                }

                if (baslangic.HasValue)
                {
                    sql += " AND p.BaslangicTarihi >= @baslangic";
                    komut.Parameters.AddWithValue("@baslangic", baslangic.Value.ToString(TarihFormati));
                }

                if (bitis.HasValue)
                {
                    sql += " AND p.TeslimTarihi <= @bitis";
                    komut.Parameters.AddWithValue("@bitis", bitis.Value.ToString(TarihFormati));
                }

                sql += " ORDER BY p.TeslimTarihi ASC;";
                komut.CommandText = sql;

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    projeler.Add(OkuyucudanProjeOlustur(okuyucu));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return projeler;
        }

        /// <summary>
        /// Tek bir projeyi ID ile getirir.
        /// </summary>
        public static Proje? ProjeGetir(int projeID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT p.ProjeID, p.MusteriID, p.ProjeAdi, p.BaslangicTarihi,
                           p.TeslimTarihi, p.Fiyat, p.Durum, m.AdSoyad,
                           COALESCE(p.TamamlanmaYuzdesi, 0), COALESCE(p.Kategori, ''),
                           COALESCE((SELECT GROUP_CONCAT(c.AdSoyad, ', ')
                                     FROM ProjeCalisanlari pc
                                     INNER JOIN Calisanlar c ON pc.CalisanID = c.CalisanID
                                     WHERE pc.ProjeID = p.ProjeID), ''),
                           COALESCE(p.ParaBirimi, 'TL'), COALESCE(p.AnlasmaKuru, 1.0)
                    FROM Projeler p
                    INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                    WHERE p.ProjeID = @projeID;";
                komut.Parameters.AddWithValue("@projeID", projeID);

                using var okuyucu = komut.ExecuteReader();
                if (okuyucu.Read())
                {
                    return OkuyucudanProjeOlustur(okuyucu);
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return null;
        }

        /// <summary>
        /// Ortak sorgu çalıştırma metodu.
        /// Tekrar eden kodu önler.
        /// </summary>
        private static List<Proje> ProjeleriSorgula(string sql, params (string ad, object deger)[] parametreler)
        {
            var projeler = new List<Proje>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = sql;

                foreach (var (ad, deger) in parametreler)
                {
                    komut.Parameters.AddWithValue(ad, deger);
                }

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    projeler.Add(OkuyucudanProjeOlustur(okuyucu));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return projeler;
        }

        /// <summary>
        /// SqliteDataReader'dan Proje nesnesi oluşturur (kod tekrarını önler).
        /// </summary>
        private static Proje OkuyucudanProjeOlustur(SqliteDataReader okuyucu)
        {
            DateTime.TryParseExact(okuyucu.GetString(3), TarihFormati, CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var baslangic);
            DateTime.TryParseExact(okuyucu.GetString(4), TarihFormati, CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var teslim);

            return new Proje
            {
                ProjeID = okuyucu.GetInt32(0),
                MusteriID = okuyucu.GetInt32(1),
                ProjeAdi = okuyucu.GetString(2),
                BaslangicTarihi = baslangic,
                TeslimTarihi = teslim,
                Fiyat = Convert.ToDecimal(okuyucu.GetDouble(5)),
                Durum = okuyucu.GetString(6),
                MusteriAdSoyad = okuyucu.GetString(7),
                TamamlanmaYuzdesi = okuyucu.GetInt32(8),
                Kategori = okuyucu.FieldCount > 9 && !okuyucu.IsDBNull(9) ? okuyucu.GetString(9) : string.Empty,
                EkipMetni = okuyucu.FieldCount > 10 && !okuyucu.IsDBNull(10) ? okuyucu.GetString(10) : string.Empty,
                ParaBirimi = okuyucu.FieldCount > 11 && !okuyucu.IsDBNull(11) ? okuyucu.GetString(11) : Sabitler.ParaBirimleri.VARSAYILAN,
                AnlasmaKuru = okuyucu.FieldCount > 12 && !okuyucu.IsDBNull(12) ? Convert.ToDecimal(okuyucu.GetDouble(12)) : 1.0m
            };
        }
    }
}
