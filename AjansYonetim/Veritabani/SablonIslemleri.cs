using System;
using System.Collections.Generic;
using AjansYonetim.Modeller;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Proje şablonları tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// Kullanıcı şablonları ekleyebilir, düzenleyebilir ve silebilir.
    /// </summary>
    public static class SablonIslemleri
    {
        /// <summary>
        /// Yeni şablon ekler.
        /// </summary>
        public static bool SablonEkle(ProjeSablonu sablon)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO ProjeSablonlari (Ad, VarsayilanSureGun, TahminiFiyat)
                    VALUES (@ad, @sure, @fiyat);";

                komut.Parameters.AddWithValue("@ad", sablon.Ad);
                komut.Parameters.AddWithValue("@sure", sablon.VarsayilanSureGun);
                komut.Parameters.AddWithValue("@fiyat", (double)sablon.TahminiFiyat);

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
        /// Mevcut şablonu günceller.
        /// </summary>
        public static bool SablonGuncelle(ProjeSablonu sablon)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    UPDATE ProjeSablonlari
                    SET Ad = @ad,
                        VarsayilanSureGun = @sure,
                        TahminiFiyat = @fiyat
                    WHERE SablonID = @id;";

                komut.Parameters.AddWithValue("@id", sablon.SablonID);
                komut.Parameters.AddWithValue("@ad", sablon.Ad);
                komut.Parameters.AddWithValue("@sure", sablon.VarsayilanSureGun);
                komut.Parameters.AddWithValue("@fiyat", (double)sablon.TahminiFiyat);

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
        /// Şablonu siler.
        /// </summary>
        public static bool SablonSil(int sablonID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM ProjeSablonlari WHERE SablonID = @id;";
                komut.Parameters.AddWithValue("@id", sablonID);

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
        /// Tüm şablonları getirir.
        /// </summary>
        public static List<ProjeSablonu> TumSablonlariGetir()
        {
            var sablonlar = new List<ProjeSablonu>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT SablonID, Ad, VarsayilanSureGun, TahminiFiyat
                    FROM ProjeSablonlari
                    ORDER BY Ad;";

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    sablonlar.Add(new ProjeSablonu
                    {
                        SablonID = okuyucu.GetInt32(0),
                        Ad = okuyucu.GetString(1),
                        VarsayilanSureGun = okuyucu.GetInt32(2),
                        TahminiFiyat = Convert.ToDecimal(okuyucu.GetDouble(3))
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return sablonlar;
        }

        /// <summary>
        /// Varsayılan şablonları ekler (ilk kullanımda).
        /// Tablo zaten veri varsa ekleme yapmaz.
        /// </summary>
        public static void VarsayilanSablonlariEkle()
        {
            try
            {
                var mevcut = TumSablonlariGetir();
                if (mevcut.Count > 0) return;

                var varsayilanlar = new[]
                {
                    new ProjeSablonu { Ad = "Logo Tasarımı", VarsayilanSureGun = 7, TahminiFiyat = 5000 },
                    new ProjeSablonu { Ad = "Web Sitesi Tasarımı", VarsayilanSureGun = 30, TahminiFiyat = 25000 },
                    new ProjeSablonu { Ad = "Sosyal Medya Paketi", VarsayilanSureGun = 14, TahminiFiyat = 8000 },
                    new ProjeSablonu { Ad = "Kurumsal Kimlik", VarsayilanSureGun = 21, TahminiFiyat = 15000 },
                    new ProjeSablonu { Ad = "Broşür / Katalog", VarsayilanSureGun = 10, TahminiFiyat = 3000 },
                    new ProjeSablonu { Ad = "Ambalaj Tasarımı", VarsayilanSureGun = 14, TahminiFiyat = 7000 }
                };

                foreach (var sablon in varsayilanlar)
                {
                    SablonEkle(sablon);
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }
    }
}
