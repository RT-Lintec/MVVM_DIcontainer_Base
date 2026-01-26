using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MVVM_Base.Common
{
    /// <summary>
    /// TextBox の入力を「半角数字のみに制限」する Behavior。
    /// 全角文字・空白・日本語IME入力もすべて禁止。
    /// 最大値チェック付き。
    /// </summary>
    public class PositiveIntegerBehavior : Behavior<TextBox>
    {
        public int MaxValue
        {
            get => (int)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(
                nameof(MaxValue),
                typeof(int),
                typeof(PositiveIntegerBehavior),
                new PropertyMetadata(0)
            );

        protected override void OnAttached()
        {
            base.OnAttached();

            // IME を完全無効化（全角入力確定を禁止）
            InputMethod.SetIsInputMethodEnabled(AssociatedObject, false);

            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(AssociatedObject, OnPaste);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
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

            // Backspaceが押された
            if (e.Key == Key.Back)
            {
                var tb = AssociatedObject;
                // 選択テキストの前後取得 されてなければどちらも値無し
                string before = tb.Text[..tb.SelectionStart];
                string after = tb.Text[(tb.SelectionStart + tb.SelectionLength)..];

                // 選択テキストを取得
                var start = tb.SelectionStart; // 選択開始インデクス
                var length = tb.SelectionLength; // 選択文字数
                var end = start + length; // 選択終了インデクス

                // 文字が無い状態での削除は無効
                if (start == 0 && end == 0)
                {
                    e.Handled = true;
                    return;
                }
                string selected = AssociatedObject.SelectedText;

                // 選択テキストが無い状態のデリート
                if (selected == "")
                {
                    string newText = tb.Text.Substring(0, start-1) + tb.Text.Substring(end);

                    // 0は許容する。0からから始まる0以上の数値は禁止する。
                    if (newText != "0" && newText.StartsWith("0"))
                    {
                        tb.Text = "0";

                        // キャレットを先頭へ
                        tb.CaretIndex = 0;
                        e.Handled = true;
                        return;
                    }
                }
                // 選択テキストが有る状態のデリート
                else
                {
                    string newText = tb.Text.Substring(0,start) + tb.Text.Substring(end);

                    // 0は許容する。0からから始まる0以上の数値は禁止する。
                    if (newText != "0" && newText.StartsWith("0"))
                    {
                        tb.Text = "0";

                        // キャレットを先頭へ
                        tb.CaretIndex = 0;
                        e.Handled = true;
                        return;
                    }
                }
            }

            // deleteが押された
            if (e.Key == Key.Delete)
            {
                var tb = AssociatedObject;
                // 選択テキストの前後取得 されてなければどちらも値無し
                string before = tb.Text[..tb.SelectionStart];
                string after = tb.Text[(tb.SelectionStart + tb.SelectionLength)..];

                // 選択テキストを取得
                var start = tb.SelectionStart; // 選択開始インデクス
                var length = tb.SelectionLength; // 選択文字数
                var end = start + length; // 選択終了インデクス

                string selected = AssociatedObject.SelectedText;

                // 選択テキストが無い状態のデリート
                if (selected == "")
                {
                    // 末尾デリートは無効
                    if (end == tb.Text.Length)
                    {
                        e.Handled = true;
                        return;
                    }

                    string newText = tb.Text.Substring(0, start) + tb.Text.Substring(end + 1, tb.Text.Length - (start + 1));

                    // 0は許容する。0からから始まる0以上の数値は禁止する。
                    if (newText != "0" && newText.StartsWith("0"))
                    {
                        tb.Text = "0";

                        // キャレットを先頭へ
                        tb.CaretIndex = 0;
                        e.Handled = true;
                        return;
                    }
                }
                // 選択テキストが有る状態のデリート
                else
                {
                    string newText = tb.Text.Substring(0, start) + tb.Text.Substring(end);

                    // 0は許容する。0からから始まる0以上の数値は禁止する。
                    if (newText != "0" && newText.StartsWith("0"))
                    {
                        tb.Text = "0";

                        // キャレットを先頭へ
                        tb.CaretIndex = 0;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// テキスト入力時のフィルタリング
        /// 半角数字以外はすべて禁止（全角数字も弾く）
        /// </summary>
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 半角数字のみ許可
            if (!e.Text.All(IsHalfWidthDigit))
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
                    e.Handled = true;
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
                    e.Handled = true;
                }
            }

            if (int.TryParse(newText, out int value))
            {
                if (value > MaxValue)
                {
                    tb.Text = MaxValue.ToString();
                    e.Handled = true;
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
                if (int.TryParse(pasteText, out int v))
                {
                    if (v > MaxValue)
                    {
                        e.CancelCommand();
                        tb.Text = MaxValue.ToString();

                        // キャレットを末尾へ
                        tb.CaretIndex = tb.Text.Length;
                    }
                }

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
                        if(tex != '0')
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
                        foreach(char tex in newText)
                        {
                            if(tex == '0')
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
                if (int.TryParse(newText, out int value))
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
