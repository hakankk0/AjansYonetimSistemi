using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Durum değişiklik geçmişi tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// </summary>
    public static class DurumGecmisiIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni durum değişikliği kaydı ekler.
        /// </summary>
        public static bool GecmisEkle(DurumGecmisi gecmis)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO DurumGecmisi (ProjeID, EskiDurum, YeniDurum, DegisimTarihi)
                    VALUES (@projeID, @eskiDurum, @yeniDurum, @degisimTarihi);";

                komut.Parameters.AddWithValue("@projeID", gecmis.ProjeID);
                komut.Parameters.AddWithValue("@eskiDurum", gecmis.EskiDurum);
                komut.Parameters.AddWithValue("@yeniDurum", gecmis.YeniDurum);
                komut.Parameters.AddWithValue("@degisimTarihi", gecmis.DegisimTarihi.ToString(TarihFormati));

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
        /// Belirli bir projenin durum geçmişini getirir.
        /// </summary>
        public static List<DurumGecmisi> ProjeGecmisiniGetir(int projeID)
        {
            var gecmisler = new List<DurumGecmisi>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT GecmisID, ProjeID, EskiDurum, YeniDurum, DegisimTarihi
                    FROM DurumGecmisi
                    WHERE ProjeID = @projeID
                    ORDER BY DegisimTarihi DESC;";

                komut.Parameters.AddWithValue("@projeID", projeID);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    gecmisler.Add(new DurumGecmisi
                    {
                        GecmisID = okuyucu.GetInt32(0),
                        ProjeID = okuyucu.GetInt32(1),
                        EskiDurum = okuyucu.GetString(2),
                        YeniDurum = okuyucu.GetString(3),
                        DegisimTarihi = DateTime.TryParseExact(okuyucu.GetString(4), TarihFormati, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return gecmisler;
        }
    }
}
