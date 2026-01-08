using MVVM_Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MVVM_Base.Model
{
    /// <summary>
    /// vmを管理する。
    /// アプリ終了時に各vmのDispose()を呼ぶ
    /// App.xamlから呼ぶ
    /// </summary>
    public class ViewModelManagerService : INotifyPropertyChanged
    {
        private readonly List<IViewModel> viewModels = new();

        /// <summary>
        /// 終了ボタンが押されたかどうか
        /// </summary>
        private int canQuit = 0;
        public int CanQuit
        {
            get => canQuit;
            set
            {
                if (canQuit != value)
                {
                    canQuit = value;
                    CheckCanQuit();
                }
            }
        }

        /// <summary>
        /// 遷移可能かどうか
        /// </summary>
        private bool canTransit = true;
        public bool CanTransit
        {
            get => canTransit;
            set
            {
                if (canTransit != value)
                {
                    canTransit = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 全vmの終了可否を確認し、OKならアプリ終了
        /// </summary>
        public void CheckCanQuit()
        {
            int cnt = 0;
            foreach (var model in viewModels) 
            { 
                if(model.canQuit)
                {
                    cnt++;
                }
            }

            // 全てのvmが終了OKなら終了
            if(cnt == viewModels.Count)
            {
                Application.Current.Shutdown();
            }

            canQuit = 0;
        }

        /// <summary>
        /// vmの登録処理
        /// </summary>
        /// <param name="vm"></param>
        public void Register(object vm)
        {
            if (vm is IViewModel d)
                viewModels.Add(d);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
