using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace MVVM_Base.Common
{
    public class HexCheckBehavior : Behavior<TextBox>
    {
        /// <summary>
        /// ViewModelとのバインド設定を行っている。
        /// 
        /// 依存関係プロパティ(DependencyProperty)の設定
        /// Registerで紐付いたプロパティ(IsValueDifferent)の情報(識別子、型、所属クラス)を登録している。
        /// FrameworkPropertyMetadataとは、このプロパティのWPFでの振る舞いについて設定をまとめている。
        /// 第一引数：プロパティの初期値
        /// 第二引数：バインディング挙動 TwoWayはView⇔VMが常に同期する。OnewayはVM➡View
        /// </summary>
        public static readonly DependencyProperty IsValueDifferentProperty =
                DependencyProperty.Register(nameof(IsValueDifferent), typeof(bool), typeof(HexCheckBehavior),
                    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// セルの値が変わったかどうかを表すプロパティ
        /// </summary>
        public bool IsValueDifferent
        {
            get => (bool)GetValue(IsValueDifferentProperty);
            set => SetValue(IsValueDifferentProperty, value);
        }

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

            // 後述のBindingExpressionにおけるIsDirtyをtrueに変える。
            // IsDirty == true の場合、WPF（DataGrid）は
            // ChangedTrigger（Target）とIsModified（Source）が未同期と判断する。
            //
            // expr?.UpdateSource(); により明示的にSourceを更新すると
            // IsDirty は false（同期済み）に変わる。
            //
            // これを行わない場合、UpdateSourceTrigger = Explicitであっても、
            // DataGridがLostFocus / CommitEdit 等の内部処理でUpdateSourceを実行し、
            // Binding先のViewModel.IsModifiedに通知が飛ぶことがある。
            //
            // 明示的に更新することで IsDirty = false（同期済み）となり、
            // DataGrid は「再同期の必要なし」と判断するため、LostFocus時に通知が飛ばなくなる。
            IsValueDifferent = !IsValueDifferent;
            oldText = tb.Text?.Trim();

            // BindingExpression(Bindingの実体)を取得する
            // このケースでの対象はTextBox tbのHexCheckAssist.ChangedTriggerPropertyproperty
            // に紐づいている実体：
            // (ChangedTrigger（Target）とIsModified（Source）を結び付けているBinding のランタイムオブジェクト)
            var expr = BindingOperations.GetBindingExpression(
                        tb,
                        HexCheckAssist.ChangedTriggerProperty);

            // IsModified（Source）を明示的に更新する。UpdateSourceTrigger.Explicitに対する処理。
            expr?.UpdateSource();

            return;
        }

        private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            string text = tb.Text?.Trim();

            if (text == oldText)
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                tb.Text = oldText;
                return;
            }

            // 大文字化
            text = text.ToUpperInvariant();

            // 16進 00〜FF 判定
            if (byte.TryParse(text, NumberStyles.HexNumber,
                              CultureInfo.InvariantCulture, out _))
            {
                if (text.Length == 1)
                {
                    text = "0" + text;
                }
                tb.Text = text;
                //IsValueDifferent = !IsValueDifferent;
            }
            else
            {
                tb.Text = oldText;
            }            
        }
    }
}
