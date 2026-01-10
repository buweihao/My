using HandyControl.Controls;
using MyConfig.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyConfig
{
    public interface IConfigWindowManager
    {
        void ShowConfigDialog(string parameter);
    }

    public class ConfigWindowManager : IConfigWindowManager
    {
        private readonly IConfigService _configService;

        // 通过构造函数注入配置服务
        public ConfigWindowManager(IConfigService configService)
        {
            _configService = configService;
        }

        public void ShowConfigDialog(string parameter)
        {
            // 此时我们需要稍微修改 TextDialog 的构造函数以接收接口
            // 或者在这里进行强制类型转换 (如果 TextDialog 逻辑暂时没法大改)
            var helper = _configService as ConfigHelper;

            if (helper == null) throw new System.Exception("Service implementation mismatch");

            // 使用 HandyControl 的 Dialog 显示
            // 注意：这里不再依赖静态的 MyConfigCommand
            Dialog.Show(new TextDialog(parameter, helper)
            {
                // 你可以在这里绑定保存逻辑，或者在 TextDialog 内部通过接口调用 Save
            });
        }
    }
}
