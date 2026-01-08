using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Base.Model
{
    public class ApplicationStatusService : INotifyPropertyChanged
    {
        /// <summary>
        /// 終了ボタンが押されたかどうか
        /// </summary>
        private bool isQuit = false;
        public bool IsQuit
        {
            get => isQuit;
            set
            {
                if (isQuit != value)
                {
                    isQuit = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
