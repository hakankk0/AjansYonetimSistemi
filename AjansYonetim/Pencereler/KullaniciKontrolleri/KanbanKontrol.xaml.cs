using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AjansYonetim.Modeller;
using AjansYonetim.Sabitler;
using AjansYonetim.Veritabani;

namespace AjansYonetim.Pencereler.KullaniciKontrolleri
{
    public partial class KanbanKontrol : UserControl
    {
        public event EventHandler? KanbanGuncellendi;

        public KanbanKontrol()
        {
            InitializeComponent();
        }

        public async Task YukleAsync()
        {
            await KanbanVerileriniYukleAsync();
        }

        private async Task KanbanVerileriniYukleAsync()
        {
            var projeler = await Task.Run(() => ProjeIslemleri.TumProjeleriGetir());
            lstKanbanGorevAtandi.ItemsSource = projeler.Where(p => p.Durum == ProjeDurumlari.GOREV_ATANDI).ToList();
            lstKanbanDevamEdiyor.ItemsSource = projeler.Where(p => p.Durum == ProjeDurumlari.DEVAM_EDIYOR).ToList();
            lstKanbanTeslimEdildi.ItemsSource = projeler.Where(p => p.Durum == ProjeDurumlari.TESLIM_EDILDI).ToList();
            lstKanbanTamamlandi.ItemsSource = projeler.Where(p => p.Durum == ProjeDurumlari.TAMAMLANDI).ToList();
        }

        private async void KanbanProjeDetay(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox lstBox && lstBox.SelectedItem is Proje secilenProje)
            {
                var detayPenceresi = new ProjeDetayPenceresi(secilenProje) { Owner = Window.GetWindow(this) };
                detayPenceresi.ShowDialog();
                
                await KanbanVerileriniYukleAsync();
                KanbanGuncellendi?.Invoke(this, EventArgs.Empty);
            }
        }

        // ---------- KANBAN DRAG & DROP İŞLEMLERİ ----------
        private Point _dragBaslangicNoktasi;
        private Window? _hayaletPencere;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Win32Point pt);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct Win32Point { public int X; public int Y; }

        private void Kanban_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragBaslangicNoktasi = e.GetPosition(null);
        }

        private void Kanban_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is ListBox listBox)
            {
                var suAnkiPozisyon = e.GetPosition(null);
                Vector fark = _dragBaslangicNoktasi - suAnkiPozisyon;

                if (Math.Abs(fark.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(fark.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var dependencyObject = (System.Windows.DependencyObject)e.OriginalSource;
                    while (dependencyObject != null && !(dependencyObject is System.Windows.Controls.ListBoxItem))
                    {
                        dependencyObject = System.Windows.Media.VisualTreeHelper.GetParent(dependencyObject);
                    }

                    if (dependencyObject is System.Windows.Controls.ListBoxItem secilenItem)
                    {
                        var secilenProje = secilenItem.DataContext as Proje;
                        if (secilenProje != null)
                        {
                            var dataObj = new DataObject("ProjeFormat", secilenProje);

                            var brush = new System.Windows.Media.VisualBrush(secilenItem) { Opacity = 0.7 };
                            _hayaletPencere = new Window
                            {
                                WindowStyle = WindowStyle.None,
                                AllowsTransparency = true,
                                AllowDrop = false,
                                Background = System.Windows.Media.Brushes.Transparent,
                                IsHitTestVisible = false,
                                SizeToContent = SizeToContent.WidthAndHeight,
                                Topmost = true,
                                ShowInTaskbar = false,
                                Content = new System.Windows.Shapes.Rectangle
                                {
                                    Width = secilenItem.RenderSize.Width,
                                    Height = secilenItem.RenderSize.Height,
                                    Fill = brush
                                }
                            };

                            Win32Point pt = new Win32Point();
                            GetCursorPos(ref pt);
                            _hayaletPencere.Left = pt.X + 15;
                            _hayaletPencere.Top = pt.Y + 15;
                            _hayaletPencere.Show();

                            DragDrop.DoDragDrop(listBox, dataObj, DragDropEffects.Move);

                            if (_hayaletPencere != null)
                            {
                                _hayaletPencere.Close();
                                _hayaletPencere = null;
                            }
                        }
                    }
                }
            }
        }

        private void Kanban_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (_hayaletPencere != null)
            {
                Win32Point pt = new Win32Point();
                GetCursorPos(ref pt);
                _hayaletPencere.Left = pt.X + 15;
                _hayaletPencere.Top = pt.Y + 15;
            }
        }

        private async void Kanban_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ProjeFormat") && sender is ListBox hedefListBox)
            {
                var tasinanProje = e.Data.GetData("ProjeFormat") as Proje;
                if (tasinanProje == null) return;

                string yeniDurum = ProjeDurumlari.GOREV_ATANDI;
                switch (hedefListBox.Name)
                {
                    case "lstKanbanGorevAtandi": yeniDurum = ProjeDurumlari.GOREV_ATANDI; break;
                    case "lstKanbanDevamEdiyor": yeniDurum = ProjeDurumlari.DEVAM_EDIYOR; break;
                    case "lstKanbanTeslimEdildi": yeniDurum = ProjeDurumlari.TESLIM_EDILDI; break;
                    case "lstKanbanTamamlandi": yeniDurum = ProjeDurumlari.TAMAMLANDI; break;
                }

                if (tasinanProje.Durum != yeniDurum)
                {
                    var eskiDurumYazi = tasinanProje.Durum;
                    tasinanProje.Durum = yeniDurum;

                    if (yeniDurum == ProjeDurumlari.TAMAMLANDI)
                    {
                        tasinanProje.TamamlanmaYuzdesi = 100;
                    }

                    ProjeIslemleri.ProjeGuncelle(tasinanProje, eskiDurumYazi);
                    AktiviteIslemleri.AktiviteEkle($"'{tasinanProje.ProjeAdi}' projesi '{yeniDurum}' durumuna getirildi.", "\uE895");

                    await KanbanVerileriniYukleAsync();
                    KanbanGuncellendi?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
