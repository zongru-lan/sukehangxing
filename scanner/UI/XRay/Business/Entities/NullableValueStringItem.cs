using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 值可以为null的字符串。如bool?以及 enumType?、int?等
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NullableValueStringItem<T>
    {
        /// <summary>
        /// 构造一个实例
        /// </summary>
        /// <param name="t">实际值，可能为null</param>
        /// <param name="str">t对应的字符串表示</param>
        public NullableValueStringItem(T t, string str)
        {
            Value = t;
            String = str;
        }

        public NullableValueStringItem(T t)
        {
            Value = t;
            String = t == null ? string.Empty : t.ToString();
        }

        /// <summary>
        /// 实际取值，可以为null
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// 可视化字符串表示
        /// </summary>
        public string String { get; set; }
    }

}
