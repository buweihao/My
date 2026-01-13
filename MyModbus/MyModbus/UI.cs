
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyModbus
{
    /// <summary>
    /// 专门用于 WPF 绑定的 Tag 对象
    /// </summary>
    public class BindableTag : INotifyPropertyChanged
    {
        private readonly DataCollectionEngine _engine; // 持有引擎
        public Tag Config { get; private set; }

        // 【新增】详情弹窗命令
        public ICommand OpenDetailCommand { get; private set; }

        public BindableTag(Tag config, DataCollectionEngine engine)
        {
            Config = config;
            _engine = engine;
            OpenDetailCommand = new RelayCommand(OpenDetailWindow);
        }

        // 打开详情窗口的逻辑
        private void OpenDetailWindow(object obj)
        {
            // 将 engine 传给弹窗，或者直接把 this 传给弹窗
            var win = new TagDetailWindow(this, _engine);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }
        private object _value;
        private bool _isGood = true;

        // 1. 数值 (供 TextBlock 绑定)
        public object Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        // 2. 质量 (供 IsEnabled 或 颜色触发器 绑定)
        public bool IsGood
        {
            get => _isGood;
            set { _isGood = value; OnPropertyChanged(); }
        }

        // --- INPC 实现 ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 核心更新方法：把 DataBus 的数据映射过来
        /// 注意：必须切回 UI 线程！
        /// </summary>
        public void Update(TagData data)
        {
            // 使用 Application.Current.Dispatcher 确保在 UI 线程更新
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Value = data.Value;
                IsGood = data.IsQualityGood;
            });
        }

    }

    public class PlcLink
    {
        private readonly DataCollectionEngine _engine; // 新增持有
        private readonly DataBus _bus;
        // 缓存所有前端请求过的 Tag
        private readonly Dictionary<string, BindableTag> _uiTags = new();
        private readonly Dictionary<string, Tag> _configLookup;
        // 【修改】构造函数，强制要求传入 configLookup
        public PlcLink(DataBus bus, Dictionary<string, Tag> configLookup, DataCollectionEngine engine)
        {
            _bus = bus;
            _configLookup = configLookup;
            _engine = engine; // 保存
        }
        /// <summary>
        /// 核心索引器：让 XAML 可以写 {Binding PlcData[TagName]}
        /// </summary>
        public BindableTag this[string tagName]
        {
            get
            {
                if (!_uiTags.ContainsKey(tagName))
                {
                    // 1. 先尝试从配置表中找到 Tag 定义
                    if (_configLookup.TryGetValue(tagName, out Tag config))
                    {
                        // 找到了：创建带身份证的 Tag
                        var newTag = new BindableTag(config, _engine);
                        InitTagSubscription(tagName, newTag);
                        _uiTags[tagName] = newTag;
                    }
                    else
                    {
                        // 没找到 (可能是XAML拼写错误)：
                        // 创建一个空的“虚假”Tag，防止程序崩溃，并在界面显示错误提示
                        var dummyConfig = new Tag
                        {
                            TagName = tagName,
                            Address = "N/A",
                            Description = "配置未找到"
                        };
                        var dummyTag = new BindableTag(dummyConfig, _engine);
                        dummyTag.Value = "配置丢失"; // 提示给操作员
                        dummyTag.IsGood = false;

                        _uiTags[tagName] = dummyTag;
                    }
                }
                return _uiTags[tagName];
            }
        }
        private void InitTagSubscription(string tagName, BindableTag tagObj)
        {
            // 尝试获取 DataBus 的初始值
            var initData = _bus.GetTagData(tagName);
            if (initData.HasValue)
            {
                tagObj.Update(initData.Value);
            }

            // 订阅更新
            _bus.Subscribe(tagName, (data) =>
            {
                tagObj.Update(data);
            });
        }
    }
    /// <summary>
    /// 一个用于将 UI 命令绑定到 ViewModel 方法的辅助类
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="execute">要执行的逻辑 (比如 OpenDetailWindow)</param>
        /// <param name="canExecute">判断命令是否可用 (可选，返回 true/false)</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 判断命令当前是否可以执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            // 如果没有设置 canExecute 逻辑，默认总是允许执行
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// 当 CanExecute 的状态可能发生改变时触发
        /// 利用 CommandManager 自动感知 UI 交互状态
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

}
