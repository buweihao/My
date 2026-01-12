using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace My
{
    public class HotKeyManager
    {
        private readonly Window _window;
        // 存储快捷键组合与对应动作的映射
        private readonly Dictionary<(Key Key, ModifierKeys Modifiers), Action> _actions = new();

        public HotKeyManager(Window window)
        {
            _window = window;
            // 监听 PreviewKeyDown 可以优先捕获按键，避免被焦点控件(如TextBox)拦截
            _window.PreviewKeyDown += OnPreviewKeyDown;
        }

        /// <summary>
        /// 注册一个快捷键功能
        /// </summary>
        /// <param name="key">主按键 (如 Key.F1, Key.A)</param>
        /// <param name="modifiers">修饰键 (如 ModifierKeys.Control)</param>
        /// <param name="action">要执行的代码块</param>
        public void Register(Key key, ModifierKeys modifiers, Action action)
        {
            var combo = (key, modifiers);

            // 如果重复注册，覆盖旧的，或者改为多播委托 _actions[combo] += action;
            if (_actions.ContainsKey(combo))
            {
                _actions[combo] = action;
            }
            else
            {
                _actions.Add(combo, action);
            }
        }

        /// <summary>
        /// 注册无修饰键的快捷键 (如 F1, F5)
        /// </summary>
        public void Register(Key key, Action action)
        {
            Register(key, ModifierKeys.None, action);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 1. 获取当前实际按下的键
            // 注意：当按住 Alt 时，WPF 会将 Key 识别为 System，实际键在 SystemKey 里
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // 2. 过滤掉纯修饰键的按下（比如只按了 Ctrl 不触发）
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // 3. 获取当前的修饰键状态
            ModifierKeys modifiers = Keyboard.Modifiers;

            // 4. 查找并执行
            if (_actions.TryGetValue((key, modifiers), out var action))
            {
                action?.Invoke();

                // 可选：标记事件已处理，防止继续冒泡（例如防止触发菜单栏）
                // e.Handled = true; 
            }
        }

        // 移除监听，防止内存泄漏（如果 Window 关闭时 Manager 还在生命周期内）
        public void Dispose()
        {
            _window.PreviewKeyDown -= OnPreviewKeyDown;
            _actions.Clear();
        }
    }
}
