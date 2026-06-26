using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Proje-Çalışan ara tablosu için işlemleri gerçekleştiren sınıf.
    /// Many-to-many ilişki yönetimi.
    /// </summary>
    public static class ProjeCalisanIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Projeye çalışan atar.
        /// </summary>
        public static bool CalisanAta(int projeID, int calisanID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                // Zaten atanmış mı kontrol et
                using var kontrolKomut = baglanti.CreateCommand();
                kontrolKomut.CommandText = @"
                    SELECT COUNT(*) FROM ProjeCalisanlari
                    WHERE ProjeID = @projeID AND CalisanID = @calisanID;";
                kontrolKomut.Parameters.AddWithValue("@projeID", projeID);
                kontrolKomut.Parameters.AddWithValue("@calisanID", calisanID);

                var mevcutSayisi = Convert.ToInt32(kontrolKomut.ExecuteScalar());
                if (mevcutSayisi > 0) return true; // Zaten atanmış

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO ProjeCalisanlari (ProjeID, CalisanID)
                    VALUES (@projeID, @calisanID);";

                komut.Parameters.AddWithValue("@projeID", projeID);
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
        /// Projeden çalışan çıkarır.
        /// </summary>
        public static bool CalisanCikar(int projeID, int calisanID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    DELETE FROM ProjeCalisanlari
                    WHERE ProjeID = @projeID AND CalisanID = @calisanID;";

                komut.Parameters.AddWithValue("@projeID", projeID);
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
        /// Bir projenin çalışanlarını getirir.
        /// </summary>
        public static List<Calisan> ProjeninCalisanlariniGetir(int projeID)
        {
            var calisanlar = new List<Calisan>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT c.CalisanID, c.AdSoyad, c.Telefon, c.Eposta, c.Departman, c.Pozisyon,
                           c.CalisanTuru, c.IseBaslamaTarihi, c.Durum, c.Notlar
                    FROM Calisanlar c
                    INNER JOIN ProjeCalisanlari pc ON c.CalisanID = pc.CalisanID
                    WHERE pc.ProjeID = @projeID
                    ORDER BY c.AdSoyad;";

                komut.Parameters.AddWithValue("@projeID", projeID);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    DateTime.TryParseExact(
                        okuyucu.IsDBNull(7) ? string.Empty : okuyucu.GetString(7),
                        TarihFormati, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var iseBaslama);

                    calisanlar.Add(new Calisan
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
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return calisanlar;
        }

        /// <summary>
        /// Bir çalışanın projelerini getirir.
        /// </summary>
        public static List<Proje> CalisaninProjeleriniGetir(int calisanID)
        {
            var projeler = new List<Proje>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT p.ProjeID, p.MusteriID, p.ProjeAdi, p.BaslangicTarihi,
                           p.TeslimTarihi, p.Fiyat, p.Durum, m.AdSoyad,
                           COALESCE(p.TamamlanmaYuzdesi, 0), COALESCE(p.Kategori, '')
                    FROM Projeler p
                    INNER JOIN ProjeCalisanlari pc ON p.ProjeID = pc.ProjeID
                    INNER JOIN Musteriler m ON p.MusteriID = m.MusteriID
                    WHERE pc.CalisanID = @calisanID
                    ORDER BY p.TeslimTarihi ASC;";

                komut.Parameters.AddWithValue("@calisanID", calisanID);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    DateTime.TryParseExact(okuyucu.GetString(3), TarihFormati,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var baslangic);
                    DateTime.TryParseExact(okuyucu.GetString(4), TarihFormati,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var teslim);

                    projeler.Add(new Proje
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
                        Kategori = okuyucu.FieldCount > 9 && !okuyucu.IsDBNull(9)
                            ? okuyucu.GetString(9) : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return projeler;
        }

        /// <summary>
        /// Bir projenin tüm çalışan atamalarını siler.
        /// </summary>
        public static bool ProjeAtamalariniTemizle(int projeID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM ProjeCalisanlari WHERE ProjeID = @projeID;";
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
    }
}
