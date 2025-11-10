using MVVM_Base.Model;
using MVVM_Base.ViewModel;
using System.ComponentModel;
//using System.Drawing;

//using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace MVVM_Base.View
{
    public partial class viewEntry : Window
    {
        /// <summary>
        /// トグルボタンの括弧に関する位置設定
        /// </summary>
        enum CornerType { TopLeft, TopRight, BottomLeft, BottomRight }

        /// <summary>
        /// トグルボタンの括弧に関するデータ
        /// </summary>
        struct PathDatas
        {
            public Path[] paths;
            public ToggleButton tb;
            public Point lastP;
        }
        PathDatas pathDatas = new PathDatas();

        /// <summary>
        /// アプリケーション終了ボタンのカラー
        /// </summary>
        public Brush btnEndColor { get; set; }
        byte r_Thumb = 0x80;
        byte g_Thumb = 0x80;
        byte b_Thumb = 0x80;

        /// <summary>
        /// アプリケーション終了ボタン枠の幅
        /// </summary>
        public int btnEndWidth { get; set; }

        /// <summary>
        /// アプリケーション終了ボタン枠の高さ
        /// </summary>
        public int btnEndHeight { get; set; }

        /// <summary>
        /// 初期値はダークモード
        /// </summary>
        private bool _isDark = true;
        private bool isOn = false;
        private int tBarAnimInterval = 5;
        private int tBarAnimTransition = 1;

        #region Win32 定数
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_ENTERSIZEMOVE = 0x0231;
        private const int WM_EXITSIZEMOVE = 0x0232;

        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private const int RESIZE_BORDER = 8;

        // --- 内部状態 ---
        private bool isResizing = false;
        #endregion

        #region Bloom関連
        private bool _isBloomEnabled;
        public bool IsBloomEnabled
        {
            get => _isBloomEnabled;
            set
            {
                if (_isBloomEnabled != value)
                {
                    _isBloomEnabled = value;
                    OnPropertyChanged(nameof(IsBloomEnabled));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private DropShadowEffect _glowEffect;
        private DispatcherTimer _glowTimer;
        private bool _glowIncreasing = true;
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="vmEntry"></param>
        public viewEntry(vmEntry vmEntry)
        {
            InitializeComponent();
            DataContext = vmEntry;

            btnEndColor = new SolidColorBrush(Color.FromRgb(r_Thumb, g_Thumb, b_Thumb));
            btnEndWidth = 100;
            btnEndHeight = 36;

            ApplyTheme("Dark");

            this.SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle)?.AddHook(WndProc);
            };
        }

        /// <summary>
        /// 指定テーマのResourceDictionaryを適用
        /// </summary>
        private void ApplyTheme(string theme)
        {
            // アプリ全体のリソースを取得
            var appResources = Application.Current.Resources;

            // "ThemeResources" を探してキャスト
            if (appResources["ThemeResources"] is ResourceDictionary themeSlot)
            {
                // スロット内の既存テーマをクリア
                themeSlot.MergedDictionaries.Clear();

                // 新しいテーマ辞書を追加
                var newTheme = new ResourceDictionary
                {
                    Source = new Uri($"/Theme/{theme}Theme.xaml", UriKind.Relative)
                };
                themeSlot.MergedDictionaries.Add(newTheme);
            }
        }

        Point LastPos;
        /// <summary>
        /// トグルボタン上でのマウス左ボタン押下時のバインド処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavToggleButton_Click(object sender, MouseButtonEventArgs e)
        {
            var button = (ToggleButton)sender;

            // クリックされた座標をボタン内で取得
            if (e == null) return;

            Point clickPosInButton = e.GetPosition(button);

            // ボタン座標 -> Canvas 座標に変換
            Point pos = button.TranslatePoint(clickPosInButton, EffectCanvas);

            //SpawnSpark(pos);
            //SpawnRipple(pos);
            LastPos = pos;
        }

        /// <summary>
        /// トグルボタン上でのマウス左ボタンクリック完了時のバインド処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavToggleButton_Check(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;
            SpawnRipple(LastPos);
            ShowCornerBrackets(button, true);
        }

        /// <summary>
        /// 描画画面全体におけるマウス左ボタンクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Area_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(EffectCanvas);
            //SpawnSpark(pos);
            SpawnRipple(pos);
        }

        /// <summary>
        /// クリックエフェクト：スパーク
        /// </summary>
        /// <param name="pos"></param>
        private void SpawnSpark(Point pos)
        {
            var rnd = new Random();

            for (int i = 0; i < 8; i++)
            {
                var ellipse = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Orange,
                    Opacity = 0.8
                };

                EffectCanvas.Children.Add(ellipse);
                Canvas.SetLeft(ellipse, pos.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, pos.Y - ellipse.Height / 2);

                var animX = new DoubleAnimation(pos.X, pos.X + rnd.Next(-40, 40), TimeSpan.FromSeconds(0.5));
                var animY = new DoubleAnimation(pos.Y, pos.Y + rnd.Next(-40, 40), TimeSpan.FromSeconds(0.5));
                var fade = new DoubleAnimation(0.8, 0, TimeSpan.FromSeconds(0.5));

                fade.Completed += (s, _) => EffectCanvas.Children.Remove(ellipse);

                ellipse.BeginAnimation(Canvas.LeftProperty, animX);
                ellipse.BeginAnimation(Canvas.TopProperty, animY);
                ellipse.BeginAnimation(UIElement.OpacityProperty, fade);
            }
        }

        /// <summary>
        /// クリックエフェクト：波紋
        /// </summary>
        /// <param name="pos"></param>
        private void SpawnRipple(Point pos)
        {
            var brush = Application.Current.Resources["RippleColorBrush"] as SolidColorBrush;

            // 波紋の基本設定 数値は適当
            var ellipse = new Ellipse
            {
                Width = 5,
                Height = 5,
                //Stroke = Brushes.DeepSkyBlue,
                Stroke = brush,//Brushes.Coral,
                StrokeThickness = 1,
                Opacity = 0.6
            };

            EffectCanvas.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, pos.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, pos.Y - ellipse.Height / 2);

            var scaleTransform = new ScaleTransform(1, 1, ellipse.Width / 2, ellipse.Height / 2);
            ellipse.RenderTransform = scaleTransform;

            // スケールアニメーション
            var scaleAnim = new DoubleAnimation(1, 5, TimeSpan.FromSeconds(0.6));
            // 透明度アニメーション
            var fadeAnim = new DoubleAnimation(0.6, 0, TimeSpan.FromSeconds(0.6));

            fadeAnim.Completed += (s, e) => EffectCanvas.Children.Remove(ellipse);

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            ellipse.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        /// <summary>
        /// ボタンエフェクト：スプリング括弧
        /// </summary>
        /// <param name="btn"></param>
        private void ShowCornerBrackets(ToggleButton btn, bool isAnimate)
        {
            EffectCanvas_tb.Children.Clear();

            // ボタンの位置とサイズをCanvas座標で取得
            var topLeft = btn.TranslatePoint(new Point(0, 0), EffectCanvas_tb);
            double x = topLeft.X;
            double y = topLeft.Y;
            double w = btn.ActualWidth;
            double h = btn.ActualHeight;
            double len = 6;  // カッコの長さ

            pathDatas.paths = new[]
            {
                CreateCornerPath(x, y, len, CornerType.TopLeft),
                CreateCornerPath(x + w, y, len, CornerType.TopRight),
                CreateCornerPath(x, y + h, len, CornerType.BottomLeft),
                CreateCornerPath(x + w, y + h, len, CornerType.BottomRight),
            };

            foreach (var p in pathDatas.paths)
            {
                EffectCanvas_tb.Children.Add(p);
                if(isAnimate)
                {
                    AnimateCorner(p, btn);
                }                
            }

            pathDatas.tb = btn;
            pathDatas.lastP = btn.PointToScreen(new Point(0, 0));
        }

        /// <summary>
        /// 左メニューボタン押下時、コーナーに括弧作成
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="len"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Path CreateCornerPath(double x, double y, double len, CornerType type)
        {
            var geom = new PathGeometry();
            var figure = new PathFigure();

            switch (type)
            {
                case CornerType.TopLeft:
                    figure.StartPoint = new Point(x + len, y);
                    figure.Segments.Add(new LineSegment(new Point(x, y), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + len), true));
                    break;

                case CornerType.TopRight:
                    figure.StartPoint = new Point(x - len, y);
                    figure.Segments.Add(new LineSegment(new Point(x, y), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + len), true));
                    break;

                case CornerType.BottomLeft:
                    figure.StartPoint = new Point(x + len, y);
                    figure.Segments.Add(new LineSegment(new Point(x, y), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y - len), true));
                    break;

                case CornerType.BottomRight:
                    figure.StartPoint = new Point(x - len, y);
                    figure.Segments.Add(new LineSegment(new Point(x, y), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y - len), true));
                    break;
            }

            geom.Figures.Add(figure);

            //var resources = Application.Current.Resources;
            var brush = Application.Current.Resources["TagColorBrush"] as SolidColorBrush;

            return new Path
            {
                Stroke = brush,//new SolidColorBrush(Color.FromRgb(255, 192, 168)),//Brushes.WhiteSmoke,
                StrokeThickness = 2,
                Data = geom
            };
        }

        /// <summary>
        /// 左メニューボタン押下時、コーナーの括弧アニメーション
        /// </summary>
        /// <param name="path"></param>
        /// <param name="btn"></param>
        private void AnimateCorner(Path path, ToggleButton btn)
        {
            // ボタン中心
            Point btnCenter = btn.TranslatePoint(new Point(btn.ActualWidth / 2, btn.ActualHeight / 2), EffectCanvas);

            // Path の中心座標
            Rect bounds = path.Data.GetRenderBounds(new Pen(path.Stroke, path.StrokeThickness));
            Point pathCenter = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

            // 中心からの方向ベクトル
            Vector dir = pathCenter - btnCenter;
            if (dir.Length == 0) dir = new Vector(0, -1); // 中心と同じ場合は上方向に
            dir.Normalize(); // 正規化

            double moveDistance = 8; // 離れる距離

            var translate = new TranslateTransform();
            path.RenderTransform = translate;

            var animX = new DoubleAnimationUsingKeyFrames();
            var animY = new DoubleAnimationUsingKeyFrames();
            var duration = TimeSpan.FromSeconds(0.7);

            // 離れる
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));

            animX.KeyFrames.Add(new EasingDoubleKeyFrame(dir.X * moveDistance, KeyTime.FromPercent(0.3), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(dir.Y * moveDistance, KeyTime.FromPercent(0.3), new QuadraticEase { EasingMode = EasingMode.EaseOut }));

            // 戻る（少し弾む）
            animX.KeyFrames.Add(new EasingDoubleKeyFrame(-dir.X * 5, KeyTime.FromPercent(0.7), new QuadraticEase { EasingMode = EasingMode.EaseIn }));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(-dir.Y * 5, KeyTime.FromPercent(0.7), new QuadraticEase { EasingMode = EasingMode.EaseIn }));

            animX.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new BounceEase { Bounces = 1, Bounciness = 2 }));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new BounceEase { Bounces = 1, Bounciness = 2 }));

            animX.Duration = animY.Duration = duration;

            translate.BeginAnimation(TranslateTransform.XProperty, animX);
            translate.BeginAnimation(TranslateTransform.YProperty, animY);
        }

        /// <summary>
        /// タイトルバーでドラッグ移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// ウィンドウロード時にアニメーション開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AnimateTitleBar();
        }

        /// <summary>
        /// タイトルバーアニメーション
        /// </summary>
        private void AnimateTitleBar()
        {
            Color from1 = GetThemeColor("TitleBarAnimFrom1", Colors.LightGray);
            Color to1 = GetThemeColor("TitleBarAnimTo1", Colors.DarkGray);
            Color from2 = GetThemeColor("TitleBarAnimFrom2", Colors.LightGray);
            Color to2 = GetThemeColor("TitleBarAnimTo2", Colors.DarkGray);

            var tbStoryboard = new Storyboard()
            {
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            };

            // 1本目のグラデーションアニメーション
            var anim1 = new ColorAnimation
            {
                From = from1,
                To = to1,
                Duration = TimeSpan.FromSeconds(tBarAnimInterval),
                AutoReverse = true
            };
            Storyboard.SetTargetName(anim1, "TitleBarGradientStop1");
            Storyboard.SetTargetProperty(anim1, new PropertyPath("Color"));
            tbStoryboard.Children.Add(anim1);

            // 2本目のグラデーションアニメーション
            var anim2 = new ColorAnimation
            {
                From = from2,
                To = to2,
                Duration = TimeSpan.FromSeconds(tBarAnimInterval),
                AutoReverse = true
            };
            Storyboard.SetTargetName(anim2, "TitleBarGradientStop2");
            Storyboard.SetTargetProperty(anim2, new PropertyPath("Color"));
            tbStoryboard.Children.Add(anim2);

            tbStoryboard.Begin(this, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        private Color GetThemeColor(string key, Color fallback)
        {
            // ThemeResourcesスロットを確認
            if (Application.Current.Resources["ThemeResources"] is ResourceDictionary themeSlot)
            {
                // 直接そのスロット内にキーがある場合
                if (themeSlot.Contains(key))
                {
                    return ConvertToColor(themeSlot[key], fallback);
                }

                // スロットの MergedDictionaries を順に検索
                foreach (var md in themeSlot.MergedDictionaries)
                {
                    if (md != null && md.Contains(key))
                        return ConvertToColor(md[key], fallback);
                }
            }

            // Applicationのtop-level MergedDictionariesを探索（念のため）
            foreach (var md in Application.Current.Resources.MergedDictionaries)
            {
                if (md != null && md.Contains(key))
                    return ConvertToColor(md[key], fallback);
            }

            // 最終手段：TryFindResource（Application レベルの探索）
            var obj = Application.Current.TryFindResource(key);
            if (obj != null)
                return ConvertToColor(obj, fallback);

            // 見つからなければフォールバック
            return fallback;
        }

        private Color ConvertToColor(object res, Color fallback)
        {
            if (res is Color c) return c;
            if (res is SolidColorBrush b) return b.Color;
            // 文字列等で定義しているケースがあれば TryParse してみる
            if (res is string s && ColorConverter.ConvertFromString(s) is Color parsed)
                return parsed;
            return fallback;
        }

        /// <summary>
        /// アプリケーション終了ボタンのドラッグ&スライド処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SlideThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = (System.Windows.Controls.Primitives.Thumb)sender;
            int enh = 2;

            // TranslateTransform を取得（初回は作る）
            if (thumb.RenderTransform is not TranslateTransform tt)
            {
                tt = new TranslateTransform();
                thumb.RenderTransform = tt;
            }            

            // X方向に移動
            tt.X += e.HorizontalChange;

            // 左端0、右端制限
            var parent = (FrameworkElement)thumb.Parent;
            double maxX = parent.ActualWidth - thumb.ActualWidth;
            tt.X = Math.Max(0, Math.Min(maxX, tt.X));

            // Endボタンの場合の処理
            if (thumb.Tag.ToString() == "end")
            {
                // Bloomのリセットはend有効位置以外で行う
                if (tt.X < maxX)
                {
                    ResetEffectParams(thumb);
                    thumb.Background = new SolidColorBrush(Color.FromRgb((byte)(Math.Min(tt.X * enh + r_Thumb, 255)), (byte)(Math.Max(g_Thumb - tt.X * enh, 0)), (byte)(Math.Max(b_Thumb - tt.X * enh, 0))));
                }
                else
                {
                    thumb.Background = new SolidColorBrush(Color.FromRgb((byte)255, (byte)30, (byte)0));
                    // Bloom有効化
                    SetThumbBloom(thumb, tt.X >= maxX);

                }
                return;
            }
            else if(thumb.Tag.ToString() == "theme")
            {

            }
        }

        /// <summary>
        /// アプリケーション終了ボタンのドラッグ完了処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SlideThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            var thumb = (System.Windows.Controls.Primitives.Thumb)sender;
            thumb.Background = new SolidColorBrush(Color.FromRgb(r_Thumb, g_Thumb, b_Thumb));
            //var tt = (TranslateTransform)thumb.RenderTransform;
            if (thumb.RenderTransform is not TranslateTransform tt)
            {
                tt = new TranslateTransform();
                thumb.RenderTransform = tt;
            }

            double finalX = tt.X;

            var parent = (FrameworkElement)thumb.Parent;
            // アプリケーション終了ボタンのスライドしてアプリ終了させる位置
            // ボタンの初期位置と終端位置での、ボタン円中心の間隔
            double threshold = btnEndWidth - btnEndHeight;

            if (finalX >= threshold)
            {
                Application.Current.Shutdown();
            }

            // Thumb を左端に戻す
            tt.X = 0;

            ResetEffectParams(thumb);
        }

        /// <summary>
        /// endボタンのブルーム間隔を制御
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GlowTimer_Tick(object? sender, EventArgs e)
        {
            if (_glowEffect == null) return;

            // Opacityを上下させて「明滅」
            if (_glowIncreasing)
            {
                _glowEffect.Opacity += 0.05;
                if (_glowEffect.Opacity >= 1.0)
                    _glowIncreasing = false;
            }
            else
            {
                _glowEffect.Opacity -= 0.05;
                if (_glowEffect.Opacity <= 0.4)
                    _glowIncreasing = true;
            }
        }

        /// <summary>
        /// endボタンのブルーム効果を設定、実行
        /// </summary>
        /// <param name="thumb"></param>
        /// <param name="enabled"></param>
        private void SetThumbBloom(System.Windows.Controls.Primitives.Thumb thumb, bool enabled)
        {
            if (enabled)
            {
                // --- ブルーム開始 ---
                if (_glowEffect == null)
                {
                    _glowEffect = new DropShadowEffect
                    {
                        Color = Colors.Red,
                        BlurRadius = 30,
                        ShadowDepth = 1,
                        Opacity = 1.0
                    };

                    thumb.Effect = _glowEffect;

                    // 明滅タイマー設定
                    _glowTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(60)
                    };
                    _glowTimer.Tick += GlowTimer_Tick;
                    _glowTimer.Start();
                }
            }
            else
            {
                thumb.Effect = null;
            }
        }



        /// <summary>
        /// endボタンのBloomクリア
        /// </summary>
        /// <param name="thumb"></param>
        void ResetEffectParams(System.Windows.Controls.Primitives.Thumb thumb)
        {
            if (_glowTimer != null)
            {
                _glowTimer.Tick -= GlowTimer_Tick;
                _glowTimer.Stop();
                _glowTimer = null;
            }
            if (_glowEffect != null)
            {
                _glowEffect = null;
                thumb.Effect = null;
            }
        }

        /// <summary>
        /// カラーモード切替処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (btn.Tag.ToString() == "theme")
            {
                btn.IsEnabled = false;
                isOn = !isOn;

                // スライド位置変更
                double targetLeft = isOn ? 30 : 0;
                var anim = new DoubleAnimation
                {
                    To = targetLeft,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                btn.BeginAnimation(Canvas.LeftProperty, anim);

                _isDark = !_isDark;
                string theme = _isDark ? "Dark" : "Light";

                ApplyTheme(theme);
                ColorThemeChange(btn, theme);
            }
        }

        /// <summary>
        /// 色変化をアニメーション付きで行う
        /// </summary>
        private void ColorThemeChange(Button btn, string theme)
        {
            if (theme != null)
            {
                // 現在色の取得
                var resources = Application.Current.Resources;
                Color oldThemeColor = (Color)resources["ButtonThemeColor"];
                Color oldAcsColor = (Color)resources["AccentColor"];
                Color oldWcColor1 = (Color)resources["LeftWindowColor1"];
                Color oldWcColor2 = (Color)resources["LeftWindowColor2"];
                Color oldCtColor1 = (Color)resources["CheckToggleColor1"];
                Color oldCtColor2 = (Color)resources["CheckToggleColor2"];
                Color oldBrktColor2 = (Color)resources["BracketColor"];
                Color oldTextColor = (Color)resources["TextColor"];
                Color oldTagColor = (Color)resources["TagColor"];

                // 変更後カラーの取得
                var newDict = new ResourceDictionary { Source = new Uri($"/Theme/{theme}Theme.xaml", UriKind.Relative) };

                Color newTbarColor1 = (Color)newDict["TitleBarAnimFrom1"];
                Color newTbarColor2 = (Color)newDict["TitleBarAnimFrom2"];
                Color newThemeColor = (Color)newDict["ButtonThemeColor"];
                Color newAcsColor = (Color)newDict["AccentColor"];
                Color newWcColor1 = (Color)newDict["LeftWindowColor1"];
                Color newWcColor2 = (Color)newDict["LeftWindowColor2"];
                Color newCtColor1 = (Color)newDict["CheckToggleColor1"];
                Color newCtColor2 = (Color)newDict["CheckToggleColor2"];
                Color newBrktColor2 = (Color)newDict["BracketColor"];
                Color newTextColor = (Color)newDict["TextColor"];
                Color newTagColor = (Color)newDict["TagColor"];

                // 既存キーの書き換え
                resources["AccentColor"] = newDict["AccentColor"];
                resources["AccentBrush"] = newDict["AccentBrush"];
                //resources["ButtonThemeColorBrush"] = newDict["ButtonThemeColorBrush"];
                resources["ButtonThemeColor"] = newDict["ButtonThemeColor"];
                resources["ThemeIcon"] = newDict["ThemeIcon"];
                resources["LeftWindowColor1"] = newDict["LeftWindowColor1"];
                resources["LeftWindowColor1Brush"] = newDict["LeftWindowColor1Brush"];
                resources["LeftWindowColor2"] = newDict["LeftWindowColor2"];
                resources["LeftWindowColor2Brush"] = newDict["LeftWindowColor2Brush"];
                //resources["CheckToggleColor1"] = newDict["CheckToggleColor1"];
                //resources["CheckToggleColor2"] = newDict["CheckToggleColor2"];
                resources["BracketColor"] = newDict["BracketColor"];
                resources["BlacketColorBrush"] = newDict["BlacketColorBrush"];
                resources["TextColor"] = newDict["TextColor"];
                resources["TagColor"] = newDict["TagColor"];
                resources["RippleColor"] = newDict["RippleColor"];
                resources["RippleColorBrush"] = newDict["RippleColorBrush"];

                // Storyboard をフィールドに保持
                Storyboard tbStoryboard = new Storyboard();

                var anim1 = new ColorAnimation
                {
                    From = TitleBarGradientStop1.Color,
                    To = newTbarColor1,
                    Duration = TimeSpan.FromSeconds(tBarAnimTransition),
                    AutoReverse = false
                };
                Storyboard.SetTargetName(anim1, "TitleBarGradientStop1");
                Storyboard.SetTargetProperty(anim1, new PropertyPath("Color"));
                tbStoryboard.Children.Add(anim1);

                var anim2 = new ColorAnimation
                {
                    From = TitleBarGradientStop2.Color,
                    To = newTbarColor2,
                    Duration = TimeSpan.FromSeconds(tBarAnimTransition),
                    AutoReverse = false
                };
                Storyboard.SetTargetName(anim2, "TitleBarGradientStop2");
                Storyboard.SetTargetProperty(anim2, new PropertyPath("Color"));
                tbStoryboard.Children.Add(anim2);

                // テーマ移行アニメーション完了と同時にテーマアニメーション開始
                tbStoryboard.Completed += (s, e) =>
                {
                    AnimateTitleBar();
                    if (btn != null)
                    {
                        btn.IsEnabled = true;
                    }
                };

                // 強制開始ではなく Begin(this) だけ
                tbStoryboard.Begin(this);




                /////////////////////////テーマボタンテスト
                // まず、ターゲットButtonを取得
                var button = ThemeToggleButton;

                // 背景ブラシがSolidColorBrushか確認
                if (button.Background is not SolidColorBrush brush)
                    return;

                // 凍結解除（Resource由来Brushは大抵IsFrozen=true）
                if (brush.IsFrozen)
                {
                    brush = brush.Clone();
                    button.Background = brush; // Buttonにクローンを再設定
                }

                // アニメーション作成
                var anim = new ColorAnimation
                {
                    From = oldThemeColor,
                    To = newThemeColor,
                    Duration = TimeSpan.FromSeconds(1),
                    AutoReverse = false
                };

                //// Storyboard作成
                //var sb = new Storyboard();
                //sb.Children.Add(anim);

                //// Buttonそのものをターゲットに指定します
                //Storyboard.SetTarget(anim, button);
                //Storyboard.SetTargetProperty(anim, new PropertyPath("(Button.Background).(SolidColorBrush.Color)"));

                //// これでアニメーションが動作
                //sb.Begin();
                brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);



                ///////////////////////////
                // アクセント共有ブラシを取得
                // アプリ全体のリソースからブラシを取得

                var brush3 = Application.Current.Resources["AccentBrush"] as SolidColorBrush;

                if (brush3 != null)
                {
                    // 凍結されている場合はクローンして再登録
                    if (brush3.IsFrozen)
                    {
                        brush3 = brush3.Clone();
                        Application.Current.Resources["AccentBrush"] = brush3;
                    }

                    // アニメーション作成
                    var anim3 = new ColorAnimation
                    {
                        From = oldAcsColor,
                        To = newAcsColor,
                        Duration = TimeSpan.FromSeconds(1),
                        AutoReverse = false
                    };

                    // アニメーション開始
                    brush3.BeginAnimation(SolidColorBrush.ColorProperty, anim3);
                }



                var brush4 = Application.Current.Resources["LeftWindowColor1Brush"] as SolidColorBrush;

                if (brush4 != null)
                {
                    // 凍結されている場合はクローンして再登録
                    if (brush4.IsFrozen)
                    {
                        brush4 = brush4.Clone();
                        Application.Current.Resources["LeftWindowColor1Brush"] = brush4;
                    }

                    // アニメーション作成
                    var anim4 = new ColorAnimation
                    {
                        From = oldWcColor1,
                        To = newWcColor1,
                        Duration = TimeSpan.FromSeconds(1),
                        AutoReverse = false
                    };

                    // アニメーション開始
                    brush4.BeginAnimation(SolidColorBrush.ColorProperty, anim4);
                }



                var brush5 = Application.Current.Resources["LeftWindowColor2Brush"] as SolidColorBrush;

                if (brush5 != null)
                {
                    // 凍結されている場合はクローンして再登録
                    if (brush5.IsFrozen)
                    {
                        brush5 = brush5.Clone();
                        Application.Current.Resources["LeftWindowColor2Brush"] = brush5;
                    }

                    // アニメーション作成
                    var anim5 = new ColorAnimation
                    {
                        From = oldWcColor2,
                        To = newWcColor2,
                        Duration = TimeSpan.FromSeconds(1),
                        AutoReverse = false
                    };

                    // アニメーション開始
                    brush5.BeginAnimation(SolidColorBrush.ColorProperty, anim5);
                }

                //var brush6 = Application.Current.Resources["BlacketColorBrush"] as SolidColorBrush;

                //if (brush6 != null)
                //{
                //    // 凍結されている場合はクローンして再登録
                //    if (brush6.IsFrozen)
                //    {
                //        brush6 = brush6.Clone();
                //        Application.Current.Resources["BlacketColorBrush"] = brush6;
                //    }

                //    // アニメーション作成
                //    var anim6 = new ColorAnimation
                //    {
                //        From = oldWcColor2,
                //        To = newWcColor2,
                //        Duration = TimeSpan.FromSeconds(1),
                //        AutoReverse = false
                //    };

                //    // アニメーション開始
                //    brush6.BeginAnimation(SolidColorBrush.ColorProperty, anim5);
                //}

                if (pathDatas.paths != null && pathDatas.paths.Count() > 0)
                {
                    foreach (var path in pathDatas.paths)
                    {
                        if (path.Stroke is SolidColorBrush brush6)
                        {
                            // 凍結されている場合はクローンして再登録
                            if (brush6.IsFrozen)
                            {
                                brush6 = brush6.Clone();
                                path.Stroke = brush6;
                            }

                            // アニメーション作成
                            var colorAnim = new ColorAnimation
                            {
                                From = oldBrktColor2,         // 現在色を取得する場合は brush.Color
                                To = newBrktColor2,          // 変更後の色
                                Duration = TimeSpan.FromSeconds(1),
                                AutoReverse = false
                            };

                            // アニメーション開始
                            brush6.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
                        }
                    }
                }

                var brush7 = Application.Current.Resources["TextColorBrush"] as SolidColorBrush;

                if (brush7 != null)
                {
                    // 凍結されている場合はクローンして再登録
                    if (brush7.IsFrozen)
                    {
                        brush7 = brush7.Clone();
                        Application.Current.Resources["TextColorBrush"] = brush7;
                    }

                    // アニメーション作成
                    var anim7 = new ColorAnimation
                    {
                        From = oldTextColor,
                        To = newTextColor,
                        Duration = TimeSpan.FromSeconds(1),
                        AutoReverse = false
                    };

                    // アニメーション開始
                    brush7.BeginAnimation(SolidColorBrush.ColorProperty, anim7);
                }

                var brush8 = Application.Current.Resources["TagColorBrush"] as SolidColorBrush;

                if (brush8 != null)
                {
                    // 凍結されている場合はクローンして再登録
                    if (brush8.IsFrozen)
                    {
                        brush8 = brush8.Clone();
                        Application.Current.Resources["TagColorBrush"] = brush8;
                    }

                    // アニメーション作成
                    var anim8 = new ColorAnimation
                    {
                        From = oldTagColor,
                        To = newTagColor,
                        Duration = TimeSpan.FromSeconds(1),
                        AutoReverse = false
                    };

                    // アニメーション開始
                    brush8.BeginAnimation(SolidColorBrush.ColorProperty, anim8);
                }

                // Brush を取得
                var brush9 = Application.Current.Resources["CheckToggleBrush"] as LinearGradientBrush;
                if (brush9 != null)
                {
                    if (brush9.IsFrozen)
                    {
                        brush9 = brush9.Clone();
                        Application.Current.Resources["CheckToggleBrush"] = brush9;
                    }

                    // GradientStop を取得
                    var gs1 = brush9.GradientStops[0];
                    var gs2 = brush9.GradientStops[1];

                    // アニメーション作成
                    var anim9 = new ColorAnimation(oldCtColor1, newCtColor1, TimeSpan.FromSeconds(1));
                    var anim10 = new ColorAnimation(oldCtColor2, newCtColor2, TimeSpan.FromSeconds(1));

                    anim10.Completed += (o, e) =>
                    {
                        resources["CheckToggleColor1"] = newDict["CheckToggleColor1"];
                        resources["CheckToggleColor2"] = newDict["CheckToggleColor2"];
                    };
                    // GradientStop にアニメーションを適用
                    gs1.BeginAnimation(GradientStop.ColorProperty, anim9);
                    gs2.BeginAnimation(GradientStop.ColorProperty, anim10);
                }

                //var brush9 = Application.Current.Resources["CheckToggleColor1"] as SolidColorBrush;

                //if (brush9 != null)
                //{
                //    // 凍結されている場合はクローンして再登録
                //    if (brush9.IsFrozen)
                //    {
                //        brush9 = brush9.Clone();
                //        Application.Current.Resources["CheckToggleColor1"] = brush9;
                //    }

                //    // アニメーション作成
                //    var anim9 = new ColorAnimation
                //    {
                //        From = oldCtColor1,
                //        To = newCtColor1,
                //        Duration = TimeSpan.FromSeconds(1),
                //        AutoReverse = false
                //    };

                //    // アニメーション開始
                //    brush9.BeginAnimation(SolidColorBrush.ColorProperty, anim9);
                //}

                //var brush10 = Application.Current.Resources["CheckToggleColor2"] as SolidColorBrush;

                //if (brush10 != null)
                //{
                //    // 凍結されている場合はクローンして再登録
                //    if (brush10.IsFrozen)
                //    {
                //        brush10 = brush10.Clone();
                //        Application.Current.Resources["CheckToggleColor2"] = brush10;
                //    }

                //    // アニメーション作成
                //    var anim10 = new ColorAnimation
                //    {
                //        From = oldCtColor2,
                //        To = newCtColor2,
                //        Duration = TimeSpan.FromSeconds(1),
                //        AutoReverse = false
                //    };

                //    // アニメーション開始
                //    brush10.BeginAnimation(SolidColorBrush.ColorProperty, anim10);
                //}
            }
        }

        /// <summary>
        /// Windowsイベント処理
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                // --- リサイズ可能領域の判定 ---
                case WM_NCHITTEST:
                    {
                        Point p = GetMousePosition();
                        var r = new Rect(this.Left, this.Top, this.Width, this.Height);
                        handled = true;

                        if (p.Y >= r.Top && p.Y < r.Top + RESIZE_BORDER)
                        {
                            if (p.X < r.Left + RESIZE_BORDER)
                            {
                                return (IntPtr)HTTOPLEFT;
                            }
                            else if (p.X > r.Right - RESIZE_BORDER)
                            {
                                return (IntPtr)HTTOPRIGHT;
                            }
                            else
                            {
                                return (IntPtr)HTTOP;
                            }
                        }
                        else if (p.Y <= r.Bottom && p.Y > r.Bottom - RESIZE_BORDER)
                        {
                            if (p.X < r.Left + RESIZE_BORDER)
                            {
                                return (IntPtr)HTBOTTOMLEFT;
                            }
                            else if (p.X > r.Right - RESIZE_BORDER)
                            {
                                return (IntPtr)HTBOTTOMRIGHT;
                            }
                            else
                            {
                                return (IntPtr)HTBOTTOM;
                            }
                        }
                        else if (p.X >= r.Left && p.X < r.Left + RESIZE_BORDER)
                        {
                            return (IntPtr)HTLEFT;
                        }
                        else if (p.X <= r.Right && p.X > r.Right - RESIZE_BORDER)
                        {
                            return (IntPtr)HTRIGHT;
                        }
                        else
                        {
                            handled = false;
                        }
                        
                        break;
                    }

                // --- リサイズ開始 ---
                //case WM_NCLBUTTONDOWN:
                //    {
                //        break;
                //    }

                // --- サイズ変更モードに入った瞬間（マウスドラッグ開始確定） ---
                case WM_ENTERSIZEMOVE:
                    isResizing = true;
                    EffectCanvas_tb.Children.Clear();

                    break;

                // --- サイズ変更完了（マウスボタン離した） ---
                case WM_EXITSIZEMOVE:
                    if (isResizing)
                    {
                        isResizing = false;

                        if (pathDatas.tb != null)
                        {
                            ShowCornerBrackets(pathDatas.tb, false);
                        }
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        #region Win32補助
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        /// <summary>
        /// マウス位置取得
        /// </summary>
        /// <returns></returns>
        private Point GetMousePosition()
        {
            GetCursorPos(out POINT p);
            return new Point(p.X, p.Y);
        }
        #endregion
    }
}
