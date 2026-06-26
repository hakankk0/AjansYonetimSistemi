using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AjansYonetim.Donusturuculer
{
    /// <summary>
    /// Metin null veya boş ise Collapsed, dolu ise Visible döndüren dönüştürücü.
    /// Görev açıklaması ve çalışan adı gibi opsiyonel alanların gösterilip gizlenmesi için kullanılır.
    /// </summary>
    public class MetinGorunurlukDonusturucu : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
