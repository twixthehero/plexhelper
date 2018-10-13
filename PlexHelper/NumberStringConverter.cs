using System;
using Windows.UI.Xaml.Data;

namespace PlexHelper
{
	public class NumberStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return $"{value}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (int.TryParse((string)value, out int intValue))
			{
				return intValue;
			}

			return -1;
		}
	}
}
