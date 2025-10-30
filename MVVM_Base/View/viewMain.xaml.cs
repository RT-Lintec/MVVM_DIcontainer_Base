using MVVM_Base.ViewModel;
using System.Windows.Controls;

namespace MVVM_Base.View
{
    /// <summary>
    /// viewMain.xaml の相互作用ロジック
    /// </summary>
    public partial class viewMain : UserControl
    {
        public viewMain(vmMain m_vmMainView)
        {
            InitializeComponent();
            DataContext = m_vmMainView;
        }
    }
}
