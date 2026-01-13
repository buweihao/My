using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    // 用于 TabControl 绑定的分组对象
    public class TagGroup
    {
        public string Header { get; set; } // 标题 (如 "PLC_01 (127.0.0.1)")
        public ObservableCollection<BindableTag> Tags { get; set; } = new();
    }

    public class DashboardViewModel
    {
        public ObservableCollection<TagGroup> Pages { get; set; } = new();

        // 只需要注入 Device 列表和 PlcLink
        public DashboardViewModel(List<Device> devices, PlcLink plcLink)
        {
            // --- 1. 生成“重点关注”页 ---
            var favGroup = new TagGroup { Header = "⭐ 重点关注" };

            // 扁平化所有点位
            var allTags = devices.SelectMany(d => d.Tags);

            // ✨ 直接判断属性！
            foreach (var tag in allTags.Where(t => t.IsFavorite))
            {
                favGroup.Tags.Add(plcLink[tag.TagName]);
            }

            if (favGroup.Tags.Any())
            {
                Pages.Add(favGroup);
            }

            // --- 2. 常规设备页 (保持不变) ---
            foreach (var device in devices)
            {
                var group = new TagGroup
                {
                    Header = $"{device.DeviceId}\n{device.IpAddress}"
                };
                foreach (var tag in device.Tags)
                {
                    group.Tags.Add(plcLink[tag.TagName]);
                }
                Pages.Add(group);
            }
        }
    }
}
