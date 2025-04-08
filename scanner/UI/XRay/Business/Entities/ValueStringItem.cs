using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 可用于绑定的值-字符串组合项，支持属性绑定
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueStringItem<T> : PropertyNotifiableObject
    {
        public ValueStringItem(T val, string str)
        {
            Value = val;
            Str = str;
        }

        public ValueStringItem(T val)
        {
            Value = val;
            Str = val.ToString();
        }

        private T _val;

        private string _str;

        /// <summary>
        /// 实际取值，可以为null
        /// </summary>
        public T Value
        {
            get { return _val; }
            set
            {
                _val = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 可视化字符串表示
        /// </summary>
        public string Str
        {
            get { return _str; }
            set { _str = value; RaisePropertyChanged();}
        }
    }
}
