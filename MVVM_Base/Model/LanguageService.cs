using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MVVM_Base.Model
{
    /// <summary>
    /// 言語タイプ
    /// </summary>
    public enum LanguageType
    {
        Japanese,
        English
    }

    public class LanguageService : INotifyPropertyChanged
    {
        private LanguageType currentLanguage = LanguageType.Japanese;
        public LanguageType CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                if (currentLanguage != value)
                {
                    currentLanguage = value;
                    //OnPropertyChanged();

                    // 現在色の取得
                    var resources = Application.Current.Resources;

                    // アイコン
                    // 変更後言語情報の取得
                    var newDict = new ResourceDictionary { Source = new Uri($"/Language/{currentLanguage.ToString()}.xaml", UriKind.Relative) };

                    // 既存キーの書き換え
                    resources["LanguageIconKind"] = newDict["LanguageIconKind"];

                    // 言語
                    newDict = new ResourceDictionary { Source = new Uri($"/Theme/{currentLanguage.ToString()}Word.xaml", UriKind.Relative) };

                    // 既存キーの書き換え
                    foreach (var key in newDict.Keys)
                    {
                        resources[key] = newDict[key];
                    }
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #region メッセージ群

        /// <summary>
        /// MFCポートエラーメッセージ
        /// </summary>
        public string MfcPortError
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "MFCポートが開いていません。",
                LanguageType.English => "MFC port is not open.",
                _ => ""
            };
        }

        /// <summary>
        /// Balanceポートエラーメッセージ
        /// </summary>
        public string BalancePortError
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "天秤ポートが開いていません。",
                LanguageType.English => "Balance port is not open.",
                _ => ""
            };
        }

        /// <summary>
        /// MFCコマンド通信エラーメッセージ
        /// </summary>
        public string MFCCommandCommError(string failedCommand)
        {
            return CurrentLanguage switch
            {
                LanguageType.Japanese => $"{failedCommand} コマンドに失敗しました。",
                LanguageType.English => $"Failed to {failedCommand} command.",
                _ => ""
            };
        }

        /// <summary>
        /// ポートオープンエラーメッセージ
        /// </summary>
        public string PortOpenError
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "ポートオープンに失敗しました。",
                LanguageType.English => "Failed to open the port.",
                _ => ""
            };
        }

        /// <summary>
        /// ポート更新メッセージ
        /// </summary>
        public string PortReloading
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "ポート更新中...",
                LanguageType.English => "Port reloading...",
                _ => ""
            };
        }

        /// <summary>
        /// ポート切断メッセージ
        /// </summary>
        public string PortDisconnected
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "ポートが切断されました。",
                LanguageType.English => "Port disconnected.",
                _ => ""
            };
        }

        /// <summary>
        /// MFC通信エラーメッセージ
        /// </summary>
        public string MfcCommError
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "MFCとの通信でエラーが生じました。",
                LanguageType.English => "Failed to communicate with MFC",
                _ => ""
            };
        }

        /// <summary>
        /// Balance通信エラーメッセージ
        /// </summary>
        public string BalanceCommError
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "天秤との通信でエラーが生じました。",
                LanguageType.English => "Failed to communicate with Balance",
                _ => ""
            };
        }

        /// <summary>
        /// MFM開始メッセージ
        /// </summary>
        public string MfmStart
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "10点リニア係数を全て初期化します",
                LanguageType.English => "All 10-point linear coefficients will be initialized.",
                _ => ""
            };
        }

        /// <summary>
        /// MFM時のゼロチェックガス確認メッセージ
        /// </summary>
        public string ZeroCheckConfirm
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "ゼロ確認を行います。\nガスを止めてバルブをクローズにしてください。",
                LanguageType.English => "Zero check will be performed.\nPlease stop the gas and close the valve.",
                _ => ""
            };
        }

        /// <summary>
        /// CalAgain時のデータ不足メッセージ
        /// </summary>
        public string CalAgainConfirm
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "ゲイン計算には10個のリーディングデータが必要です。",
                LanguageType.English => "Ten readings are required for gain calculation.",
                _ => ""
            };
        }

        /// <summary>
        /// 画面遷移時、出力保存の初回確認メッセージ
        /// </summary>
        public string FirstConfirmBeforeTransit
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "未保存の出力結果があります。\nデータを保存しますか？",
                LanguageType.English => "There are unsaved output results.\nDo you want to save the data?",
                _ => ""
            };
        }

        /// <summary>
        /// 画面遷移時、出力保存の初回確認メッセージ
        /// </summary>
        public string SecondConfirmBeforeTransit
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "確認です。\n未保存の出力結果があります。\nデータを保存しますか？",
                LanguageType.English => "Confirm:\nThere are unsaved output results.\nDo you want to save the data?",
                _ => ""
            };
        }

        /// <summary>
        /// アプリ終了時、出力保存の初回確認メッセージ
        /// </summary>
        public string FirstConfirmBeforeQuit
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "未保存の出力結果があります。\nこのままアプリケーションを終了しますか？",
                LanguageType.English => "You have unsaved changes.\nAre you sure you want to exit the application?",
                _ => ""
            };
        }

        /// <summary>
        /// アプリ終了時、出力保存の最終確認メッセージ
        /// </summary>
        public string SecondConfirmBeforeQuit
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "確認です。\n未保存の出力結果があります。本当にアプリケーションを終了しますか？",
                LanguageType.English => "Confirmation:\nYou have unsaved changes. Are you absolutely sure you want to exit the application?",
                _ => ""
            };
        }

        /// <summary>
        /// 5%刻み設定でCal&Conf押したときのメッセージ
        /// </summary>
        public string CannotCalWith5per
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "5%刻みでの実行は不可能です。",
                LanguageType.English => "It is not possible to execute in 5% increments.",
                _ => ""
            };
        }

        /// <summary>
        /// オペレーション失敗
        /// </summary>
        public string OperationFailed
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "操作が失敗しました。",
                LanguageType.English => "The operation failed.",
                _ => ""
            };
        }

        /// <summary>
        /// オペレーションキャンセル
        /// </summary>
        public string OperationCanceled
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "操作が中断されました。",
                LanguageType.English => "The operation canceled.",
                _ => ""
            };
        }

        /// <summary>
        /// アドレスファイルが検知不可
        /// </summary>
        public string AddressCsvNotfound
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "アドレスcsvファイルが見つかりません。",
                LanguageType.English => "The address csv file does not found.",
                _ => ""
            };
        }

        /// <summary>
        /// アドレスファイルのフォーマットエラー
        /// </summary>
        public string AddressCsvFormatError
        {
            get => CurrentLanguage switch
            {
                LanguageType.Japanese => "アドレスcsvファイルのフォーマットに誤りがあります。",
                LanguageType.English => "The address CSV format is invalid.",
                _ => ""
            };
        }

        #endregion
    }
}
