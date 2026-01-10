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
        // 2. 定义 ConfigCategory 依赖属性 (缺失的部分)
        //    这是传给 ShowConfigDialog 的参数，比如 "My" 或 "Database"
        // ==========================================
        public static readonly DependencyProperty ConfigCategoryProperty =
            DependencyProperty.Register(
                nameof(ConfigCategory),
                typeof(string),
                typeof(ConfigButton),
                new PropertyMetadata("My")); // 默认值为 "My"

        public string ConfigCategory
        {
            get { return (string)GetValue(ConfigCategoryProperty); }
            set { SetValue(ConfigCategoryProperty, value); }
        }

        // ==========================================
        // 3. 构造函数与逻辑
        // ==========================================
        public ConfigButton()
        {
            InitializeComponent();
        }

        // 按钮点击事件处理
        // (请确保 XAML 中的 Button 绑定了这个事件: Click="OnButtonClick")
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (WindowManager != null)
            {
                // 现在 ConfigCategory 可以正常访问了
                WindowManager.ShowConfigDialog(ConfigCategory);
            }
            else
            {
                MessageBox.Show("错误：WindowManager 未注入。\n请在 MainWindow 中设置：MyConfigBtn.WindowManager = ...", "配置错误");
            }
        }
    }
}