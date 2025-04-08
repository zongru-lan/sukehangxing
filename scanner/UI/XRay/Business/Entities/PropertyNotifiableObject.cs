using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    public class PropertyNotifiableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        //protected static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        //{
        //    if (propertyExpression == null)
        //    {
        //        throw new ArgumentNullException("propertyExpression");
        //    }
        //    MemberExpression expression = propertyExpression as MemberExpression;
        //    if (expression == null)
        //    {
        //        throw new ArgumentException("Invalid argument", "propertyExpression");
        //    }
        //    PropertyInfo info = expression as PropertyInfo;
        //    if (info == null)
        //    {
        //        throw new ArgumentException("Argument is not a property", "propertyExpression");
        //    }
        //    return info.Name;
        //}

        //protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        //{
        //    PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        //    if (propertyChanged != null)
        //    {
        //        string propertyName = GetPropertyName<T>(propertyExpression);
        //        propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        //    }
        //}

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //protected bool Set<T>(string propertyName, ref T field, T newValue)
        //{
        //    if (EqualityComparer<T>.get_Default().Equals(field, newValue))
        //    {
        //        return false;
        //    }
        //    field = newValue;
        //    this.RaisePropertyChanged(propertyName);
        //    return true;
        //}

        //protected bool Set<T>(Expression<Func<T>> propertyExpression, ref T field, T newValue)
        //{
        //    if (EqualityComparer<T>.get_Default().Equals(field, newValue))
        //    {
        //        return false;
        //    }
        //    field = newValue;
        //    this.RaisePropertyChanged<T>(propertyExpression);
        //    return true;
        //}

        //protected bool Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        //{
        //    return this.Set<T>(propertyName, ref field, newValue);
        //}

        [Conditional("DEBUG"), DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            Type type = base.GetType();
            if (!string.IsNullOrEmpty(propertyName) &&
                (IntrospectionExtensions.GetTypeInfo(type).GetDeclaredProperty(propertyName) == null))
            {
                throw new ArgumentException("Property not found", propertyName);
            }
        }

        protected PropertyChangedEventHandler PropertyChangedHandler
        {
            get { return this.PropertyChanged; }
        }
    }
}
