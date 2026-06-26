using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler.KullaniciKontrolleri
{
    public partial class HareketlerKontrol : UserControl
    {
        public HareketlerKontrol()
        {
            InitializeComponent();
        }

        public async Task YukleAsync()
        {
            var aktiviteler = await Task.Run(() => AktiviteIslemleri.SonAktiviteleriGetir(300));
            dgHareketler.ItemsSource = aktiviteler;
        }

        private async void HareketleriYenileTiklandi(object sender, RoutedEventArgs e)
        {
            await YukleAsync();
        }
    }
}
