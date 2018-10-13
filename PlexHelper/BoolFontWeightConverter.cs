using System;
using Windows.UI.Text;
using Windows.UI.Xaml.Data;

namespace PlexHelper
{
	public class BoolFontWeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (bool)value ? FontWeights.Bold : FontWeights.Normal;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
