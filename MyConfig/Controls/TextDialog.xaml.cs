using CommunityToolkit.Mvvm.Input;
using HandyControl;
using HandyControl.Controls;
using MyConfig;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyConfig.Controls;

public partial class TextDialog : UserControl
{
    // 1. 改为依赖接口
    private readonly IConfigService _configService;

    // 构造函数注入接口
    public TextDialog(string parameter, IConfigService configService)
    {
        InitializeComponent();
        Parameter = parameter;
        DataContext = this;
        _configService = configService; // 保存接口引用
        LoadIpNodes();
    }

    public string Parameter { get; set; }
    public ObservableCollection<IpNode> IpNodes { get; set; } = new();

    // 2. 加载逻辑：通过接口获取数据
    private void LoadIpNodes()
    {
        if (_configService != null)
        {
            // 将 object 类型的 RawJsonConfig 转换为 JObject 以便遍历
            // 注意：这假设底层实现确实是基于 Json.NET 的，如果为了彻底解耦，可以在接口中增加 GetNodes 方法
            if (_configService.RawJsonConfig is JObject json)
            {
                foreach (var property in json.Properties())
                {
                    string key = property.Name;
                    // 你的过滤逻辑保持不变
                    if (key.StartsWith($"_{Parameter}上") || key.StartsWith($"_{Parameter}下") || key.StartsWith($"_{Parameter}翻") || key.StartsWith($"_{Parameter}周") || key.StartsWith($"{Parameter}_"))
                    {
                        IpNodes.Add(new IpNode
                        {
                            Key = key,
                            Value = property.Value?.ToString()
                        });
                    }
                }
            }
        }
    }

    // 3. 实现确认保存命令
    // 对应 XAML 中的 Command="{Binding DataContext.ConfirmCommand...}"
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

        // 关闭弹窗 (使用 HandyControl 的命令关闭当前 Dialog)
        HandyControl.Interactivity.ControlCommands.Close.Execute(null, this);
    }
}

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
