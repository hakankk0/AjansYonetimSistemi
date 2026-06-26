using System;
using System.Collections.Generic;
using AjansYonetim.Modeller;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Müşteri tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// Tüm sorgularda SQL Parameters kullanılır (SQL Injection önlemi).
    /// </summary>
    public static class MusteriIslemleri
    {
        /// <summary>
        /// Yeni müşteri ekler.
        /// </summary>
        public static bool MusteriEkle(Musteri musteri)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO Musteriler (AdSoyad, Telefon, Eposta, SirketAdi, VergiNo, Adres, Notlar, MusteriTuru)
                    VALUES (@adSoyad, @telefon, @eposta, @sirketAdi, @vergiNo, @adres, @notlar, @musteriTuru);";

                komut.Parameters.AddWithValue("@adSoyad", musteri.AdSoyad);
                komut.Parameters.AddWithValue("@telefon", musteri.Telefon);
                komut.Parameters.AddWithValue("@eposta", musteri.Eposta);
                komut.Parameters.AddWithValue("@sirketAdi", (object?)musteri.SirketAdi ?? DBNull.Value);
                komut.Parameters.AddWithValue("@vergiNo", (object?)musteri.VergiNo ?? DBNull.Value);
                komut.Parameters.AddWithValue("@adres", (object?)musteri.Adres ?? DBNull.Value);
                komut.Parameters.AddWithValue("@notlar", (object?)musteri.Notlar ?? DBNull.Value);
                komut.Parameters.AddWithValue("@musteriTuru", (object?)musteri.MusteriTuru ?? DBNull.Value);

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
        /// Mevcut müşteriyi günceller.
        /// </summary>
        public static bool MusteriGuncelle(Musteri musteri)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    UPDATE Musteriler
                    SET AdSoyad    = @adSoyad,
                        Telefon    = @telefon,
                        Eposta     = @eposta,
                        SirketAdi  = @sirketAdi,
                        VergiNo    = @vergiNo,
                        Adres      = @adres,
                        Notlar     = @notlar,
                        MusteriTuru = @musteriTuru
                    WHERE MusteriID = @musteriID;";

                komut.Parameters.AddWithValue("@musteriID", musteri.MusteriID);
                komut.Parameters.AddWithValue("@adSoyad", musteri.AdSoyad);
                komut.Parameters.AddWithValue("@telefon", musteri.Telefon);
                komut.Parameters.AddWithValue("@eposta", musteri.Eposta);
                komut.Parameters.AddWithValue("@sirketAdi", (object?)musteri.SirketAdi ?? DBNull.Value);
                komut.Parameters.AddWithValue("@vergiNo", (object?)musteri.VergiNo ?? DBNull.Value);
                komut.Parameters.AddWithValue("@adres", (object?)musteri.Adres ?? DBNull.Value);
                komut.Parameters.AddWithValue("@notlar", (object?)musteri.Notlar ?? DBNull.Value);
                komut.Parameters.AddWithValue("@musteriTuru", (object?)musteri.MusteriTuru ?? DBNull.Value);

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
        /// Müşteriyi siler. ON DELETE CASCADE ile ilişkili projeler de silinir.
        /// </summary>
        public static bool MusteriSil(int musteriID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM Musteriler WHERE MusteriID = @musteriID;";
                komut.Parameters.AddWithValue("@musteriID", musteriID);

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
        /// Tüm müşterileri getirir.
        /// </summary>
        public static List<Musteri> TumMusterileriGetir()
        {
            var musteriler = new List<Musteri>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT MusteriID, AdSoyad, Telefon, Eposta, SirketAdi, VergiNo, Adres, Notlar, COALESCE(MusteriTuru, 'Yurt İçi')
                    FROM Musteriler ORDER BY AdSoyad;";

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    musteriler.Add(OkuyucudanMusteriOlustur(okuyucu));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return musteriler;
        }

        /// <summary>
        /// Müşteri adı veya şirket adına göre arama yapar.
        /// </summary>
        public static List<Musteri> MusteriAra(string aramaMetni)
        {
            var musteriler = new List<Musteri>();

            if (string.IsNullOrWhiteSpace(aramaMetni))
                return TumMusterileriGetir();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT MusteriID, AdSoyad, Telefon, Eposta, SirketAdi, VergiNo, Adres, Notlar, COALESCE(MusteriTuru, 'Yurt İçi')
                    FROM Musteriler
                    WHERE AdSoyad LIKE @arama OR SirketAdi LIKE @arama OR Telefon LIKE @arama
                    ORDER BY AdSoyad;";

                komut.Parameters.AddWithValue("@arama", $"%{aramaMetni}%");

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    musteriler.Add(OkuyucudanMusteriOlustur(okuyucu));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return musteriler;
        }

        /// <summary>
        /// SqliteDataReader'dan Musteri nesnesi oluşturur (kod tekrarını önler).
        /// </summary>
        private static Musteri OkuyucudanMusteriOlustur(SqliteDataReader okuyucu)
        {
            return new Musteri
            {
                MusteriID = okuyucu.GetInt32(0),
                AdSoyad = okuyucu.GetString(1),
                Telefon = okuyucu.IsDBNull(2) ? string.Empty : okuyucu.GetString(2),
                Eposta = okuyucu.IsDBNull(3) ? string.Empty : okuyucu.GetString(3),
                SirketAdi = okuyucu.IsDBNull(4) ? string.Empty : okuyucu.GetString(4),
                VergiNo = okuyucu.IsDBNull(5) ? string.Empty : okuyucu.GetString(5),
                Adres = okuyucu.IsDBNull(6) ? string.Empty : okuyucu.GetString(6),
                Notlar = okuyucu.IsDBNull(7) ? string.Empty : okuyucu.GetString(7),
                MusteriTuru = okuyucu.FieldCount > 8 && !okuyucu.IsDBNull(8) ? okuyucu.GetString(8) : string.Empty
            };
        }
    }
}
