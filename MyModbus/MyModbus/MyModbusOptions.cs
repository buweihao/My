using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    public class MyModbusOptions
    {
        /// <summary>
        /// 定义如何判断一个 Tag 是否为“重点关注”点位
        /// 返回 true 则显示在首页，false 则不显示
        /// </summary>
        public Func<Tag, bool> IsFavorite { get; set; } = _ => false; // 默认不关注任何点位
    }
}
