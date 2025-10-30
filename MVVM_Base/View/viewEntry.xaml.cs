using MVVM_Base.ViewModel;
using System.Windows;

namespace MVVM_Base.View
{
    /// <summary>
    /// Interaction logic for viewEntry.xaml
    /// </summary>
    public partial class viewEntry : Window
    {
        public viewEntry(vmEntry m_vmEntry)
        {
            InitializeComponent();
            DataContext = m_vmEntry;
        }
    }
}