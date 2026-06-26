using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace AjansYonetim.Yardimcilar
{
    public static class SessionYonetimi
    {
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public int dwTime;
        }

        private static DispatcherTimer _zamanlayici;
        public static event Action OturumZamanAsimiOldu;
        private static int _zamanAsimiDakika = 30;

        public static void Baslat(int zamanAsimiDakika = 30)
        {
            _zamanAsimiDakika = zamanAsimiDakika;
            if (_zamanlayici == null)
            {
                _zamanlayici = new DispatcherTimer();
                _zamanlayici.Interval = TimeSpan.FromSeconds(10);
                _zamanlayici.Tick += Zamanlayici_Tick;
            }
            _zamanlayici.Start();
        }

        public static void Durdur()
        {
            _zamanlayici?.Stop();
        }

        private static void Zamanlayici_Tick(object? sender, EventArgs e)
        {
            var lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);

            if (GetLastInputInfo(ref lii))
            {
                var sonIslemZamani = lii.dwTime;
                var suAn = Environment.TickCount;
                
                // TickCount overflow handler (- to + transition safe check)
                long bostaKalinanMilisaniye = (long)suAn - (long)sonIslemZamani;
                if (bostaKalinanMilisaniye < 0) 
                     bostaKalinanMilisaniye += uint.MaxValue;

                var bostaKalinanDakika = TimeSpan.FromMilliseconds(bostaKalinanMilisaniye).TotalMinutes;

                if (bostaKalinanDakika >= _zamanAsimiDakika)
                {
                    Durdur(); 
                    OturumZamanAsimiOldu?.Invoke();
                }
            }
        }
    }
}
