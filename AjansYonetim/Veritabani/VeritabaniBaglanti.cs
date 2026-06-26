using System;
using System.IO;
using System.Text.RegularExpressions;
using AjansYonetim.Sabitler;
using Microsoft.Data.Sqlite;

namespace AjansYonetim.Veritabani
{
    /// <summary>
    /// SQLite veritabanı bağlantısını yöneten sınıf.
    /// Tabloları oluşturur ve bağlantı sağlar.
    /// </summary>
    public static class VeritabaniBaglanti
    {
        /// <summary>
        /// Veritabanı dosya adı sabiti. Tekrar kullanım için public.
        /// </summary>
        public const string VeritabaniDosyaAdi = "AjansYonetim.db";

        /// <summary>
        /// Veritabanı dosyasının tam yolunu döndürür.
        /// Uygulama dizininde saklanır.
        /// </summary>
        public static string VeritabaniYolu
        {
            get
            {
                var uygulamaDizini = Yardimcilar.DosyaYollari.UygulamaVeriDizini;
                var lisansID = Yardimcilar.LisansYoneticisi.LisansDosyasiOku();
                string dosyaAdi = string.IsNullOrWhiteSpace(lisansID) 
                    ? VeritabaniDosyaAdi 
                    : $"AjansYonetim_{lisansID}.db";
                return Path.Combine(uygulamaDizini, dosyaAdi);
            }
        }

        /// <summary>
        /// SQLite bağlantı dizesini döndürür.
        /// </summary>
        public static string BaglantiDizesi => $"Data Source={VeritabaniYolu};Pooling=True";

        /// <summary>
        /// Yeni bir SQLite bağlantısı oluşturur ve döndürür.
        /// </summary>
        public static SqliteConnection BaglantiOlustur()
        {
            return new SqliteConnection(BaglantiDizesi);
        }

        /// <summary>
        /// Bağlantı açar ve PRAGMA foreign_keys'i aktif eder.
        /// Tüm DB işlemlerinde bu metod kullanılmalıdır.
        /// </summary>
        public static SqliteConnection BaglantiAcVeHazirla()
        {
            var baglanti = new SqliteConnection(BaglantiDizesi);
            baglanti.Open();

            using var fkKomut = baglanti.CreateCommand();
            fkKomut.CommandText = "PRAGMA foreign_keys = ON;";
            fkKomut.ExecuteNonQuery();

            return baglanti;
        }

        /// <summary>
        /// Veritabanı tablolarını oluşturur (yoksa).
        /// Uygulama başlangıcında çağrılmalıdır.
        /// </summary>
        public static void VeritabaniBaslat()
        {
            try
            {
                using var baglanti = BaglantiAcVeHazirla();

                // Müşteriler tablosunu oluştur
                using var musteriKomut = baglanti.CreateCommand();
                musteriKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Musteriler (
                        MusteriID   INTEGER PRIMARY KEY AUTOINCREMENT,
                        AdSoyad     NVARCHAR(200) NOT NULL,
                        Telefon     NVARCHAR(20),
                        Eposta      NVARCHAR(200),
                        SirketAdi   NVARCHAR(300),
                        VergiNo     NVARCHAR(50),
                        Adres       NVARCHAR(500),
                        Notlar      NVARCHAR(1000)
                    );";
                musteriKomut.ExecuteNonQuery();

                // Projeler tablosunu oluştur (ON DELETE CASCADE ekli)
                using var projeKomut = baglanti.CreateCommand();
                projeKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Projeler (
                        ProjeID         INTEGER PRIMARY KEY AUTOINCREMENT,
                        MusteriID       INTEGER NOT NULL,
                        ProjeAdi        NVARCHAR(300) NOT NULL,
                        BaslangicTarihi TEXT NOT NULL,
                        TeslimTarihi    TEXT NOT NULL,
                        Fiyat           REAL NOT NULL DEFAULT 0,
                        Durum           NVARCHAR(50) NOT NULL DEFAULT 'Görev Atandı',
                        FOREIGN KEY (MusteriID) REFERENCES Musteriler(MusteriID) ON DELETE CASCADE
                    );";
                projeKomut.ExecuteNonQuery();

                // Ayarlar tablosunu oluştur (anahtar-değer çifti)
                using var ayarKomut = baglanti.CreateCommand();
                ayarKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Ayarlar (
                        Anahtar NVARCHAR(100) PRIMARY KEY,
                        Deger   NVARCHAR(500)
                    );";
                ayarKomut.ExecuteNonQuery();

                // Proje Notları tablosu
                using var notKomut = baglanti.CreateCommand();
                notKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ProjeNotlari (
                        NotID           INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProjeID         INTEGER NOT NULL,
                        NotMetni        NVARCHAR(2000) NOT NULL,
                        OlusturmaTarihi TEXT NOT NULL,
                        FOREIGN KEY (ProjeID) REFERENCES Projeler(ProjeID) ON DELETE CASCADE
                    );";
                notKomut.ExecuteNonQuery();

                // Ödemeler tablosu
                using var odemeKomut = baglanti.CreateCommand();
                odemeKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Odemeler (
                        OdemeID         INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProjeID         INTEGER NOT NULL,
                        Tutar           REAL NOT NULL,
                        OdemeTarihi     TEXT NOT NULL,
                        Aciklama        NVARCHAR(500),
                        FOREIGN KEY (ProjeID) REFERENCES Projeler(ProjeID) ON DELETE CASCADE
                    );";
                odemeKomut.ExecuteNonQuery();

                // Durum Geçmişi tablosu
                using var gecmisKomut = baglanti.CreateCommand();
                gecmisKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS DurumGecmisi (
                        GecmisID        INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProjeID         INTEGER NOT NULL,
                        EskiDurum       NVARCHAR(50) NOT NULL,
                        YeniDurum       NVARCHAR(50) NOT NULL,
                        DegisimTarihi   TEXT NOT NULL,
                        FOREIGN KEY (ProjeID) REFERENCES Projeler(ProjeID) ON DELETE CASCADE
                    );";
                gecmisKomut.ExecuteNonQuery();

                // Proje Şablonları tablosu
                using var sablonKomut = baglanti.CreateCommand();
                sablonKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ProjeSablonlari (
                        SablonID            INTEGER PRIMARY KEY AUTOINCREMENT,
                        Ad                  NVARCHAR(200) NOT NULL,
                        VarsayilanSureGun   INTEGER NOT NULL DEFAULT 7,
                        TahminiFiyat        REAL NOT NULL DEFAULT 0
                    );";
                sablonKomut.ExecuteNonQuery();

                // Migration: Mevcut veritabanına yeni sütunlar ekle (hata olursa zaten var demektir)
                SutunEkle(baglanti, "Musteriler", "SirketAdi", "NVARCHAR(300)");
                SutunEkle(baglanti, "Musteriler", "VergiNo", "NVARCHAR(50)");
                SutunEkle(baglanti, "Musteriler", "Adres", "NVARCHAR(500)");
                SutunEkle(baglanti, "Musteriler", "Notlar", "NVARCHAR(1000)");
                SutunEkle(baglanti, "Projeler", "TamamlanmaYuzdesi", "INTEGER DEFAULT 0");
                SutunEkle(baglanti, "Projeler", "Kategori", "NVARCHAR(100) DEFAULT ''");

                // Varsayılan şablonları ekle (ilk kullanımda)
                SablonIslemleri.VarsayilanSablonlariEkle();

                // Müşteri Notları tablosu
                using var musteriNotKomut = baglanti.CreateCommand();
                musteriNotKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS MusteriNotlari (
                        NotID               INTEGER PRIMARY KEY AUTOINCREMENT,
                        MusteriID           INTEGER NOT NULL,
                        NotMetni            NVARCHAR(2000) NOT NULL,
                        IletisimTuru        NVARCHAR(100),
                        OlusturmaTarihi     TEXT NOT NULL,
                        FOREIGN KEY (MusteriID) REFERENCES Musteriler(MusteriID) ON DELETE CASCADE
                    );";
                musteriNotKomut.ExecuteNonQuery();

                // ═══════════════ ÇALIŞAN TABLOLARI ═══════════════

                // Çalışanlar tablosu
                using var calisanKomut = baglanti.CreateCommand();
                calisanKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Calisanlar (
                        CalisanID           INTEGER PRIMARY KEY AUTOINCREMENT,
                        AdSoyad             NVARCHAR(200) NOT NULL,
                        Telefon             NVARCHAR(20),
                        Eposta              NVARCHAR(200),
                        Departman           NVARCHAR(100),
                        Pozisyon            NVARCHAR(200),
                        CalisanTuru         NVARCHAR(50) NOT NULL DEFAULT 'Yurt İçi',
                        IseBaslamaTarihi    TEXT,
                        Durum               NVARCHAR(50) NOT NULL DEFAULT 'Aktif',
                        Notlar              NVARCHAR(1000)
                    );";
                calisanKomut.ExecuteNonQuery();

                // Proje-Çalışan ara tablosu (many-to-many)
                using var projeCalisanKomut = baglanti.CreateCommand();
                projeCalisanKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ProjeCalisanlari (
                        ProjeID     INTEGER NOT NULL,
                        CalisanID   INTEGER NOT NULL,
                        PRIMARY KEY (ProjeID, CalisanID),
                        FOREIGN KEY (ProjeID) REFERENCES Projeler(ProjeID) ON DELETE CASCADE,
                        FOREIGN KEY (CalisanID) REFERENCES Calisanlar(CalisanID) ON DELETE CASCADE
                    );";
                projeCalisanKomut.ExecuteNonQuery();

                // Görevler tablosu (proje alt görevleri / yapılacaklar listesi)
                using var gorevKomut = baglanti.CreateCommand();
                gorevKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Gorevler (
                        GorevID             INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProjeID             INTEGER NOT NULL,
                        CalisanID           INTEGER,
                        Baslik              NVARCHAR(300) NOT NULL,
                        Aciklama            NVARCHAR(1000),
                        Tamamlandi          INTEGER NOT NULL DEFAULT 0,
                        OlusturmaTarihi     TEXT NOT NULL,
                        TamamlanmaTarihi    TEXT,
                        FOREIGN KEY (ProjeID) REFERENCES Projeler(ProjeID) ON DELETE CASCADE,
                        FOREIGN KEY (CalisanID) REFERENCES Calisanlar(CalisanID) ON DELETE SET NULL
                    );";
                gorevKomut.ExecuteNonQuery();

                // ── Faturalar Tablosu ──
                var faturaKomut = baglanti.CreateCommand();
                faturaKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Faturalar (
                        FaturaID        INTEGER PRIMARY KEY AUTOINCREMENT,
                        MusteriID       INTEGER NOT NULL,
                        ProjeID         INTEGER,
                        FaturaNo        NVARCHAR(50) NOT NULL UNIQUE,
                        FaturaTuru      NVARCHAR(20) NOT NULL DEFAULT 'Fatura',
                        Tarih           TEXT NOT NULL,
                        AraToplam       REAL NOT NULL DEFAULT 0,
                        KDVOrani        INTEGER NOT NULL DEFAULT 20,
                        ToplamTutar     REAL NOT NULL DEFAULT 0,
                        Aciklama        NVARCHAR(500),
                        FOREIGN KEY (MusteriID) REFERENCES Musteriler(MusteriID) ON DELETE CASCADE,
                        FOREIGN KEY (ProjeID) REFERENCES Projeler(ProjeID) ON DELETE SET NULL
                    );";
                faturaKomut.ExecuteNonQuery();

                // ── Bildirimler Tablosu ──
                using var bildirimKomut = baglanti.CreateCommand();
                bildirimKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Bildirimler (
                        BildirimID      INTEGER PRIMARY KEY AUTOINCREMENT,
                        CalisanID       INTEGER,
                        Mesaj           NVARCHAR(500) NOT NULL,
                        OkunduMu        INTEGER NOT NULL DEFAULT 0,
                        OlusturmaTarihi TEXT NOT NULL,
                        FOREIGN KEY (CalisanID) REFERENCES Calisanlar(CalisanID) ON DELETE CASCADE
                    );";
                bildirimKomut.ExecuteNonQuery();

                // ── Aktiviteler Tablosu (Geçmiş Hareketler Logu) ──
                using var aktiviteKomut = baglanti.CreateCommand();
                aktiviteKomut.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Aktiviteler (
                        AktiviteID      INTEGER PRIMARY KEY AUTOINCREMENT,
                        AksiyonMetni    NVARCHAR(500) NOT NULL,
                        Ikon            NVARCHAR(50) DEFAULT '\uE718',
                        OlusturmaTarihi TEXT NOT NULL
                    );";
                aktiviteKomut.ExecuteNonQuery();

                // Migration: Müşteri tablosuna MusteriTuru sütunu ekle
                SutunEkle(baglanti, "Musteriler", "MusteriTuru", "NVARCHAR(50) DEFAULT 'Yurt İçi'");

                // Migration: Çoklu para birimi desteği
                SutunEkle(baglanti, "Projeler", "ParaBirimi", "NVARCHAR(10) DEFAULT 'TL'");
                SutunEkle(baglanti, "Projeler", "AnlasmaKuru", "REAL DEFAULT 1.0");
                SutunEkle(baglanti, "Odemeler", "ParaBirimi", "NVARCHAR(10) DEFAULT 'TL'");
                SutunEkle(baglanti, "Odemeler", "OdemeKuru", "REAL DEFAULT 1.0");
                SutunEkle(baglanti, "Faturalar", "ParaBirimi", "NVARCHAR(10) DEFAULT 'TL'");

                // Migration: Eski proje durumlarını yeni durumlara güncelle
                EskiDurumlariGuncelle(baglanti);
            }
            catch (Exception ex)
            {
                App.HataKaydet(ex);
            }
        }

        /// <summary>
        /// İzin verilen tablo ve sütun adı pattern'i.
        /// SQL Injection'ı önlemek için whitelist doğrulaması.
        /// </summary>
        private static readonly Regex GecerliSqlIsimPattern = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        /// Güvenli sütun ekleme: Whitelist doğrulaması + sütun zaten varsa hata vermez.
        /// </summary>
        private static void SutunEkle(SqliteConnection baglanti, string tabloAdi, string sutunAdi, string sutunTipi)
        {
            // SQL Injection önlemi: sadece geçerli SQL isimleri kabul et
            if (!GecerliSqlIsimPattern.IsMatch(tabloAdi) || !GecerliSqlIsimPattern.IsMatch(sutunAdi))
            {
                return;
            }

            try
            {
                using var komut = baglanti.CreateCommand();
                komut.CommandText = $"ALTER TABLE {tabloAdi} ADD COLUMN {sutunAdi} {sutunTipi};";
                komut.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Sütun zaten mevcut — sorun yok
            }
        }

        /// <summary>
        /// Eski proje durumlarını yeni durumlara günceller.
        /// Brief Alındı → Görev Atandı, Tasarım Aşamasında → Devam Ediyor, Revizyon Bekliyor → Devam Ediyor
        /// </summary>
        private static void EskiDurumlariGuncelle(SqliteConnection baglanti)
        {
            try
            {
                var durumEslesmeleri = new[]
                {
                    (ProjeDurumlari.ESKI_BRIEF_ALINDI, ProjeDurumlari.GOREV_ATANDI),
                    (ProjeDurumlari.ESKI_TASARIM_ASAMASINDA, ProjeDurumlari.DEVAM_EDIYOR),
                    (ProjeDurumlari.ESKI_REVIZYON_BEKLIYOR, ProjeDurumlari.DEVAM_EDIYOR)
                };

                foreach (var (eskiDurum, yeniDurum) in durumEslesmeleri)
                {
                    using var komut = baglanti.CreateCommand();
                    komut.CommandText = "UPDATE Projeler SET Durum = @yeniDurum WHERE Durum = @eskiDurum;";
                    komut.Parameters.AddWithValue("@yeniDurum", yeniDurum);
                    komut.Parameters.AddWithValue("@eskiDurum", eskiDurum);
                    komut.ExecuteNonQuery();
                }
            }
            catch (SqliteException)
            {
                // Migration zaten yapılmış olabilir — sorun yok
            }
        }
    }
}
