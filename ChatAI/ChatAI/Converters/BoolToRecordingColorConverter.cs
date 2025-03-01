using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ChatAI
{
    public class BoolToRecordingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isRecording = (bool)value;
            return new SolidColorBrush(Color.FromRgb(
                isRecording ? (byte)234 : (byte)109,
                isRecording ? (byte)66 : (byte)66,
                isRecording ? (byte)66 : (byte)234));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 