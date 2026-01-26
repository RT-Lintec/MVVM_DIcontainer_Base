using Microsoft.Xaml.Behaviors;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace MVVM_Base.Common
{
    public class HexCheckBehavior : Behavior<TextBox>
    {
        string oldText = "";

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.GotFocus += HexTextBox_Focus;
            AssociatedObject.LostFocus += HexTextBox_LostFocus;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= HexTextBox_Focus;
            AssociatedObject.LostFocus -= HexTextBox_LostFocus;
            base.OnDetaching();
        }

        private void HexTextBox_Focus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            oldText = tb.Text?.Trim();
            return;
        }

        private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            string text = tb.Text?.Trim();
            

            if (string.IsNullOrEmpty(text))
            {
                tb.Text = "";
                return;
            }

            // 大文字化
            text = text.ToUpperInvariant();

            // 16進 00〜FF 判定
            if (byte.TryParse(text, NumberStyles.HexNumber,
                              CultureInfo.InvariantCulture, out _))
            {
                tb.Text = text;
            }
            else
            {
                tb.Text = oldText;
            }
        }
    }
}
