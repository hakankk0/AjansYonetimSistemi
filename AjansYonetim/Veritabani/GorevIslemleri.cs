using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Görevler tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// Proje alt görevleri (yapılacaklar listesi) yönetimi.
    /// </summary>
    public static class GorevIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni görev ekler.
        /// </summary>
        public static bool GorevEkle(Gorev gorev)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO Gorevler (ProjeID, CalisanID, Baslik, Aciklama, Tamamlandi, OlusturmaTarihi)
                    VALUES (@projeID, @calisanID, @baslik, @aciklama, 0, @olusturmaTarihi);";

                komut.Parameters.AddWithValue("@projeID", gorev.ProjeID);
                komut.Parameters.AddWithValue("@calisanID", gorev.CalisanID.HasValue ? (object)gorev.CalisanID.Value : DBNull.Value);
                komut.Parameters.AddWithValue("@baslik", gorev.Baslik);
                komut.Parameters.AddWithValue("@aciklama", (object?)gorev.Aciklama ?? DBNull.Value);
                komut.Parameters.AddWithValue("@olusturmaTarihi", gorev.OlusturmaTarihi.ToString(TarihFormati));

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
        /// Görevi günceller (başlık, açıklama, çalışan ataması).
        /// </summary>
        public static bool GorevGuncelle(Gorev gorev)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    UPDATE Gorevler
                    SET Baslik     = @baslik,
                        Aciklama   = @aciklama,
                        CalisanID  = @calisanID
                    WHERE GorevID = @gorevID;";

                komut.Parameters.AddWithValue("@gorevID", gorev.GorevID);
                komut.Parameters.AddWithValue("@baslik", gorev.Baslik);
                komut.Parameters.AddWithValue("@aciklama", (object?)gorev.Aciklama ?? DBNull.Value);
                komut.Parameters.AddWithValue("@calisanID", gorev.CalisanID.HasValue ? (object)gorev.CalisanID.Value : DBNull.Value);

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
        /// Görevin tamamlanma durumunu değiştirir.
        /// Tamamlandığında TamamlanmaTarihi set edilir, geri alındığında null yapılır.
        /// </summary>
        public static bool GorevTamamlaDurumuDegistir(int gorevID, bool tamamlandi)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    UPDATE Gorevler
                    SET Tamamlandi       = @tamamlandi,
                        TamamlanmaTarihi = @tamamlanmaTarihi
                    WHERE GorevID = @gorevID;";

                komut.Parameters.AddWithValue("@gorevID", gorevID);
                komut.Parameters.AddWithValue("@tamamlandi", tamamlandi ? 1 : 0);
                komut.Parameters.AddWithValue("@tamamlanmaTarihi",
                    tamamlandi ? (object)DateTime.Now.ToString(TarihFormati) : DBNull.Value);

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
        /// Görevi siler.
        /// </summary>
        public static bool GorevSil(int gorevID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM Gorevler WHERE GorevID = @gorevID;";
                komut.Parameters.AddWithValue("@gorevID", gorevID);

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
        /// Belirli bir projenin görevlerini getirir.
        /// Çalışan adı LEFT JOIN ile eklenir.
        /// Sıralama: tamamlanmayanlar önce, sonra oluşturma tarihine göre.
        /// </summary>
        public static List<Gorev> ProjeGorevleriniGetir(int projeID)
        {
            var gorevler = new List<Gorev>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT g.GorevID, g.ProjeID, g.CalisanID, g.Baslik, g.Aciklama,
                           g.Tamamlandi, g.OlusturmaTarihi, g.TamamlanmaTarihi,
                           COALESCE(c.AdSoyad, '')
                    FROM Gorevler g
                    LEFT JOIN Calisanlar c ON g.CalisanID = c.CalisanID
                    WHERE g.ProjeID = @projeID
                    ORDER BY g.Tamamlandi ASC, g.OlusturmaTarihi DESC;";

                komut.Parameters.AddWithValue("@projeID", projeID);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    DateTime.TryParseExact(
                        okuyucu.GetString(6), TarihFormati,
                        CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out var olusturmaTarihi);

                    DateTime? tamamlanmaTarihi = null;
                    if (!okuyucu.IsDBNull(7))
                    {
                        if (DateTime.TryParseExact(
                            okuyucu.GetString(7), TarihFormati,
                            CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out var tamamDt))
                        {
                            tamamlanmaTarihi = tamamDt;
                        }
                    }

                    gorevler.Add(new Gorev
                    {
                        GorevID = okuyucu.GetInt32(0),
                        ProjeID = okuyucu.GetInt32(1),
                        CalisanID = okuyucu.IsDBNull(2) ? null : okuyucu.GetInt32(2),
                        Baslik = okuyucu.GetString(3),
                        Aciklama = okuyucu.IsDBNull(4) ? null : okuyucu.GetString(4),
                        Tamamlandi = okuyucu.GetInt32(5) == 1,
                        OlusturmaTarihi = olusturmaTarihi,
                        TamamlanmaTarihi = tamamlanmaTarihi,
                        CalisanAdSoyad = okuyucu.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return gorevler;
        }
    }
}
