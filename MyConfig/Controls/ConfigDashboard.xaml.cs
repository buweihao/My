using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyConfig.Controls
{
    public partial class ConfigDashboard : UserControl
    {
        private readonly IConfigWindowManager _manager;

        public List<ConfigItemModel> Categories { get; set; }

        public ConfigDashboard(List<ConfigItemModel> categories, IConfigWindowManager manager)
        {
            InitializeComponent();
            Categories = categories;
            _manager = manager;
            DataContext = this;
        }

        // [修改] 命令接收 Func<string, bool> 参数
        private ICommand _openItemCommand;
        public ICommand OpenItemCommand => _openItemCommand ??= new RelayCommand<object>(obj =>
        {
            // 安全地将 object 转换为所需的委托类型
            if (obj is Func<string, bool> filter)
            {
                OpenItem(filter);
            }
        });

        private void OpenItem(Func<string, bool> filter)
        {
            _manager.ShowConfigDialog(filter);
        }
        // [新增] 关闭按钮点击事件
        private void OnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Window.GetWindow(this)?.Close();
        }
    }
}