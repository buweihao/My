using System.Windows;
using System.Windows.Controls;
using MyConfig; // 确保引用了定义 IConfigWindowManager 的命名空间

namespace MyConfig.Controls
{
    public partial class ConfigButton : UserControl
    {
        // ==========================================
        // 1. 定义 WindowManager 依赖属性 (用于注入服务)
        // ==========================================
        public static readonly DependencyProperty WindowManagerProperty =
            DependencyProperty.Register(
                nameof(WindowManager),
                typeof(IConfigWindowManager),
                typeof(ConfigButton),
                new PropertyMetadata(null));

        public IConfigWindowManager WindowManager
        {
            get { return (IConfigWindowManager)GetValue(WindowManagerProperty); }
            set { SetValue(WindowManagerProperty, value); }
        }


        // ==========================================
        // 3. 构造函数与逻辑
        // ==========================================
        public ConfigButton()
        {
            InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            // 如果没有注入，尝试使用上下文默认值
            var manager = WindowManager ?? MyConfigContext.DefaultManager;

            if (manager != null)
            {
                // [修改] 改为打开管理面板
                manager.ShowManagementPanel();
            }
            else
            {
                MessageBox.Show("配置服务未初始化。");
            }
        }
    }
}