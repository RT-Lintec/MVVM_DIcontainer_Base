using System;
using System.Globalization;
using System.Windows.Data;

namespace MVVM_Base.Common
{
    public class StringToBooleanConverter : IValueConverter
    {
        // ViewModel -> View
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        // View -> ViewModel
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return parameter.ToString();
            }
            // falseのときはVMを変えない
            return Binding.DoNothing;
        }
    }
}
