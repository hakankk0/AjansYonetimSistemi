using System;
using System.Collections.Generic;
using AjansYonetim.Modeller;

namespace AjansYonetim.Veritabani
{
    public static class BildirimIslemleri
    {
        public static void BildirimEkle(Bildirim bildirim)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                
                komut.CommandText = @"
                    INSERT INTO Bildirimler (CalisanID, Mesaj, OkunduMu, OlusturmaTarihi)
                    VALUES (@calisanID, @mesaj, 0, @tarih);";
                
                komut.Parameters.AddWithValue("@calisanID", bildirim.CalisanID.HasValue ? bildirim.CalisanID.Value : DBNull.Value);
                komut.Parameters.AddWithValue("@mesaj", bildirim.Mesaj);
                komut.Parameters.AddWithValue("@tarih", bildirim.OlusturmaTarihi.ToString("yyyy-MM-dd HH:mm:ss"));
                
                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }

        public static List<Bildirim> SonBildirimleriGetir(int limit = 50)
        {
            var list = new List<Bildirim>();
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                
                komut.CommandText = @"
                    SELECT BildirimID, CalisanID, Mesaj, OkunduMu, OlusturmaTarihi 
                    FROM Bildirimler
                    ORDER BY OlusturmaTarihi DESC LIMIT @limit;";
                komut.Parameters.AddWithValue("@limit", limit);

                using var okuyucu = komut.ExecuteReader();
                while (okuyucu.Read())
                {
                    list.Add(new Bildirim
                    {
                        BildirimID = okuyucu.GetInt32(0),
                        CalisanID = okuyucu.IsDBNull(1) ? null : okuyucu.GetInt32(1),
                        Mesaj = okuyucu.GetString(2),
                        OkunduMu = okuyucu.GetBoolean(3),
                        OlusturmaTarihi = DateTime.Parse(okuyucu.GetString(4))
                    });
                }
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }

            return list;
        }

        public static int OkunmayanBildirimSayisi()
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                komut.CommandText = "SELECT COUNT(*) FROM Bildirimler WHERE OkunduMu = 0;";
                var sonuc = komut.ExecuteScalar();
                return Convert.ToInt32(sonuc);
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return 0;
            }
        }

        public static void TümünüOkunduIsaretle()
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                komut.CommandText = "UPDATE Bildirimler SET OkunduMu = 1 WHERE OkunduMu = 0;";
                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }

        public static void BildirimSil(int bildirimId)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                
                komut.CommandText = "DELETE FROM Bildirimler WHERE BildirimID = @id;";
                komut.Parameters.AddWithValue("@id", bildirimId);
                
                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }

        public static void TumBildirimleriSil()
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();
                using var komut = baglanti.CreateCommand();
                
                komut.CommandText = "DELETE FROM Bildirimler;";
                komut.ExecuteNonQuery();
                AjansYonetim.Yardimcilar.ArkaPlanSenkronizasyon.DegisiklikBildir();
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }
    }
}
