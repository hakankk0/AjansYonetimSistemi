using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Donusturuculer
{
    /// <summary>
    /// Teslim tarihine göre satır arka plan rengini belirleyen dönüştürücü.
    /// Performans için DB sorgusunu önbelleğe alır.
    /// </summary>
    public class TeslimTarihiRenkDonusturucu : IValueConverter
    {
        /// <summary>
        /// Varsayılan acil gün eşiği.
        /// </summary>
        private const int VarsayilanAcilGunEsigi = 2;

        /// <summary>
        /// Önbellek geçerlilik süresi (saniye).
        /// </summary>
        private const int OnbellekSuresiSaniye = 60;

        /// <summary>
        /// Önbellekteki acil gün eşiği değeri.
        /// </summary>
        private static int _onbellekEsik = VarsayilanAcilGunEsigi;

        /// <summary>
        /// Son DB okumasının zamanı.
        /// </summary>
        private static DateTime _sonOkumaZamani = DateTime.MinValue;

        /// <summary>
        /// Acil projeler için açık kırmızı arka plan rengi.
        /// </summary>
        private static readonly SolidColorBrush AcilArkaPlanRengi =
            new SolidColorBrush(Color.FromRgb(255, 200, 200));

        /// <summary>
        /// Normal projeler için şeffaf arka plan.
        /// </summary>
        private static readonly SolidColorBrush NormalArkaPlanRengi =
            new SolidColorBrush(Colors.Transparent);

        /// <summary>
        /// Önbellekten veya DB'den acil gün eşiğini döndürür.
        /// </summary>
        private static int AcilGunEsiginiGetir()
        {
            if ((DateTime.Now - _sonOkumaZamani).TotalSeconds < OnbellekSuresiSaniye)
            {
                return _onbellekEsik;
            }

            var esikMetni = AyarIslemleri.AyarGetir(
                AyarIslemleri.ANAHTAR_ACIL_GUN_ESIGI,
                VarsayilanAcilGunEsigi.ToString());

            _onbellekEsik = int.TryParse(esikMetni, out var esik) ? esik : VarsayilanAcilGunEsigi;
            _sonOkumaZamani = DateTime.Now;

            return _onbellekEsik;
        }

        /// <summary>
        /// Önbelleği temizler (ayarlar değiştiğinde çağrılır).
        /// </summary>
        public static void OnbellegiTemizle()
        {
            _sonOkumaZamani = DateTime.MinValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime teslimTarihi)
            {
                var acilGunEsigi = AcilGunEsiginiGetir();
                var kalanGun = (teslimTarihi - DateTime.Now).TotalDays;

                if (kalanGun <= acilGunEsigi)
                {
                    return AcilArkaPlanRengi;
                }
            }

            return NormalArkaPlanRengi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
