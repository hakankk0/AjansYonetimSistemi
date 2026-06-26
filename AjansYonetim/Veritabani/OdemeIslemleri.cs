using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Ödemeler tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// </summary>
    public static class OdemeIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni ödeme ekler.
        /// </summary>
        public static bool OdemeEkle(Odeme odeme)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO Odemeler (ProjeID, Tutar, OdemeTarihi, Aciklama, ParaBirimi, OdemeKuru)
                    VALUES (@projeID, @tutar, @odemeTarihi, @aciklama, @paraBirimi, @odemeKuru);";

                komut.Parameters.AddWithValue("@projeID", odeme.ProjeID);
                komut.Parameters.AddWithValue("@tutar", (double)odeme.Tutar);
                komut.Parameters.AddWithValue("@odemeTarihi", odeme.OdemeTarihi.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@aciklama", (object?)odeme.Aciklama ?? DBNull.Value);
                komut.Parameters.AddWithValue("@paraBirimi", odeme.ParaBirimi);
                komut.Parameters.AddWithValue("@odemeKuru", (double)odeme.OdemeKuru);

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
        /// Belirli bir projenin ödemelerini getirir.
        /// </summary>
        public static List<Odeme> ProjeOdemeleriniGetir(int projeID)
        {
            var odemeler = new List<Odeme>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT OdemeID, ProjeID, Tutar, OdemeTarihi, Aciklama,
                           COALESCE(ParaBirimi, 'TL'), COALESCE(OdemeKuru, 1.0)
                    FROM Odemeler
                    WHERE ProjeID = @projeID
                    ORDER BY OdemeTarihi DESC;";

                komut.Parameters.AddWithValue("@projeID", projeID);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    odemeler.Add(new Odeme
                    {
                        OdemeID = okuyucu.GetInt32(0),
                        ProjeID = okuyucu.GetInt32(1),
                        Tutar = Convert.ToDecimal(okuyucu.GetDouble(2)),
                        OdemeTarihi = DateTime.ParseExact(okuyucu.GetString(3), TarihFormati, CultureInfo.InvariantCulture),
                        Aciklama = okuyucu.IsDBNull(4) ? string.Empty : okuyucu.GetString(4),
                        ParaBirimi = !okuyucu.IsDBNull(5) ? okuyucu.GetString(5) : ParaBirimleri.VARSAYILAN,
                        OdemeKuru = !okuyucu.IsDBNull(6) ? Convert.ToDecimal(okuyucu.GetDouble(6)) : 1.0m
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return odemeler;
        }

        /// <summary>
        /// Tüm ödemeleri proje ve müşteri bilgisiyle birlikte getirir (rapor için).
        /// </summary>
        public static List<Odeme> TumOdemeleriGetir()
        {
            var odemeler = new List<Odeme>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT o.OdemeID, o.ProjeID, o.Tutar, o.OdemeTarihi, o.Aciklama,
                           p.ProjeAdi, m.AdSoyad,
                           COALESCE(o.ParaBirimi, 'TL'), COALESCE(o.OdemeKuru, 1.0)
                    FROM Odemeler o
                    INNER JOIN Projeler p ON o.ProjeID = p.ProjeID
                    INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                    ORDER BY o.OdemeTarihi DESC;";

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    odemeler.Add(new Odeme
                    {
                        OdemeID = okuyucu.GetInt32(0),
                        ProjeID = okuyucu.GetInt32(1),
                        Tutar = Convert.ToDecimal(okuyucu.GetDouble(2)),
                        OdemeTarihi = DateTime.ParseExact(okuyucu.GetString(3), TarihFormati, CultureInfo.InvariantCulture),
                        Aciklama = okuyucu.IsDBNull(4) ? string.Empty : okuyucu.GetString(4),
                        ProjeAdi = okuyucu.GetString(5),
                        MusteriAdSoyad = okuyucu.GetString(6),
                        ParaBirimi = !okuyucu.IsDBNull(7) ? okuyucu.GetString(7) : ParaBirimleri.VARSAYILAN,
                        OdemeKuru = !okuyucu.IsDBNull(8) ? Convert.ToDecimal(okuyucu.GetDouble(8)) : 1.0m
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return odemeler;
        }

        /// <summary>
        /// Ödeme siler.
        /// </summary>
        public static bool OdemeSil(int odemeID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM Odemeler WHERE OdemeID = @odemeID;";
                komut.Parameters.AddWithValue("@odemeID", odemeID);

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
        /// Tüm ödemelerin toplamını TL bazında getirir (dövizli ödemeler kura göre çevrilir).
        /// </summary>
        public static decimal ToplamGelirGetir()
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT Tutar, COALESCE(ParaBirimi, 'TL'), COALESCE(OdemeKuru, 1.0)
                    FROM Odemeler;";

                decimal toplam = 0;
                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    var tutar = Convert.ToDecimal(okuyucu.GetDouble(0));
                    var paraBirimi = okuyucu.GetString(1);
                    var odemeKuru = Convert.ToDecimal(okuyucu.GetDouble(2));

                    toplam += paraBirimi == ParaBirimleri.TL ? tutar : tutar * odemeKuru;
                }

                return toplam;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return 0;
            }
        }

        /// <summary>
        /// Ödenmemiş toplam tutarı hesaplar (Proje fiyatları TL - ödemeler TL).
        /// </summary>
        public static decimal BekleyenOdemeGetir()
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                // Proje toplamını TL bazında hesapla
                decimal projeToplam = 0;
                using (var pKomut = baglanti.CreateCommand())
                {
                    pKomut.CommandText = @"
                        SELECT Fiyat, COALESCE(ParaBirimi, 'TL'), COALESCE(AnlasmaKuru, 1.0)
                        FROM Projeler;";
                    using var pOkuyucu = pKomut.ExecuteReader();
                    while (pOkuyucu.Read())
                    {
                        var fiyat = Convert.ToDecimal(pOkuyucu.GetDouble(0));
                        var pb = pOkuyucu.GetString(1);
                        var kur = Convert.ToDecimal(pOkuyucu.GetDouble(2));
                        projeToplam += pb == ParaBirimleri.TL ? fiyat : fiyat * kur;
                    }
                }

                // Ödeme toplamını TL bazında hesapla
                var odemeToplam = ToplamGelirGetir();

                var bekleyen = projeToplam - odemeToplam;
                return bekleyen < 0 ? 0 : bekleyen;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return 0;
            }
        }

        /// <summary>
        /// Aylık gelir verilerini getirir (son 12 ay).
        /// </summary>
        public static List<(string Ay, decimal Tutar)> AylikGelirGetir()
        {
            var veriler = new List<(string Ay, decimal Tutar)>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT strftime('%Y-%m', OdemeTarihi) AS Ay, 
                           SUM(CASE WHEN COALESCE(ParaBirimi, 'TL') = 'TL' THEN Tutar ELSE Tutar * COALESCE(OdemeKuru, 1.0) END) AS Toplam
                    FROM Odemeler
                    WHERE OdemeTarihi >= date('now', '-12 months')
                    GROUP BY strftime('%Y-%m', OdemeTarihi)
                    ORDER BY Ay ASC;";

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    veriler.Add((okuyucu.GetString(0), Convert.ToDecimal(okuyucu.GetDouble(1))));
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return veriler;
        }

        /// <summary>
        /// Belirli bir aydaki toplam geliri getirir.
        /// </summary>
        private static decimal AyGelirGetir(int yilFarki, int ayFarki)
        {
            try
            {
                var hedefAy = DateTime.Now.AddMonths(ayFarki).AddYears(yilFarki);
                var ayBaslangic = new DateTime(hedefAy.Year, hedefAy.Month, 1);
                var aySon = ayBaslangic.AddMonths(1);

                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT COALESCE(SUM(CASE WHEN COALESCE(ParaBirimi, 'TL') = 'TL' THEN Tutar ELSE Tutar * COALESCE(OdemeKuru, 1.0) END), 0) 
                    FROM Odemeler
                    WHERE OdemeTarihi >= @baslangic AND OdemeTarihi < @son;";

                komut.Parameters.AddWithValue("@baslangic", ayBaslangic.ToString(TarihFormati));
                komut.Parameters.AddWithValue("@son", aySon.ToString(TarihFormati));

                var sonuc = komut.ExecuteScalar();
                return sonuc != null ? Convert.ToDecimal(sonuc) : 0;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return 0;
            }
        }

        /// <summary>
        /// Bu aydaki toplam geliri getirir.
        /// </summary>
        public static decimal BuAyGelirGetir() => AyGelirGetir(0, 0);

        /// <summary>
        /// Geçen aydaki toplam geliri getirir.
        /// </summary>
        public static decimal GecenAyGelirGetir() => AyGelirGetir(0, -1);

        /// <summary>
        /// Belirli bir müşterinin tüm projelerine ait toplam ödeme tutarını getirir.
        /// N+1 sorgu problemini önlemek için tek bir SQL sorgusuyla hesaplar.
        /// </summary>
        public static decimal MusterininToplamOdemeleriniGetir(int musteriID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT COALESCE(SUM(CASE WHEN COALESCE(o.ParaBirimi, 'TL') = 'TL' THEN o.Tutar ELSE o.Tutar * COALESCE(o.OdemeKuru, 1.0) END), 0)
                    FROM Odemeler o
                    INNER JOIN Projeler p ON o.ProjeID = p.ProjeID
                    WHERE p.MusteriID = @musteriID;";

                komut.Parameters.AddWithValue("@musteriID", musteriID);

                var sonuc = komut.ExecuteScalar();
                return sonuc != null ? Convert.ToDecimal(sonuc) : 0;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return 0;
            }
        }
    }
}
