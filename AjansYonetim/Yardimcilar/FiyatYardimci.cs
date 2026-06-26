using System.Globalization;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Para birimi değerlerini güvenli parse eden yardımcı sınıf.
    /// Hem Türkçe (7.000,00) hem İngilizce (7000.00) hem sade (7000) formatları destekler.
    /// </summary>
    public static class FiyatYardimci
    {
        /// <summary>
        /// Fiyat metnini decimal'e çevirir.
        /// </summary>
        /// <param name="metin">Fiyat metni.</param>
        /// <param name="sonuc">Parse edilen decimal değer.</param>
        /// <returns>Başarılı ise true.</returns>
        public static bool Parse(string metin, out decimal sonuc)
        {
            sonuc = 0;
            if (string.IsNullOrWhiteSpace(metin)) return false;

            var temiz = metin.Trim();

            // Hem nokta hem virgül varsa → Türkçe format: 7.000,00
            // Virgülden sonra nokta geliyorsa → İngilizce: 7,000.00
            var sonNokta = temiz.LastIndexOf('.');
            var sonVirgul = temiz.LastIndexOf(',');

            if (sonNokta >= 0 && sonVirgul >= 0)
            {
                if (sonVirgul > sonNokta)
                {
                    // Türkçe: 7.000,00 → nokta binlik, virgül ondalık
                    temiz = temiz.Replace(".", "").Replace(",", ".");
                }
                else
                {
                    // İngilizce: 7,000.00 → virgül binlik, nokta ondalık
                    temiz = temiz.Replace(",", "");
                }
            }
            else if (sonVirgul >= 0)
            {
                // Sadece virgül var: 7000,00 → virgül ondalık
                temiz = temiz.Replace(",", ".");
            }
            // Sadece nokta veya hiç ayırıcı yok → doğrudan parse

            return decimal.TryParse(temiz,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out sonuc) && sonuc >= 0;
        }

        /// <summary>
        /// Decimal değeri ekranda göstermek için güvenli formata çevirir.
        /// Binlik ayırıcı kullanmaz, nokta ile ondalık.
        /// </summary>
        public static string Formatla(decimal deger)
        {
            return deger.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
