using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using AjansYonetim.Sabitler;

namespace AjansYonetim.Veritabani
{
    public class AjansModel
    {
        public string AjansAdi { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AgencyId { get; set; } = string.Empty; // Bulut Yedekleme ve kimlik için asıl anahtar
        public string KayitTarihi { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;
        public string SifreSalt { get; set; } = string.Empty;
        public List<string> GuvenliCihazlar { get; set; } = new List<string>();
    }

    public class OTPModel
    {
        public string Kod { get; set; } = string.Empty;
        public DateTime SonKullanmaTarihi { get; set; }
    }

    /// <summary>
    /// Firebase kullanılarak ajans kayıt ve e-posta OTP tabanlı kimlik doğrulamasını yöneten servis.
    /// </summary>
    public static class AuthServisi
    {
        private static readonly HttpClient _httpClient = new HttpClient();


        /// <summary>
        /// E-posta adreslerindeki '.', '#', '$', '[', ']' gibi Firebase key'leri olarak yasaklı karakterleri güvenli hale getirir.
        /// Örn: ornek@gmail.com -> ornek@gmail,com
        /// </summary>
        public static string GuvenliEmailAnahtari(string email)
        {
             return email.Trim().ToLowerInvariant().Replace(".", ",");
        }

        /// <summary>
        /// Yeni bir ajansın Firebase üzerine kaydını gerçekleştirir.
        /// </summary>
        /// <returns>Eğer e-posta zaten kayıtlıysa false, kayıt başarılıysa true döner.</returns>
        public static async Task<bool> KayitOlAsync(string ajansAdi, string telefon, string email, string parola)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 
                 // E-posta var mı kontrolü
                 var mevcutCevap = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 if (mevcutCevap != "null" && !string.IsNullOrWhiteSpace(mevcutCevap))
                 {
                      return false; // Zaten bu e-posta ile kayıtlı bir ajans var.
                 }

                 // Sınırlama ve Çakışmayı önlemek için yeni bir Unique ID (Guid) 
                 var yeniAjansId = Guid.NewGuid().ToString("N").ToUpper();

                 var hashSonuc = AjansYonetim.Yardimcilar.SifrelemeYardimcisi.SifreHashle(parola);

                 var yeniAjans = new AjansModel
                 {
                     AjansAdi = ajansAdi,
                     Telefon = telefon,
                     Email = email.Trim().ToLowerInvariant(),
                     AgencyId = yeniAjansId,
                     KayitTarihi = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                     SifreHash = hashSonuc.hash,
                     SifreSalt = hashSonuc.salt,
                     GuvenliCihazlar = new List<string>()
                 };

                 var json = JsonSerializer.Serialize(yeniAjans);
                 var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                 var kayitCevap = await _httpClient.PutAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json", content);
                 return kayitCevap.IsSuccessStatusCode;
             }
             catch(Exception ex)
             {
                 App.HataKaydet(ex);
                 return false;
             }
        }

        /// <summary>
        /// Giriş öncesi hesap varlığını ve parolayı doğrular
        /// </summary>
        public static async Task<(bool gecerli, bool yeniCihaz, AjansModel? ajans)> GirisIcinParolaDogrulaAsync(string email, string parola, string cihazId)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 var ajansCevap = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 if (ajansCevap == "null" || string.IsNullOrWhiteSpace(ajansCevap)) 
                      return (false, false, null); 
                      
                 var ajans = JsonSerializer.Deserialize<AjansModel>(ajansCevap);
                 if (ajans == null) return (false, false, null);

                 bool sifreDogru = AjansYonetim.Yardimcilar.SifrelemeYardimcisi.SifreDogrula(parola, ajans.SifreHash, ajans.SifreSalt);
                 if (!sifreDogru) 
                      return (false, false, null);
                      
                 bool cihazKayitli = ajans.GuvenliCihazlar != null && ajans.GuvenliCihazlar.Contains(cihazId);
                 if (!cihazKayitli)
                 {
                      await KodGonderVeGirisIstegiBaslatAsync(email);
                      return (true, true, ajans);
                 }
                 
                 return (true, false, ajans);
             }
             catch
             {
                 return (false, false, null);
             }
        }

        public static async Task<bool> CihaziGuvenceyeAlAsync(string email, string cihazId)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 var ajansCevap = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 if (ajansCevap == "null" || string.IsNullOrWhiteSpace(ajansCevap)) return false;

                 var ajans = JsonSerializer.Deserialize<AjansModel>(ajansCevap);
                 if (ajans == null) return false;

                 if (ajans.GuvenliCihazlar == null) ajans.GuvenliCihazlar = new List<string>();
                 
                 if (!ajans.GuvenliCihazlar.Contains(cihazId))
                 {
                      ajans.GuvenliCihazlar.Add(cihazId);
                      var content = new StringContent(JsonSerializer.Serialize(ajans), System.Text.Encoding.UTF8, "application/json");
                      await _httpClient.PutAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json", content);
                 }
                 return true;
             }
             catch { return false; }
        }

        /// <summary>
        /// Firebase üzerinden e-postayı bulur ve 6 haneli kod üretip mail gönderir.
        /// </summary>
        /// <returns>Eğer ajans kayıtlıysa ve mail başarılı gönderilirse true, henüz kayıtlı degilse veya hata varsa false.</returns>
        public static async Task<bool> KodGonderVeGirisIstegiBaslatAsync(string email)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 // Kayıtlı ajans mı?
                 var ajansCevap = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 if (ajansCevap == "null" || string.IsNullOrWhiteSpace(ajansCevap))
                      return false; // Ajans bulunamadı! Önce kayıt olmalı.

                 // 6 Haneli Kriptografik Güvenli Şifre Üret
                 string kod = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

                 var otp = new OTPModel
                 {
                     Kod = kod,
                     SonKullanmaTarihi = DateTime.Now.AddMinutes(3) // 3 dakika geçerli
                 };

                 var content = new StringContent(JsonSerializer.Serialize(otp), System.Text.Encoding.UTF8, "application/json");
                 
                 // Kodu Firebase'e yaz
                 var otpCevap = await _httpClient.PutAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json", content);
                 
                 if(otpCevap.IsSuccessStatusCode)
                 {
                      // Mail Gönder!
                      return await AjansYonetim.Yardimcilar.EmailServisi.DogrulamaKoduGonderAsync(email, kod);
                 }
                 return false;
             }
             catch (Exception ex)
             {
                 App.HataKaydet(ex);
                 return false;
             }
        }

        /// <summary>
        /// Ajans kayıt olmadan önce e-postasının başkası tarafından alınmadığından emin olur ve OTP gönderir.
        /// </summary>
        public static async Task<bool> KodGonderVeKayitIstegiBaslatAsync(string email)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 // Kayıtlı ajans mı?
                 var ajansCevap = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 if (ajansCevap != "null" && !string.IsNullOrWhiteSpace(ajansCevap))
                      return false; // Ajans zaten kayıtlı!

                 // 6 Haneli Kriptografik Güvenli Şifre Üret
                 string kod = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

                 var otp = new OTPModel
                 {
                     Kod = kod,
                     SonKullanmaTarihi = DateTime.Now.AddMinutes(3)
                 };

                 var content = new StringContent(JsonSerializer.Serialize(otp), System.Text.Encoding.UTF8, "application/json");
                 
                 // Kodu Firebase'e yaz
                 var otpCevap = await _httpClient.PutAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json", content);
                 
                 if(otpCevap.IsSuccessStatusCode)
                 {
                      // Mail Gönder!
                      return await AjansYonetim.Yardimcilar.EmailServisi.DogrulamaKoduGonderAsync(email, kod);
                 }
                 return false;
             }
             catch (Exception ex)
             {
                 App.HataKaydet(ex);
                 return false;
             }
        }

        /// <summary>
        /// Sadece OTP (Tek Kullanımlık Şifre) doğrulaması yapar. (Kayıt olurken kullanılır).
        /// Başarılıysa OTP'yi siler ve true döner.
        /// </summary>
        public static async Task<bool> KoduDogrulaVeSilAsync(string email, string girilenKod)
        {
            try
            {
                 var anahtar = GuvenliEmailAnahtari(email);

                 var otpSorgu = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json");
                 if (otpSorgu == "null" || string.IsNullOrWhiteSpace(otpSorgu))
                      return false; 

                 var otp = JsonSerializer.Deserialize<OTPModel>(otpSorgu);
                 if(otp == null || otp.Kod != girilenKod.Trim())
                      return false; 
                      
                 if (DateTime.Now > otp.SonKullanmaTarihi)
                 {
                      await _httpClient.DeleteAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json");
                      return false; 
                 }

                 await _httpClient.DeleteAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json");
                 return true;
            }
            catch (Exception ex)
            {
                 App.HataKaydet(ex);
                 return false;
            }
        }
        
        /// <summary>
        /// E-posta ve 6 haneli kod ile doğrulama yapar.
        /// Başarılıysa, AgencyId, ve AjansAdı bilgisini döner. (Giriş yaparken kullanılır).
        /// </summary>
        public static async Task<AjansModel?> KoduDogrulaGirisIcinAsync(string email, string girilenKod)
        {
            try
            {
                 var anahtar = GuvenliEmailAnahtari(email);

                 // Firebase'den OTP bilgisini oku
                 var otpSorgu = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json");
                 if (otpSorgu == "null" || string.IsNullOrWhiteSpace(otpSorgu))
                      return null; // OTP Bulunamadı (süresi dolmuş veya hiç istenmemiş)

                 var otp = JsonSerializer.Deserialize<OTPModel>(otpSorgu);
                 if(otp == null || otp.Kod != girilenKod.Trim())
                      return null; // Yanlış kod
                      
                 if (DateTime.Now > otp.SonKullanmaTarihi)
                 {
                      // Süresi Dolmuş, temizle
                      await _httpClient.DeleteAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json");
                      return null; 
                 }

                 // Doğrulandı! Ajans bilgisini çekelim
                 var ajansSorgu = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 if (ajansSorgu != "null" && !string.IsNullOrWhiteSpace(ajansSorgu))
                 {
                      var ajans = JsonSerializer.Deserialize<AjansModel>(ajansSorgu);
                      
                      // İşimiz bitti, şifreyi Firebase'den ucuralım
                      await _httpClient.DeleteAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json");
                      
                      return ajans;
                 }

                 return null;
            }
            catch (Exception ex)
            {
                 App.HataKaydet(ex);
                 return null;
            }
        }

        /// <summary>
        /// Şifre sıfırlama işlemi için önce emailin veritabanında var olup olmadığını kontrol eder.
        /// Varsa 6 haneli OTP üretip gönderir.
        /// </summary>
        public static async Task<bool> KodGonderVeSifreSifirlamaIstegiBaslatAsync(string email)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 
                 // Ajans gerçekten kayıtlı mı?
                 var ajansCevap = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 if (ajansCevap == "null" || string.IsNullOrWhiteSpace(ajansCevap))
                      return false; // Böyle bir hesap yok.

                 // Doğrulama Kodu Üret
                 string kod = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
                 var otp = new OTPModel
                 {
                     Kod = kod,
                     SonKullanmaTarihi = DateTime.Now.AddMinutes(5) // 5 dakika geçerli
                 };

                 var content = new StringContent(JsonSerializer.Serialize(otp), System.Text.Encoding.UTF8, "application/json");
                 
                 // Kodu Firebase'e yaz
                 var otpCevap = await _httpClient.PutAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.OTP_NODE}{anahtar}.json", content);
                 
                 if(otpCevap.IsSuccessStatusCode)
                 {
                      // Şifre sıfırlama kodu maili
                      return await AjansYonetim.Yardimcilar.EmailServisi.DogrulamaKoduGonderAsync(email, kod);
                 }
                 return false;
             }
             catch (Exception ex)
             {
                 App.HataKaydet(ex);
                 return false;
             }
        }

        /// <summary>
        /// E-posta doğrulandıktan sonra yeni parolayı Firebase'e (Agencies/{email}) kaydeder.
        /// </summary>
        public static async Task<bool> SifreYenileAsync(string email, string yeniParola)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 
                 // Mevcut Ajans kaydını okumamız gerekiyor ki diğer verileri kaybolmasın (Sadece Patch de atabilirdik ama Put atmak için tümünü indirmek garanti olur veya direkt ilgili düğümleri güncelleyebiliriz).
                 // Ancak hızlı ve güvenli çözüm: Sadece SifreHash ve SifreSalt özelliklerine PUT veya PATCH atmak.
                 
                 var hashSonuc = AjansYonetim.Yardimcilar.SifrelemeYardimcisi.SifreHashle(yeniParola);

                 // Sadece şifre bilgilerini güncelleyen Partial nesne
                 var sifreGuncelleme = new
                 {
                     SifreHash = hashSonuc.hash,
                     SifreSalt = hashSonuc.salt
                 };

                 var content = new StringContent(JsonSerializer.Serialize(sifreGuncelleme), System.Text.Encoding.UTF8, "application/json");
                 
                 // Sadece belirli property'leri değiştirmek için HttpMethod.Patch kullanılır. .NET'te HttpClient için Patch direkt yoksa HttpRequestMessage.
                 var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json")
                 {
                     Content = content
                 };

                 var response = await _httpClient.SendAsync(request);
                 return response.IsSuccessStatusCode;
             }
             catch (Exception ex)
             {
                 App.HataKaydet(ex);
                 return false;
             }
        }

        /// <summary>
        /// Admin Web Paneli'nden bu ajans hesabını kalıcı olarak silmiş mi?
        /// Eğer sunucu "null" dönerse hesap silinmiş demektir (false).
        /// Eğer internet yoksa veya ağ hatası varsa false-positive engellemek için geçici (true) dönülür.
        /// </summary>
        public static async Task<bool> HesapHalaVarMiAsync(string email)
        {
             try
             {
                 var anahtar = GuvenliEmailAnahtari(email);
                 var ajansCevap = await _httpClient.GetStringAsync($"{FirebaseSabitleri.BASE_URL}{FirebaseSabitleri.AGENCIES_NODE}{anahtar}.json");
                 
                 if (string.IsNullOrWhiteSpace(ajansCevap) || ajansCevap.Trim().ToLower() == "null")
                      return false; // HESAP SİLİNMİŞ (YOK EDİLMİŞ)
                 
                 return true;
             }
             catch (HttpRequestException)
             {
                 return true; // İnternet yoksa, adamı boş yere programdan atma, offline çalışabilsin
             }
             catch
             {
                 return true; 
             }
        }
    }
}
