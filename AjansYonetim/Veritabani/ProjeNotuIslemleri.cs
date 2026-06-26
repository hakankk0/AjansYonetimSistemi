using System;
using System.Collections.Generic;
using System.Globalization;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Proje notları tablosu için CRUD işlemlerini gerçekleştiren sınıf.
    /// </summary>
    public static class ProjeNotuIslemleri
    {
        private const string TarihFormati = VeritabaniSabitleri.TarihFormati;

        /// <summary>
        /// Yeni not ekler.
        /// </summary>
        public static bool NotEkle(ProjeNotu not)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO ProjeNotlari (ProjeID, NotMetni, OlusturmaTarihi)
                    VALUES (@projeID, @notMetni, @olusturmaTarihi);";

                komut.Parameters.AddWithValue("@projeID", not.ProjeID);
                komut.Parameters.AddWithValue("@notMetni", not.NotMetni);
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
        /// Belirli bir projenin notlarını getirir.
        /// </summary>
        public static List<ProjeNotu> ProjeNotlariniGetir(int projeID)
        {
            var notlar = new List<ProjeNotu>();

            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    SELECT NotID, ProjeID, NotMetni, OlusturmaTarihi
                    FROM ProjeNotlari
                    WHERE ProjeID = @projeID
                    ORDER BY OlusturmaTarihi DESC;";

                komut.Parameters.AddWithValue("@projeID", projeID);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    notlar.Add(new ProjeNotu
                    {
                        NotID = okuyucu.GetInt32(0),
                        ProjeID = okuyucu.GetInt32(1),
                        NotMetni = okuyucu.GetString(2),
                        OlusturmaTarihi = DateTime.TryParseExact(okuyucu.GetString(3), TarihFormati, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.Now
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
        /// Notu siler.
        /// </summary>
        public static bool NotSil(int notID)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "DELETE FROM ProjeNotlari WHERE NotID = @notID;";
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
