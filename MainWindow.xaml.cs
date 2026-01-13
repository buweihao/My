using MyConfig;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace My
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HotKeyManager _hotKeyManager;
        private MainViewModel _vm;
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            _vm = vm;
            _hotKeyManager = new HotKeyManager(this);
            MyHotKeyInit();
        }

        private void MyHotKeyInit()
        {
            _hotKeyManager.Register(Key.F12, () =>
            {
                MyConfigContext.ShowPanel();
            });
            _hotKeyManager.Register(Key.F11, () =>
            {
                _vm.OpenDashboardCommand.Execute(null);
            });
        }


    }
}