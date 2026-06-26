using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Müşteri notları tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// </summary>
    public static class MusteriNotuIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni müşteri notu ekler.
        /// </summary>
        public static bool NotEkle(MusteriNotu not)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO MusteriNotlari (MusteriID, NotMetni, IletisimTuru, OlusturmaTarihi)
                    VALUES (@musteriID, @notMetni, @iletisimTuru, @olusturmaTarihi);";

                komut.Parameters.AddWithValue("@musteriID", not.MusteriID);
                komut.Parameters.AddWithValue("@notMetni", not.NotMetni);
                komut.Parameters.AddWithValue("@iletisimTuru", not.IletisimTuru);
                komut.Parameters.AddWithValue("@olusturmaTarihi", not.OlusturmaTarihi.ToString(TarihFormati));

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
        /// Belirli bir müşterinin notlarını getirir (tarihsel sıra).
        /// </summary>
        public static List<MusteriNotu> MusteriNotlariniGetir(int musteriID)
        {
            var notlar = new List<MusteriNotu>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT NotID, MusteriID, NotMetni, IletisimTuru, OlusturmaTarihi
                    FROM MusteriNotlari
                    WHERE MusteriID = @musteriID
                    ORDER BY OlusturmaTarihi DESC;";

                komut.Parameters.AddWithValue("@musteriID", musteriID);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    notlar.Add(new MusteriNotu
                    {
                        NotID = okuyucu.GetInt32(0),
                        MusteriID = okuyucu.GetInt32(1),
                        NotMetni = okuyucu.GetString(2),
                        IletisimTuru = okuyucu.IsDBNull(3) ? string.Empty : okuyucu.GetString(3),
                        OlusturmaTarihi = DateTime.TryParseExact(okuyucu.GetString(4), TarihFormati,
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return notlar;
        }

        /// <summary>
        /// Müşteri notunu siler.
        /// </summary>
        public static bool NotSil(int notID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM MusteriNotlari WHERE NotID = @notID;";
                komut.Parameters.AddWithValue("@notID", notID);

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
