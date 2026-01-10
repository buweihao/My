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
        public MainWindow()
        {
            InitializeComponent();
            // 1. 获取管理器实例
            var manager = MyConfig.MyConfigContext.DefaultManager;

            // 注册分类 1：加载 My 开头的所有项
            manager.RegisterCategory("系统参数", key => key.StartsWith("1_")||key.StartsWith("_1"));

            // 注册分类 2：加载 Database 开头 或者 Connection 结尾的项 (复杂的业务逻辑)
            manager.RegisterCategory("数据库配置", key =>
                key.StartsWith("Database") || key.EndsWith("Connection"));

            // 注册分类 3：只精确加载某几个特定的键
            manager.RegisterCategory("关键开关", key =>
                key == "EnableLog" || key == "IsDebugMode");
        }
    }
}