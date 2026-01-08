using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Base.ViewModel
{
    public interface IViewModel : IDisposable
    {
        /// <summary>
        /// 終了可否
        /// </summary>
        public bool canQuit { get; set; }

        /// <summary>
        /// 画面遷移可否
        /// </summary>
        public bool canTransitOther { get; set; }
}
}
