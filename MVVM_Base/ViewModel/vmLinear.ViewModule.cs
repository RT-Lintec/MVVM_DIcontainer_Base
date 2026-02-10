using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {
        /// <summary>
        /// UI拡縮
        /// </summary>
        public ICommand AdjustUICommand => new RelayCommand<object>(e =>
        {
            int delta = 0;

            if (e is KeyEventArgs k)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
                if (k.Key == Key.OemPlus || k.Key == Key.Add) delta = 1;
                else if (k.Key == Key.OemMinus || k.Key == Key.Subtract) delta = -1;
            }
            else if (e is MouseWheelEventArgs me)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
                delta = me.Delta > 0 ? 1 : -1;
            }
            else
            {
                return;
            }

            // deltaが決まったらサイズ調整
            AdjustFontSizeByDelta(delta);
        });

        /// <summary>
        /// UI拡縮
        /// 現合のため全てマジックナンバー
        /// </summary>
        /// <param name="delta"></param>
        private void AdjustFontSizeByDelta(int delta)
        {
            if (delta > 0 && tcnt > 4) return;
            if (delta < 0 && tcnt < -4) return;

            TitleFontSize += delta;
            LabelFontSize += delta;
            UnitFontSize += delta;
            DataGridFontSize += delta;
            StatusFontSize += delta;
            LogFontSize += (float)delta * 0.5f;
            VtFontSize += (float)delta;
            ConfHeightSize += (float)delta * 0.68f;
            ConfBtnFontSize += (float)delta * 0.5f;
            OtherSettingFontSize += delta;

            RadioBtnSIze += delta;
            IconSize += delta;
            CmdBtnSIze += delta * 6;
            MfmBtnSIze += delta * 4;
            ZaBtnSIze += delta * 6;
            SpanGainSIze += delta * 2;
            OutputBtnSIze += delta * 4;
            MesureBtnSize += delta * 4;

            GroupBoxWidth90 += delta * 4;
            GroupBoxWidth100 += delta * 4;
            SmallGBWidth += delta * 14;
            MiddleGBWidth += delta * 18;
            LargeGBWidth += delta * 18;
            GroupBoxWidth700 += delta * 37;
            GroupBoxWidth150 += delta * 8;
            GroupBoxWidth500 += delta * 29;
            GroupBoxWidth245 += delta * 14;
            GroupBoxWidth200 += delta * 15;
            UnitTextboxWidth += delta * 8;
            MeasureColumNameWidth += delta * 4;
            SpanInputWidth += delta * 3;
            FlowOutputBoxWidth += delta * 5;

            MiddleGBHeight += delta * 8;
            GroupBoxHeight250 += delta * 10;
            LogHeightSize += (float)delta * 21.5f;
            FivePerHeightSize += (float)delta * 10.2f;
            FivePerMatrixWidth += (float)delta * 3f;

            VTempOutputBoxWidth += delta * 2;
            MeasureSettingTextBoxWidth += delta * 3;
            tcnt += delta;
        }

        /// <summary>
        /// ログ追加
        /// </summary>
        /// <param name="message"></param>
        private void Logging(string message, bool isNeedLinebreak)
        {
            // 改行不要
            if (!isNeedLinebreak)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    Logs.Add($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} {message}");
                });
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    Logs.Add($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} {message}");
                    
                    // TODO : ユニークな文字列しか反応してくれない
                    Logs.Add($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}");
                });
            }
        }

        /// <summary>
        /// viewロード時に呼ばれる。要イベント登録
        /// </summary>
        public async void OnViewLoaded()
        {
            // FBアドレスの読み込み
            string baseDir = AppContext.BaseDirectory;
            baseDir = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
            string csv = System.IO.Path.Combine(baseDir, "FB\\FB_Address.csv");


            if (!File.Exists(csv))
            {
                await messageService.ShowMessage(languageService.AddressCsvNotfound);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();

                return;
            }

            if (!LoadFromCsv(csv))
            {
                await messageService.ShowMessage(languageService.AddressCsvFormatError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();

                return;
            }
;
            IsMapGenerated = true; // 初回起動時のみ
            // 計算を何もしていない場合
            if (!isCalculated && !isCalcedAndConfed)
            {
                isSavedOutput = false;

                // 誤操作防止
                await ChangeState(ProcessState.Transit);
                vmService.CanTransit = false;

                // TODO 以下二つの処理でエラー出た場合の処理
                _loadCts = new CancellationTokenSource();

                if (commStatusService.IsMfcConnected)
                {
                    var res = await ReadSerialNumber(_loadCts.Token);
                    SerialNum = res.Payload;
                    res = await FBDataRead(_loadCts.Token);
                    IsMapGenerated = false; // 初回起動時のみ
                }

                // MFM必須コマンド以外を有効化(Initial状態)
                await ChangeState(ProcessState.Initial);
                vmService.CanTransit = true;
            }
            // Calc済み
            else if(isCalculated && !isCalcedAndConfed)
            {
                isSavedOutput = false;
                await ChangeState(ProcessState.AfterCalc);
            }
            // Calc、Conf済み
            else if (isCalculated && isCalcedAndConfed)
            {
                await ChangeState(ProcessState.AfterCalcAndConf);
            }           
        }

        /// <summary>
        /// 10点調整ゲインアドレスを取得する
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="InvalidDataException"></exception>
        public bool LoadFromCsv(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path)
                    .Select(Clean)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();

                int gainLineNum = 0;
                int gainLineEndNum = gainLineNum + 5;

                // ゲイン
                for (int i = gainLineNum; i < gainLineEndNum;)
                {
                    var header = lines[i++];

                    var logicals = lines[i++]
                        .Split(',')
                        .Select(Clean)
                        .Where(x => x.Length > 0)
                        .ToArray();

                    var actuals = lines[i++]
                        .Split(',')
                        .Select(Clean)
                        .Where(x => x.Length > 0)
                        .ToArray();

                    // 長さが違う = 内容・形式が異なる
                    if (logicals.Length != actuals.Length)
                    {
                        return false;
                    }

                    // 重複チェック
                    bool hasDuplicate = logicals.Length != logicals.Distinct().Count();
                    if (hasDuplicate)
                    {
                        return false;
                    }

                    hasDuplicate = actuals.Length != actuals.Distinct().Count();
                    if (hasDuplicate)
                    {
                        return false;
                    }

                    // キーとバリューのセット
                    for (int j = 0; j < logicals.Length; j++)
                    {
                        FbMap[logicals[j]] = actuals[j];
                    }
                }

                // UI 更新
                foreach (var key in FbMap.Keys)
                    OnPropertyChanged($"Item[{key}]");

                // 10点リニア閾値
                int thresholdLineNum = gainLineEndNum + 1;
                int thresholdLineEndNum = gainLineEndNum + 5;
                
                for (int i = thresholdLineNum; i < thresholdLineEndNum;)
                {
                    var header = lines[i++];

                    var logicals = lines[i++]
                        .Split(',')
                        .Select(Clean)
                        .Where(x => x.Length > 0)
                        .ToArray();

                    var actuals = lines[i++]
                        .Split(',')
                        .Select(Clean)
                        .Where(x => x.Length > 0)
                        .ToArray();

                    // 長さが違う = 内容・形式が異なる
                    if (logicals.Length != actuals.Length)
                    {
                        return false;
                    }

                    // 重複チェック
                    bool hasDuplicate = logicals.Length != logicals.Distinct().Count();
                    if (hasDuplicate)
                    {
                        return false;
                    }

                    hasDuplicate = actuals.Length != actuals.Distinct().Count();
                    if (hasDuplicate)
                    {
                        return false;
                    }

                    // キーとバリューのセット
                    for (int j = 0; j < logicals.Length; j++)
                    {
                        ThresholdMap[logicals[j]] = actuals[j];
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 不要な文字を削除する
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static string Clean(string s)
        {
            return s
                .Replace("\"", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();
        }

        /// <summary>
        /// viewアンロード時に呼ばれる。要イベント登録
        /// </summary>
        public async void OnViewUnloaded()
        {
            //IsViewVisible = false;
            // TODO : 画面遷移を待ちたい
            await ChangeState(ProcessState.Initial);
        }

        /// <summary>
        /// True値の計算・格納
        /// </summary>
        private void CalAndSetTrueValue()
        {
            for (int i = 1; i < TrueValueArray.Count; i++)
            {
                TrueValueArray[i] = (float.Parse(FlowValue) / 10f * (float)i).ToString("F2");
            }
        }

        /// <summary>
        /// 計測結果の表をリセットする
        /// </summary>
        private void ResetMeasureResult()
        {
            // 計測結果の表を新規形成
            if (MesurementItems.Count == 0)
            {                
                for (int i = 0; i < 11; i++)
                {
                    // 天秤値格納リスト初期化
                    balNumList[i] = 0;
                    if(i == 0)
                    {
                        MeasureResult temp1 = new MeasureResult();
                        temp1.Value = "gn5-gn";
                        MesurementItems.Add(temp1);

                        MeasureResult temp2 = new MeasureResult();
                        temp2.Value = ($"");
                        MeasurementValues.Add(temp2);

                        dateList[i] = new DateTime();
                        continue;
                    }

                    MeasureResult di = new MeasureResult();
                    di.Value = ($"d{i}");
                    MesurementItems.Add(di);
                    dateList[i] = new DateTime();

                    MeasureResult m = new MeasureResult();
                    m.Value = ($"");
                    MeasurementValues.Add(m);

                    SetPointArray[i] = (i * 10).ToString();

                    SetPointBelow50PercentArray[i] = (i * 5).ToString();
                    SetPointAbove50PercentArray[i] = (i * 5 + 50).ToString();
                }
            }
            // 値を全て初期化
            else
            {
                for (int i = 0; i < 11; i++)
                {
                    MeasurementValues[i].Value = "";                    
                }
            }            
        }

        /// <summary>
        /// 初回出力かどうかを判定。出力結果表の項目書き込みを一回のみ行うため。
        /// </summary>
        bool isInitOutput = false;
        /// <summary>
        /// 出力結果の表をリセットする
        /// </summary>
        private void ResetOutputResult(bool isFiveper)
        {
            // 計測結果の表を新規形成
            if (!isInitOutput)
            {
                SetPointArray[0] = "Set Point";
                SetPointBelow50PercentArray[0] = "Set Point";
                SetPointAbove50PercentArray[0] = "Set Point";
                TrueValueArray[0] = "True_V";
                ReadingValueArray[0].Value = "Reading_V";
                ReadingValueBelow50Array[0] = "Reading_V";
                ReadingValueAbove50Array[0] = "Reading_V";

                InitialVoArray[0] = "Initial VO";
                CorrectDataArray[0] = "C_Data";
                VoutArray[0] = "VOUT";
                VOArray[0] = "VO";
                ConfirmArray[0] = "Confirm";
                isInitOutput = true;
            }
            // 値を全て初期化
            else
            {
                // 通常の出力表のみ
                if (!isFiveper)
                {
                    for (int i = 1; i < 11; i++)
                    {
                        ReadingValueArray[i].Value = "";
                        InitialVoArray[i] = "";
                        CorrectDataArray[i] = "";
                        VoutArray[i] = "";
                        VOArray[i] = "";
                    }
                }
                // 5%刻みのみ
                else
                {
                    for (int i = 1; i < 11; i++)
                    {
                        ReadingValueBelow50Array[i] = "";
                        ReadingValueAbove50Array[i] = "";
                    }
                }
            }
        }

        /// <summary>
        /// Confrim処理に関する出力結果の表をリセットする
        /// </summary>
        private void ResetOutputResult_Confrim()
        {
            for (int i = 1; i < 11; i++)
            {
                CorrectDataArray[i] = "";
                VOArray[i] = "";
            }            
        }

        /// <summary>
        /// Reading値をcsv出力
        /// </summary>
        /// <param name="path"></param>
        [RelayCommand]
        private async Task ExportParamsToCsv()
        {
            // 空欄なしチェック
            // VO
            foreach (var val in VOArray)
            {
                if (val == "")
                {
                    return;
                }
            }

            // True値
            foreach (var val in TrueValueArray)
            {
                if (val == "")
                {
                    return;
                }
            }

            // Reading値
            foreach (var val in ReadingValueArray) 
            {
                if (val.Value == "")
                {
                    return;
                }
            }

            // VOUT
            foreach (var val in VoutArray)
            {
                if (val == "")
                {
                    return;
                }
            }

            // FB
            var props = this.GetType().GetProperties()
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttributes(typeof(FbCodeAttribute), false)
                            .Cast<FbCodeAttribute>()
                            .FirstOrDefault()
                })
                .Where(x => x.Attr != null)
                .ToList();

            var fbPairs = new List<Tuple<string, string>>();
            for (int i = 0; i < props.Count; i++)
            {
                string value = (string)props[i].Property.GetValue(this);
                string code = FbMap[props[i].Attr.Code];

                fbPairs.Add(Tuple.Create(code, value));
            }

            // Vtemp 基準1を取得
            var vTempLowerCal = VtempValueCal.Substring(0, 2);
            var vTempUpperCal = VtempValueCal.Substring(3, 2);
            var vTempLowerConf = VtempValueConf.Substring(0, 2);
            var vTempUpperConf = VtempValueConf.Substring(3, 2);

            var sb = new StringBuilder();
            sb.AppendLine("Initial VO,C_Data,True,Reading,Vout,VO,Vtemp lower(Cal), Vtemp upper(Cal),Vtemp lower(Conf), Vtemp upper(Conf)");

            for (int i = 1; i < VOArray.Count; i++)
            {
                var line = new StringBuilder();
                line.Append($"=\"{InitialVoArray[i]}\",=\"{CorrectDataArray[i]}\",=\"{TrueValueArray[i]}\",=\"{ReadingValueArray[i].Value}\",=\"{VoutArray[i]}\",=\"{VOArray[i]}\"");
                if (i == 1)
                {
                    line.Append($",=\"{vTempLowerCal}\"");
                    line.Append($",=\"{vTempUpperCal}\"");
                    line.Append($",=\"{vTempLowerConf}\"");
                    line.Append($",=\"{vTempUpperConf}\"");
                }
                sb.AppendLine(line.ToString());
            }

            sb.AppendLine("");

            string[] fbsList =
            {
                    "FB90","FB91","FB92","FB93","FB94","FB95","FB96","FB97","FB98","FB99",
                    "FB9A","FB9B","FB9C","FB9D","FB9E","FB9F","FBA0","FBA1","FBA2","FBA3","FB41","FB42"
            };

            string l = "";
            for (int i = 0; i < FbMap.Count; i++)
            {
                if (i != FbMap.Count - 1)
                {
                     l += FbMap[fbsList[i]] + ",";                    
                }
                else
                {
                    l += FbMap[fbsList[i]];
                }
            }

            sb.AppendLine(l);

            var line2 = new StringBuilder();
            for (int j = 0; j < fbPairs.Count; j++)
            {
                line2.Append($"=\"{fbPairs[j].Item2}\",");
            }
            sb.AppendLine(line2.ToString());       

            string baseDir = AppContext.BaseDirectory;
            string csvDir = System.IO.Path.Combine(baseDir, "CSV\\Parameters");

            if (!Directory.Exists(csvDir))
            {
                Directory.CreateDirectory(csvDir);
            }

            string now = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = "\\"+ "SN" + SerialNum + "_" + now + "_Parameters.csv";
            string path = csvDir + fileName;

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            isSavedOutput = true;
            vmService.HasNonsavedOutput = false;
        }

        /// <summary>
        /// Reading値をcsv出力
        /// </summary>
        /// <param name="path"></param>

        private void ExportReadingCsv()
        {
            foreach (var val in ReadingValueArray)
            {
                if (val.Value == "")
                {
                    return;
                }
            }

            var sb = new StringBuilder();

            sb.AppendLine("Reading Value");

            for (int i = 1; i < ReadingValueArray.Count; i++)
            {
                sb.AppendLine(ReadingValueArray[i].Value);
            }

            string baseDir = AppContext.BaseDirectory;
            string csvDir = System.IO.Path.Combine(baseDir, "CSV\\Reading");

            if (Directory.Exists(csvDir))
            {
                // フォルダあり
            }
            else
            {
                Directory.CreateDirectory(csvDir);
            }

            string now = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = "\\" + SerialNum + "_" + now + "Reading.csv";
            string path = csvDir + fileName;

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        #region ボタンのスイッチング
        /// <summary>
        /// CanBtnAttribute属性が付与されたboolプロパティの値を一括変更する
        /// </summary>
        /// <param name="enable"></param>
        private void SwitchAllbtn(bool enable)
        {
            CanExport = enable;
            CanConfAlone = enable;
            SwitchAfterMFMBtn(enable);
            SwitchBeforeMFMBtn(enable);
            SwitchZeroBtn(enable);
            SwitchSpanBtn(enable);

            if (vmService.HasNonsavedOutput)
            {
                CanExport = true;
            }
        }

        /// <summary>
        /// CanAfterMFMAttribute属性が付与されたboolプロパティの値を一括変更する
        /// </summary>
        /// <param name="enable"></param>
        private void SwitchAfterMFMBtn(bool enable)
        {
            var props = this.GetType().GetProperties()
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttributes(typeof(CanAfterMFMAttribute), false)
                            .Cast<CanAfterMFMAttribute>()
                            .FirstOrDefault()
                })
                .Where(x => x.Attr != null)
                .ToList();

            foreach (var p in props)
            {
                p.Property.SetValue(this, enable);
            }
        }

        /// <summary>
        /// CanBeforeMFMAttribute属性が付与されたboolプロパティの値を一括変更する
        /// </summary>
        /// <param name="enable"></param>
        private void SwitchBeforeMFMBtn(bool enable)
        {
            var props = this.GetType().GetProperties()
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttributes(typeof(CanBeforeMFMAttribute), false)
                            .Cast<CanBeforeMFMAttribute>()
                            .FirstOrDefault()
                })
                .Where(x => x.Attr != null)
                .ToList();

            foreach (var p in props)
            {
                p.Property.SetValue(this, enable);
            }
        }

        /// <summary>
        /// ゼロ調整関連のボタン：CanZeroAttributeをスイッチング
        /// </summary>
        /// <param name="enable"></param>
        private void SwitchZeroBtn(bool enable)
        {
            var props = this.GetType().GetProperties()
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttributes(typeof(CanZeroAttribute), false)
                    .Cast<CanZeroAttribute>()
                    .FirstOrDefault()
                })
                .Where(x => x.Attr != null)
                .ToList();

            foreach (var p in props)
            {
                p.Property.SetValue(this, enable);
            }
        }

        /// <summary>
        /// スパン関連のボタン：CanSpanAttributeをスイッチング
        /// </summary>
        /// <param name="enable"></param>
        private void SwitchSpanBtn(bool enable)
        {
            var props = this.GetType().GetProperties()
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttributes(typeof(CanSpanAttribute), false)
                    .Cast<CanSpanAttribute>()
                    .FirstOrDefault()
                })
                .Where(x => x.Attr != null)
                .ToList();

            foreach (var p in props)
            {
                p.Property.SetValue(this, enable);
            }
        }
        #endregion
    }
}
