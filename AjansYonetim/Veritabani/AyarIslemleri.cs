using System;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// Ayarlar tablosu üzerinde CRUD işlemlerini yöneten sınıf.
    /// Anahtar-değer çifti olarak ayarları saklar.
    /// </summary>
    public static class AyarIslemleri
    {
        // ═══════════════ AYAR ANAHTARLARI ═══════════════

        /// <summary>Ajans adı ayar anahtarı.</summary>
        public const string ANAHTAR_AJANS_ADI = "AjansAdi";

        /// <summary>Ajans telefon ayar anahtarı.</summary>
        public const string ANAHTAR_AJANS_TELEFON = "AjansTelefon";

        /// <summary>Ajans e-posta ayar anahtarı.</summary>
        public const string ANAHTAR_AJANS_EPOSTA = "AjansEposta";

        /// <summary>Ajans adres ayar anahtarı.</summary>
        public const string ANAHTAR_AJANS_ADRES = "AjansAdres";

        /// <summary>Acil proje gün eşiği ayar anahtarı.</summary>
        public const string ANAHTAR_ACIL_GUN_ESIGI = "AcilGunEsigi";

        /// <summary>Otomatik yedekleme aralığı ayar anahtarı.</summary>
        public const string ANAHTAR_YEDEKLEME_ARALIGI = "YedeklemeAraligi";

        // ═══════════════ CRUD İŞLEMLERİ ═══════════════

        /// <summary>
        /// Belirtilen anahtara ait ayar değerini döndürür.
        /// Anahtar bulunamazsa varsayılan değeri döndürür.
        /// </summary>
        public static string AyarGetir(string anahtar, string varsayilanDeger = "")
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = "SELECT Deger FROM Ayarlar WHERE Anahtar = @anahtar";
                komut.Parameters.AddWithValue("@anahtar", anahtar);

                var sonuc = komut.ExecuteScalar();
                return sonuc?.ToString() ?? varsayilanDeger;
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
                return varsayilanDeger;
            }
        }

        /// <summary>
        /// Belirtilen anahtara ait ayarı kaydeder veya günceller.
        /// </summary>
        public static bool AyarKaydet(string anahtar, string deger)
        {
            try
            {
                using var baglanti = VeritabaniBaglanti.BaglantiAcVeHazirla();

                using var komut = baglanti.CreateCommand();
                komut.CommandText = @"
                    INSERT INTO Ayarlar (Anahtar, Deger) 
                    VALUES (@anahtar, @deger)
                    ON CONFLICT(Anahtar) DO UPDATE SET Deger = @deger;";
                komut.Parameters.AddWithValue("@anahtar", anahtar);
                komut.Parameters.AddWithValue("@deger", deger);

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
