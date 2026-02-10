using Microsoft.Xaml.Behaviors;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MVVM_Base.Common
{
    /// <summary>
    /// TextBox の入力を「半角数字のみに制限」する浮動小数点数向けBehavior。
    /// 全角文字・空白・日本語IME入力もすべて禁止。
    /// 最大値チェック付き。
    /// </summary>
    public class FpNumberBehavior : Behavior<TextBox>
    {
        /// <summary>
        /// 最大値
        /// </summary>
        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register(
            nameof(MaxValue),
            typeof(double),
            typeof(FpNumberBehavior),
            new PropertyMetadata(0d)
        );

        /// <summary>
        /// 最小値
        /// </summary>
        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register(
            nameof(MinValue),
            typeof(double),
            typeof(FpNumberBehavior),
            new PropertyMetadata(0d)
        );

        protected override void OnAttached()
        {
            base.OnAttached();

            // IME を完全無効化（全角入力確定を禁止）
            InputMethod.SetIsInputMethodEnabled(AssociatedObject, false);

            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            AssociatedObject.LostFocus += AssociatedObject_LostFocus;
            DataObject.AddPastingHandler(AssociatedObject, OnPaste);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            AssociatedObject.LostFocus -= AssociatedObject_LostFocus;
            DataObject.RemovePastingHandler(AssociatedObject, OnPaste);
        }

        /// <summary>
        /// 印字不可文字やスペースをブロックする
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 半角スペース、全角スペース、タブ禁止
            if (e.Key == Key.Space || e.Key == Key.Tab)
            {
                e.Handled = true;
                return;
            }

            // IMEからの全角スペースなどを検出
            if (e.Key == Key.ImeProcessed)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// テキスト入力時のフィルタリング
        /// 半角数字以外はすべて禁止（全角数字も弾く）
        /// </summary>
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 半角数字のみ許可
            if (!e.Text.All(c => IsHalfWidthDigit(c) || c == '.'))
            {
                e.Handled = true;
                return;
            }

            var tb = AssociatedObject;

            // 入力後のテキストを仮想的に生成
            string newText =
                tb.Text[..tb.SelectionStart] +
                e.Text +
                tb.Text[(tb.SelectionStart + tb.SelectionLength)..];

            // ピリオドが二つ以上含まれていないか判定
            if (newText.Where(x => x == '.').Count() > 1)
            {
                e.Handled = true;
                return;
            }

            if (double.TryParse(newText, out double value))
            {
                if (value > MaxValue)
                {
                    tb.Text = MaxValue.ToString();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// フォーカスロスト時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObject_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            // フォーカスが外れたときに行いたい処理
            var textBox = (TextBox)sender;
            string text = textBox.Text;

            bool isNeedDelete = true;

            // 空欄チェック
            if (text == "")
            {
                textBox.Text = MinValue.ToString();
                return;
            }

            if (text != "0" && text.StartsWith("0"))
            {
                bool isAllZero = true;
                foreach (char tex in text)
                {
                    if (tex != '0')
                    {
                        isAllZero = false;
                        break;
                    }
                }

                // 全て0なら0に書き変え
                if (isAllZero)
                {
                    textBox.Text = "0";
                    e.Handled = true;
                    return;
                }                
                else
                {
                    // ピリオド前に複数文字列がありそれらが全て0の場合、0.の形に直す
                    int zeroIndex = 0;
                    while (true)
                    {
                        if (text[zeroIndex] == '0')
                        {
                            zeroIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // ピリオドの位置とzeroIndexの位置が同じならば全て0である
                    if (zeroIndex > 0)
                    {
                        for (int i = 0; i < zeroIndex; i++)
                        {
                            text = text.Substring(1, text.Length - 1);
                        }

                        textBox.Text = text;
                    }

                    if (text.Contains("."))
                    {
                        // ピリオド後に複数文字列がありそれらが全て0の場合、.0の形に直す
                        zeroIndex = text.Length - 1;
                        while (true)
                        {
                            if (text[zeroIndex] == '0')
                            {
                                zeroIndex--;
                            }
                            else
                            {
                                break;
                            }
                        }

                        // ピリオドの位置とzeroIndexの位置が同じならば全て0である
                        if (zeroIndex > 0)
                        {
                            text = text.Substring(0, zeroIndex + 1);
                            textBox.Text = text;
                        }
                    }
                }
            }
            else
            {
                if (!text.Contains("."))
                {

                }
                else
                {

                    // ピリオド後に複数文字列がありそれらが全て0の場合、.0の形に直す
                    var zeroIndex = text.Length - 1;
                    while (true)
                    {
                        if (text[zeroIndex] == '0')
                        {
                            zeroIndex--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // ピリオドの位置とzeroIndexの位置が同じならば全て0である
                    if (zeroIndex > 0)
                    {
                        text = text.Substring(0, zeroIndex + 1);
                        textBox.Text = text;
                    }
                }
            }

            // 末尾がピリオドで終わっている場合
            if (text.EndsWith('.'))
            {
                // 不正な小数の例：末尾ピリオドを削除
                textBox.Text = text.Trim('.');
                //return;
            }

            // . ピリオドから始まる場合は、先頭に0を追加して終了
            if (text.StartsWith('.'))
            {
                textBox.Text = '0' + text;
                //return;
            }

            // 最大値・最小値チェック
            if (int.TryParse(text, out int value))
            {
                if (value > MaxValue)
                {
                    textBox.Text = MaxValue.ToString();
                    return;
                }
                else if (value < MinValue)
                {
                    textBox.Text = MinValue.ToString();
                    return;
                }
            }

        }

        /// <summary>
        /// 半角数字かどうか判定（全角数字は false）
        /// </summary>
        private bool IsHalfWidthDigit(char c)
            => c >= '0' && c <= '9';

        /// <summary>
        /// 貼り付け時のチェック（全角文字含む場合は全て禁止）
        /// </summary>
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pasteText = (string)e.DataObject.GetData(typeof(string));

                // 半角数字のみ許可（全角を含む場合も false）
                if (!pasteText.All(IsHalfWidthDigit))
                {
                    e.CancelCommand();
                    return;
                }

                var tb = AssociatedObject;

                // 最大値チェック
                //if (double.TryParse(pasteText, out double v))
                //{
                //    if (v > MaxValue)
                //    {
                //        e.CancelCommand();
                //        tb.Text = MaxValue.ToString();

                //        // キャレットを末尾へ
                //        tb.CaretIndex = tb.Text.Length;
                //    }
                //}

                // 選択テキストを取得
                string selected = AssociatedObject.SelectedText;

                // 入力箇所を取得
                int index = tb.SelectionStart;

                // 選択テキストの前後取得 されてなければどちらも値無し
                string before = tb.Text[..tb.SelectionStart];
                string after = tb.Text[(tb.SelectionStart + tb.SelectionLength)..];
                string newText = before + pasteText + after;

                // 0は許容する。0からから始まる0以上の数値は禁止する。
                if (newText != "0" && newText.StartsWith("0"))
                {
                    bool isAllZero = true;
                    foreach (char tex in newText)
                    {
                        if (tex != '0')
                        {
                            isAllZero = false;
                            break;
                        }
                    }

                    // 全て0なら0に書き変え
                    if (isAllZero)
                    {
                        tb.Text = "0";
                        e.CancelCommand();
                        return;
                    }
                    // 全て0でなければ先頭から0以外の数字が見つかるまで0を消す
                    else
                    {
                        foreach (char tex in newText)
                        {
                            if (tex == '0')
                            {
                                newText = newText.Substring(1, newText.Length - 1);
                            }
                            else
                            {
                                break;
                            }
                        }
                        tb.Text = newText;
                    }

                    e.CancelCommand();
                    return;
                }

                // 最大値を超えていないかチェック
                if (double.TryParse(newText, out double value))
                {
                    if (value > MaxValue)
                    {
                        e.CancelCommand();
                        tb.Text = MaxValue.ToString();
                        tb.CaretIndex = tb.Text.Length;
                        return;
                    }
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
