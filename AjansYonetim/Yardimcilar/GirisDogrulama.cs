using System.Windows.Input;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Kullanıcı girişi doğrulama yardımcı sınıfı.
    /// Tekrarlanan input validasyon mantığını merkezileştirir.
    /// </summary>
    public static class GirisDogrulama
    {
        /// <summary>
        /// Sadece rakam, virgül ve nokta girilmesine izin verir.
        /// Fiyat/tutar alanları için kullanılır.
        /// </summary>
        public static void SadeceParaKarakteri(TextCompositionEventArgs e)
        {
            foreach (char karakter in e.Text)
            {
                if (!char.IsDigit(karakter) && karakter != ',' && karakter != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Sadece rakam girilmesine izin verir.
        /// Telefon, sayısal ID ve eşik değer alanları için kullanılır.
        /// </summary>
        public static void SadeceRakam(TextCompositionEventArgs e)
        {
            foreach (char karakter in e.Text)
            {
                if (!char.IsDigit(karakter))
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}
