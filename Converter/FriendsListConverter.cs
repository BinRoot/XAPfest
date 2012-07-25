using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SmashSampleApp.Converter
{
    public class FriendsListConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string status = (string)value;
            string tType = targetType.Name;

            if (targetType.Name == "ImageSource")
            {
                if (status == "yes") return "/SmashSampleApp;component/Images/Icons/check_icon.png";
                else return "/SmashSampleApp;component/Images/Icons/cross_icon.png";

            }
            else
            {
                return null;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
