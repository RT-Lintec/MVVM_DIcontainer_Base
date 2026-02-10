using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MVVM_Base.Common
{
    public static class HexCheckAssist
    {
        /// <summary>
        /// Attachedプロパティの設定 (Behaviorを付与するためのスイッチ)
        /// 本来WPFオブジェクトが持たないプロパティを付与して、カスタムすることが可能。
        /// RegisterAttachedによって、カスタムプロパティの情報(識別子、型、所属クラス)を登録する。
        /// App.xamlにある<Setter Property="local:HexCheckAssist.Enable" Value="True"/>のように記述して付与する。
        /// PropertyMetadataでは、
        /// 第一引数：基本はfalse。参照するオブジェクトが生成されるとtrueになる。
        /// 第二引数：このクラスに記述しているコールバック関数を指定する。第一引数が変化するたびに呼ばれる。(つまり一回)
        /// </summary>
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(HexCheckAssist),
                new PropertyMetadata(false, OnEnableChanged));

        /// <summary>
        /// プロパティ値のセット
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);
        
        /// <summary>
        /// プロパティ値のゲット
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

        /// <summary>
        /// ChangedTriggerプロパティ (VMと連動するためのプロパティ)
        /// 依存関係プロパティ(DependencyProperty)の設定はHexCheckBehavior参照
        /// </summary>
        public static readonly DependencyProperty ChangedTriggerProperty =
            DependencyProperty.RegisterAttached("ChangedTrigger", typeof(bool), typeof(HexCheckAssist),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// プロパティ値のセット
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetChangedTrigger(DependencyObject obj, bool value) => obj.SetValue(ChangedTriggerProperty, value);

        /// <summary>
        /// プロパティ値のゲット
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool GetChangedTrigger(DependencyObject obj) => (bool)obj.GetValue(ChangedTriggerProperty);

        /// <summary>
        /// コールバック関数
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // TextBox以外に付与されている場合は処理しない
            if (d is not TextBox tb || (bool)e.NewValue == false) return;

            // 保持しているビヘイビアを全て取得
            var behaviors = Microsoft.Xaml.Behaviors.Interaction.GetBehaviors(tb);

            // TextBoxが保持するビヘイビアにHexCheckBehaviorが存在するかチェック
            // ビヘイビアの多重登録を回避している
            if (!behaviors.OfType<HexCheckBehavior>().Any())
            {
                var behavior = new HexCheckBehavior();

                // Behaviorのプロパティと、この添付プロパティをバインド
                // SetBindingによって、直前にbehaviorに付与したHexCheckBehaviorのIsValueDifferentPropertyを
                // 通してIsValueDifferentに、Attach先のTextBox(Source)のもつChangedTriggerProperty(Path)
                // と同期(Mode : TwoWay)する設定、つまりバインドしている。
                // これによって、IsValueDifferent = !IsValueDifferent;という記述により
                // TextBox➡VMに届く仕組みが確立される。
                BindingOperations.SetBinding(behavior, HexCheckBehavior.IsValueDifferentProperty, new Binding
                {
                    Source = tb,
                    Path = new PropertyPath(HexCheckAssist.ChangedTriggerProperty),
                    Mode = BindingMode.TwoWay
                });

                behaviors.Add(behavior);
            }
        }
    }
}
