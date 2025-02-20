using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChatAI.Converters
{
    public class BoolToMessageTailConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value)
			{
				// Pico a la derecha del mensaje
				return "M 0,10 L 10,0 L 10,20 Z";
			}
			else
			{
				// Pico a la izquierda del mensaje
				return "M 10,10 L 0,0 L 0,20 Z";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
    }
}
