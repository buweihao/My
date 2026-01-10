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

    private void OnConfirm(ObservableCollection<IpNode>? nodes)
    {
        if (nodes == null) return;

        // 遍历界面上的数据，写回 Service
        foreach (var node in nodes)
        {
            _configService.SetConfig(node.Key, node.Value);
        }

        // 持久化保存
        _configService.SaveConfig();

        // 关闭弹窗
        HandyControl.Interactivity.ControlCommands.Close.Execute(null, this);
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