using Core;
using HandyControl;
using HandyControl.Controls;
using MyConfig;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Input;

namespace MyConfig.Controls;

public partial class TextDialog
{
    ConfigHelper configHelper;
    public TextDialog(string parameter, ConfigHelper _configHelper)
    {
        InitializeComponent();
        Parameter = parameter;
        DataContext = this;
        configHelper = _configHelper;
        LoadIpNodes();
    }

    public ICommand ConfirmCommand
    {
        get => (ICommand)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public string Parameter { get; set; }

    public static readonly DependencyProperty ConfirmCommandProperty =
        DependencyProperty.Register(nameof(ConfirmCommand), typeof(ICommand), typeof(TextDialog), new PropertyMetadata(null));
    public ObservableCollection<IpNode> IpNodes { get; set; } = new();

    private void LoadIpNodes()
    {
        if (configHelper != null)
        {
            foreach (var property in configHelper._configJson.Properties())
            {
                string key = property.Name;
                if (key.StartsWith($"_{Parameter}上")|| key.StartsWith($"_{Parameter}下") || key.StartsWith($"_{Parameter}翻") || key.StartsWith($"_{Parameter}周") || key.StartsWith($"{Parameter}_") )
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

public static class MyConfigCommand
{
    public static ConfigHelper? configHelper;
    private static HandyControl.Controls.Dialog dialog;
    public static void ShowText(string element)
    {
        if (configHelper == null)
            throw new ArgumentNullException(nameof(configHelper) + "还没有将configHelper传入");
        dialog = HandyControl.Controls.Dialog.Show(new TextDialog(element, configHelper) { ConfirmCommand = ConfirmCommand });
    }
    public static event Action? Confirmed;

    public static ICommand ConfirmCommand => new RelayCommand<object>(OnConfirm);

    private static void OnConfirm(object param)
    {
        if (param is ObservableCollection<IpNode> nodes)
        {
            foreach (var node in nodes)
            {
                configHelper._configJson[node.Key] = node.Value;
            }
            configHelper.SaveConfig();
            dialog.Close();

            // 👇 触发事件
            Confirmed?.Invoke();
        }
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
