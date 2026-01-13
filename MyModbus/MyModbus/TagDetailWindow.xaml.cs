
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyModbus
{
    /// <summary>
    /// TagDetailWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TagDetailWindow : Window
    {
        private BindableTag _vm;
        private DataCollectionEngine _engine; // 本地持有
        public TagDetailWindow(BindableTag vm, DataCollectionEngine engine)
        {
            InitializeComponent();
            _vm = vm;
            _engine = engine;
            this.DataContext = vm;
        }
        private void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            string input = WriteBox.Text;
            string tagName = _vm.Config.TagName;

            try
            {
                // 简单类型转换 (实际建议根据 DataType 做严格转换)
                object valToWrite = input;

                // 调用全局引擎写入
                // 假设你有一个全局可访问的 Global.Engine
                bool success = _engine.WriteTag(tagName, valToWrite);
                if (success)
                {
                    MsgText.Text = "写入指令已下发";
                    MsgText.Foreground = Brushes.Green;
                }
                else
                {
                    MsgText.Text = "写入失败 (断线或驱动拒绝)";
                    MsgText.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MsgText.Text = "输入格式错误: " + ex.Message;
            }
        }   
    }
}
