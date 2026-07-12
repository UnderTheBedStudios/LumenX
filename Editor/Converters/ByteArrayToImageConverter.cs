using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace LumenX.Converters;
public class ByteArrayToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return new Bitmap(stream); // Avalonia.Media.Imaging.Bitmap
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
