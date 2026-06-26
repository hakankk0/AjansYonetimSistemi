using System;
using System.Collections.Generic;
using AjansYonetim.Modeller;

namespace AjansYonetim.Veritabani
{
    public static class AktiviteIslemleri
    {
        public static void AktiviteEkle(string metin, string ikon = "\uE718")
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                
                komut.CommandText = @"
                    INSERT INTO Aktiviteler (AksiyonMetni, Ikon, OlusturmaTarihi)
                    VALUES (@metin, @ikon, @tarih);";
                
                komut.Parameters.AddWithValue("@metin", metin);
                komut.Parameters.AddWithValue("@ikon", ikon);
                komut.Parameters.AddWithValue("@tarih", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }

        public static List<Aktivite> SonAktiviteleriGetir(int limit = 100)
        {
            var list = new List<Aktivite>();
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                
                komut.CommandText = @"
                    SELECT AktiviteID, AksiyonMetni, Ikon, OlusturmaTarihi 
                    FROM Aktiviteler
                    ORDER BY OlusturmaTarihi DESC LIMIT @limit;";
                komut.Parameters.AddWithValue("@limit", limit);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    list.Add(new Aktivite
                    {
                        AktiviteID = okuyucu.GetInt32(0),
                        AksiyonMetni = okuyucu.GetString(1),
                        Ikon = okuyucu.GetString(2),
                        OlusturmaTarihi = DateTime.Parse(okuyucu.GetString(3))
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return list;
        }
    }
}
