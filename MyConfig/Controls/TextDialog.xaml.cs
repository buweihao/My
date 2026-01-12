using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using MyConfig;
// using Newtonsoft.Json.Linq; // 1. 移除这个引用，UI 层不再需要知道 Json
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyConfig.Controls;

public partial class TextDialog : UserControl
{
    private readonly IConfigService _configService;

    // 数据源
    public ObservableCollection<IpNode> IpNodes { get; set; } = new();

    // 参数属性
    public string Parameter { get; set; }

    public TextDialog(Func<string, bool> filter, IConfigService configService)
    {
        InitializeComponent();
        _configService = configService;

        // 直接把 filter 传给 Service，不在这里做任何字符串判断
        var items = _configService.GetConfigItems(filter);

        foreach (var item in items)
        {
            IpNodes.Add(new IpNode { Key = item.Key, Value = item.Value });
        }

        DataContext = this;
    }

    // ---------------------------------------------------------
    // 【已删除】LoadIpNodes 方法
    // 原因：旧逻辑直接依赖 JObject，现已由上面的 GetConfigItems 替代
    // ---------------------------------------------------------

    // 确认保存命令
    private ICommand _confirmCommand;
    public ICommand ConfirmCommand => _confirmCommand ??= new RelayCommand<ObservableCollection<IpNode>>(OnConfirm);

    //private void OnConfirm(ObservableCollection<IpNode>? nodes)
    //{
    //    if (nodes == null) return;

    //    // 1. 保存配置逻辑 (保持不变)
    //    foreach (var node in nodes)
    //    {
    //        _configService.SetConfig(node.Key, node.Value);
    //    }
    //    _configService.SaveConfig();

    //    // 2. [修改这里] 使用 WPF 标准方式关闭父窗口
    //    // 原代码: HandyControl.Interactivity.ControlCommands.Close.Execute(null, this);

    //    // 新代码: 显式获取父窗口并关闭
    //    var parentWindow = System.Windows.Window.GetWindow(this);
    //    parentWindow?.Close();

    //    // 补充说明: 
    //    // 如果您的意图不是关闭整个窗口，而是"返回"到上一个面板(Dashboard)，
    //    // 可以使用: this.NavigationService?.GoBack();
    //}
    // [新增] 通用的关闭/取消事件
    private void OnClose_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        System.Windows.Window.GetWindow(this)?.Close();
    }

    // [确认] 之前的确认逻辑也应确保是这样关闭的
    private void OnConfirm(ObservableCollection<IpNode>? nodes)
    {
        if (nodes == null) return;
        foreach (var node in nodes)
        {
            _configService.SetConfig(node.Key, node.Value);
        }
        _configService.SaveConfig();

        // 确保这里也是用的 GetWindow(this)?.Close()
        System.Windows.Window.GetWindow(this)?.Close();
    }
}

// VM 类保持不变
public class IpNode : INotifyPropertyChanged
{
    private string _value;

    public string Key { get; set; }
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}