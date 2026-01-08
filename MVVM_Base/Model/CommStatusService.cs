using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MVVM_Base.Model
{
    public class CommStatusService : INotifyPropertyChanged
    { 
        /// <summary>
        /// MFCと接続しているかどうか
        /// </summary>
        private bool isMfcConnected = false;
        public bool IsMfcConnected
        {
            get => isMfcConnected;
            set
            {
                if (isMfcConnected != value)
                {
                    isMfcConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Balanceと接続しているかどうか
        /// </summary>
        private bool isBalanceConnected = false;
        public bool IsBalanceConnected
        {
            get => isBalanceConnected;
            set
            {
                if (isBalanceConnected != value)
                {
                    isBalanceConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
