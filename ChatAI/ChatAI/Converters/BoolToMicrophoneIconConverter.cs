using System;
using System.Globalization;
using System.Windows.Data;

namespace ChatAI
{
    public class BoolToMicrophoneIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isRecording = (bool)value;
            return isRecording ? "Images/mute.png" : "Images/microphone.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 