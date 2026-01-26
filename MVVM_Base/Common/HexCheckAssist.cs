using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows;

namespace MVVM_Base.Common
{
    public static class HexCheckAssist
    {
        public static bool GetEnable(DependencyObject obj)
            => (bool)obj.GetValue(EnableProperty);

        public static void SetEnable(DependencyObject obj, bool value)
            => obj.SetValue(EnableProperty, value);

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(HexCheckAssist),
                new PropertyMetadata(false, OnEnableChanged));

        private static void OnEnableChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb || (bool)e.NewValue == false)
                return;

            var behaviors = Interaction.GetBehaviors(tb);

            if (!behaviors.OfType<HexCheckBehavior>().Any())
            {
                behaviors.Add(new HexCheckBehavior
                {
                });
            }
        }
    }

}
