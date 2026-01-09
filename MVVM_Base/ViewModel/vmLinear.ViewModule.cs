using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

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


        private void AdjustFontSizeByDelta(int delta)
        {
            if (delta > 0 && tcnt > 4) return;
            if (delta < 0 && tcnt < -4) return;

            TitleFontSize += delta;
            LabelFontSize += delta;
            //StatusFontSize += delta;
            //ComboFontSize += delta;
            IconSize += delta;
            //StatusIconSize += delta;

            SmallGBWidth += delta * 12;
            MiddleGBWidth += delta * 18;
            LargeGBWidth += delta * 18;
            //GroupBoxStatusWidth += delta * 16;
            //GroupBoxDebugWidthA += delta * 32;
            //GroupBoxDebugWidthB += delta * 16;

            //ComboWidthLongSize += delta * 16;
            //ComboWidthSize += delta * 4;
            //ComboHeighSize += delta * 1;
            //ComboPaddingSize += delta * 1;

            //PortBtnSize += delta * 3;
            //RadioBtnSIze += delta * 1;
            //RadioBtnBloomSIze += delta * 1;

            //DebugTextBoxSIze += delta * 5;
            //DebugTextBoxLongSIze += delta * 8;

            tcnt += delta;
        }

        /// <summary>
        /// viewロード時に呼ばれる。要イベント登録
        /// </summary>
        public async void OnViewLoaded()
        {
            vmService.CanTransit = false;

            // 誤操作防止
            SwitchAllbtn(false);

            IsViewVisible = true;

            // TODO 以下二つの処理でエラー出た場合の処理
            _loadCts = new CancellationTokenSource();

            if (commStatusService.IsMfcConnected && commStatusService.IsBalanceConnected)
            {
                SerialNum = await ReadSerialNumber(_loadCts.Token);
                var res = await FBDataRead(_loadCts.Token);
            }

            // MFM必須コマンド以外を有効化(Initial状態)
            SwitchBeforeMFMBtn(true);

            vmService.CanTransit = true;
        }

        /// <summary>
        /// viewアンロード時に呼ばれる。要イベント登録
        /// </summary>
        public void OnViewUnloaded()
        {
            IsViewVisible = false;
        }

        /// <summary>
        /// 計測結果の表をリセットする
        /// </summary>
        private void ResetMeasureResult()
        {
            // 計測結果の表を新規形成
            if (Column0.Count == 0)
            {                
                for (int i = 0; i < 11; i++)
                {
                    // 天秤値格納リスト初期化
                    balNumList[i] = 0;
                    if(i == 0)
                    {
                        MeasureResult temp1 = new MeasureResult();
                        temp1.Value = "gn5-gn";
                        Column0.Add(temp1);

                        MeasureResult temp2 = new MeasureResult();
                        temp2.Value = ($"");
                        Column1.Add(temp2);

                        dateList[i] = new DateTime();
                        continue;
                    }

                    MeasureResult di = new MeasureResult();
                    di.Value = ($"d{i}");
                    Column0.Add(di);
                    dateList[i] = new DateTime();

                    MeasureResult m = new MeasureResult();
                    m.Value = ($"");
                    Column1.Add(m);

                    SetPointArray[i] = (i * 10).ToString();
                    
                }
            }
            // 値を全て初期化
            else
            {
                for (int i = 0; i < 11; i++)
                {
                    Column1[i].Value = "";                    
                }
            }            
        }

        bool isInitOutput = false;
        /// <summary>
        /// 出力結果の表をリセットする
        /// </summary>
        private void ResetOutputResult()
        {
            // 計測結果の表を新規形成
            if (!isInitOutput)
            {
                SetPointArray[0] = "Set Point";
                TrueValueArray[0] = "True_V";
                ReadingValueArray[0] = "Reading_V";
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
                for (int i = 1; i < 11; i++)
                {
                    ReadingValueArray[i] = "";
                    InitialVoArray[i] = "";
                    CorrectDataArray[i] = "";
                    VoutArray[i] = "";
                    VOArray[i] = "";
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


        #region ボタンのスイッチング
        /// <summary>
        /// CanBtnAttribute属性が付与されたboolプロパティの値を一括変更する
        /// </summary>
        /// <param name="enable"></param>
        private void SwitchAllbtn(bool enable)
        {
            SwitchAfterMFMBtn(enable);
            SwitchBeforeMFMBtn(enable);
            SwitchZeroBtn(enable);
            SwitchSpanBtn(enable);
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
