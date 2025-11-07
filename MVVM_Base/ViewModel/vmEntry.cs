using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.DiContainer;
using MVVM_Base.View;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MVVM_Base.ViewModel
{
    public enum ViewType
    {
        Main,
        AView,
        BView,
        Help,
        End
    }

    public partial class vmEntry : ObservableObject
    {

        [ObservableProperty]
        private UserControl _currentView;

        /// <summary>
        /// Mainボタン選択
        /// </summary>
        private bool _isMainSelected;
        public bool IsMainSelected
        {
            get => _isMainSelected;
            set => SetProperty(ref _isMainSelected, value);
        }

        /// <summary>
        /// A_Viewボタン選択
        /// </summary>
        private bool _isAViewSelected;
        public bool IsAViewSelected
        {
            get => _isAViewSelected;
            set => SetProperty(ref _isAViewSelected, value);
        }

        /// <summary>
        /// B_Viewボタン選択
        /// </summary>
        private bool _isBViewSelected;
        public bool IsBViewSelected
        {
            get => _isBViewSelected;
            set => SetProperty(ref _isBViewSelected, value);
        }

        /// <summary>
        /// Helpボタン選択
        /// </summary>
        private bool _IsHelpSelected;
        public bool IsHelpSelected
        {
            get => _IsHelpSelected;
            set => SetProperty(ref _IsHelpSelected, value);
        }

        /// <summary>
        /// Endボタン選択
        /// </summary>
        private bool _IsEndSelected;
        public bool IsEndSelected
        {
            get => _IsEndSelected;
            set => SetProperty(ref _IsEndSelected, value);
        }

        /// <summary>
        /// 選択されたViewタイプと一致するbooleanをtrueにする→Viewに通知がいく
        /// </summary>
        /// <param name="type"></param>
        public void SelectView(ViewType type)
        {
            IsMainSelected = type == ViewType.Main;
            IsAViewSelected = type == ViewType.AView;
            IsBViewSelected = type == ViewType.BView;
            IsHelpSelected = type == ViewType.Help;
            IsEndSelected = type == ViewType.End;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public vmEntry() { }

        /// <summary>
        /// ビュー切り替え
        /// </summary>
        /// <param name="type"></param>
        [RelayCommand]
        private void ShowView(ViewType type)
        {
            // 選択状態を一括で更新
            SelectView(type);

            // CurrentView 切り替え
            CurrentView = type switch
            {
                ViewType.Main => diRoot.Instance.GetService<viewMain>(),
                ViewType.AView => diRoot.Instance.GetService<viewA>(),
                ViewType.BView => diRoot.Instance.GetService<viewB>(),
                _ => CurrentView
            };
        }

        [RelayCommand]
        private void New()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }

        [RelayCommand]
        private void Open()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }

        [RelayCommand]
        private void Save()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }

        [RelayCommand]
        private void Exit(ViewType type)
        {
            // 選択状態を一括で更新
            SelectView(type);

            // Viewの終了処理なので、本来はViewに持たせたい
            if (MessageBox.Show("End?", "確認", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        [RelayCommand]
        private void Undo()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }

        [RelayCommand]
        private void Redo()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }

        [RelayCommand]
        private void Copy()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }

        [RelayCommand]
        private void Paste()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }

        [RelayCommand]
        private void About()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }
    }
}