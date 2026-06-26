using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Kullanıcılara OTP (Tek Kullanımlık Kod) ve diğer bildirimleri göndermek için e-posta servisi.
    /// GMAIL SMTP altyapısını kullanır.
    /// </summary>
    public static class EmailServisi
    {
        // Sabit gönderici bilgileri
        private const string GONDEREN_EMAIL = "ajansyonetimsistemi@gmail.com";
        private const string GONDEREN_SIFRE = "qwoqbnavhrrfupxc"; // Google App Password
        private const string SMTP_HOST = "smtp.gmail.com";
        private const int SMTP_PORT = 587; // TLS için port numarası

        /// <summary>
        /// Belirtilen e-posta adresine 6 haneli doğrulama kodunu şık bir formatta gönderir.
        /// </summary>
        /// <param name="aliciEmail">Kodun gönderileceği e-posta adresi.</param>
        /// <param name="doğrulamaKodu">Gönderilecek 6 haneli kod.</param>
        /// <returns>Gönderim başarılıysa true, başarısızsa false döner.</returns>
        public static async Task<bool> DogrulamaKoduGonderAsync(string aliciEmail, string dogrulamaKodu)
        {
            try
            {
                using var mesaj = new MailMessage();
                mesaj.From = new MailAddress(GONDEREN_EMAIL, "Proje Yöneticim");
                mesaj.To.Add(new MailAddress(aliciEmail));
                
                mesaj.Subject = "Giriş İçin Doğrulama Kodunuz";
                
                // Şık ve kurumsal duran bir HTML e-posta tasarımı
                mesaj.IsBodyHtml = true;
                mesaj.Body = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f6f8; padding: 20px; text-align: center; border-radius: 8px;'>
                        <h2 style='color: #2A2A3E;'>Giriş Talebiniz Alındı</h2>
                        <p style='color: #555; font-size: 16px; margin-bottom: 30px;'>Uygulamaya giriş yapmak için aşağıdaki 6 haneli tek kullanımlık şifreyi giriniz:</p>
                        
                        <div style='background-color: #ffffff; padding: 15px 30px; display: inline-block; border-radius: 8px; border: 2px solid #7C3AED; font-size: 28px; font-weight: bold; letter-spacing: 4px; color: #7C3AED;'>
                            {dogrulamaKodu}
                        </div>
                        
                        <p style='color: #888; font-size: 14px; margin-top: 30px;'>Bu kodun geçerlilik süresi 3 dakikadır. Eğer bu işlemi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                    </div>";

                using var smtp = new SmtpClient(SMTP_HOST, SMTP_PORT);
                smtp.Credentials = new NetworkCredential(GONDEREN_EMAIL, GONDEREN_SIFRE);
                smtp.EnableSsl = true; // Güvenli bağlantı

                await smtp.SendMailAsync(mesaj);
                return true;
            }
            catch (Exception ex)
            {
                // Hata bilgisini global loga kaydet
                App.HataKaydet(ex);
                return false;
            }
        }
    }
}
