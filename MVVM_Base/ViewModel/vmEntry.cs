using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using MVVM_Base.DiContainer;
using MVVM_Base.View;

namespace MVVM_Base.ViewModel
{
    public partial class vmEntry : ObservableObject
    {
        public vmEntry(){}

        [ObservableProperty]
        private UserControl _currentView;

        [RelayCommand]
        private void ShowMainView()
        {
            CurrentView = diRoot.Instance.GetService<viewMain>();
        }

        [RelayCommand]
        private void ShowAView()
        {
            CurrentView = diRoot.Instance.GetService<viewA>();
        }

        [RelayCommand]
        private void ShowBView()
        {
            CurrentView = diRoot.Instance.GetService<viewB>();
        }
    }
}