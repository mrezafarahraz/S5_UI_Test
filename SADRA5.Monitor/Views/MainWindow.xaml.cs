using System.Windows;
using SADRA5.Monitor.ViewModels;

namespace SADRA5.Monitor.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
