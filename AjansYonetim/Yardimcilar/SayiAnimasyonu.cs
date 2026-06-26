using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AjansYonetim.Yardimcilar
{
    /// <summary>
    /// Dashboard kartlarında count-up animasyonu sağlar.
    /// Sayı 0'dan hedef değere yavaşça artar.
    /// </summary>
    public static class SayiAnimasyonu
    {
        /// <summary>
        /// Animasyon süresi (milisaniye).
        /// </summary>
        private const int ToplamSureMs = 500;

        /// <summary>
        /// Animasyon adım aralığı (milisaniye).
        /// </summary>
        private const int AdimAraligi = 20;

        /// <summary>
        /// TextBlock'taki sayıyı animasyonlu şekilde günceller.
        /// </summary>
        public static void AnimasyonluGuncelle(TextBlock hedef, int hedefDeger, string format = "{0}")
        {
            var adimSayisi = ToplamSureMs / AdimAraligi;
            var mevcutAdim = 0;

            var zamanlayici = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AdimAraligi)
            };

            zamanlayici.Tick += (s, e) =>
            {
                mevcutAdim++;
                var oran = (double)mevcutAdim / adimSayisi;

                // Ease-out eğrisi
                var yumusatilmisOran = 1 - Math.Pow(1 - oran, 3);
                var guncelDeger = (int)(hedefDeger * yumusatilmisOran);

                hedef.Text = string.Format(format, guncelDeger);

                if (mevcutAdim >= adimSayisi)
                {
                    hedef.Text = string.Format(format, hedefDeger);
                    zamanlayici.Stop();
                }
            };

            zamanlayici.Start();
        }

        /// <summary>
        /// Para birimi ile animasyonlu güncelleme.
        /// </summary>
        public static void ParaAnimasyonluGuncelle(TextBlock hedef, decimal hedefDeger)
        {
            var adimSayisi = ToplamSureMs / AdimAraligi;
            var mevcutAdim = 0;

            var zamanlayici = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AdimAraligi)
            };

            zamanlayici.Tick += (s, e) =>
            {
                mevcutAdim++;
                var oran = (double)mevcutAdim / adimSayisi;
                var yumusatilmisOran = 1 - Math.Pow(1 - oran, 3);
                var guncelDeger = (decimal)(double.Parse(hedefDeger.ToString()) * yumusatilmisOran);

                hedef.Text = $"₺{guncelDeger:N0}";

                if (mevcutAdim >= adimSayisi)
                {
                    hedef.Text = $"₺{hedefDeger:N0}";
                    zamanlayici.Stop();
                }
            };

            zamanlayici.Start();
        }
    }
}
