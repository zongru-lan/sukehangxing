using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UI.XRay.Gui.Framework
{
    /// <summary>  
    /// 为PasswordBox控件的Password增加绑定功能
    /// 此类是从网上找到的，用的较多的
    /// </summary>  
    public static class PasswordBoxHelper
    {
        /// <summary>
        /// 密码依赖项属性，用于绑定，解决PasswordBox没有这个依赖项属性而无法绑定的问题
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password",
            typeof(string), typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));
        /// <summary>
        /// 表示该类是否依附到别的控件（PasswordBox），是一个依赖项属性，需要设置为true，才能依附到目标控件（PasswordBox）
        /// </summary>
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach",
            typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(false, Attach));
        /// <summary>
        /// 表示绑定之间的数据变换是否正在更新
        /// </summary>
        private static readonly DependencyProperty IsUpdatingProperty =
           DependencyProperty.RegisterAttached("IsUpdating", typeof(bool),
           typeof(PasswordBoxHelper));

        /// <summary>
        /// 设置依赖项属性（是否依附到别的控件）的值
        /// </summary>
        /// <param name="dp"></param>
        /// <param name="value"></param>
        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        /// <summary>
        /// 获取依赖项属性（是否依附到别的控件）的值
        /// </summary>
        /// <param name="dp"></param>
        /// <returns></returns>
        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        /// <summary>
        /// 获取依赖项属性密码的值
        /// </summary>
        /// <param name="dp"></param>
        /// <returns></returns>
        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }

        /// <summary>
        /// 设置依赖项属性密码的值
        /// </summary>
        /// <param name="dp"></param>
        /// <param name="value"></param>
        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        /// <summary>
        /// 获取依赖项属性是否在更新数据的值
        /// </summary>
        /// <param name="dp"></param>
        /// <returns></returns>
        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        /// <summary>
        /// 设置依赖项属性是否在更新数据的值
        /// </summary>
        /// <param name="dp"></param>
        /// <param name="value"></param>
        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        /// <summary>
        /// 依赖项属性密码的值变化处理函数，根据当前密码是否在跟新，来设置密码的值，只有为在更新时，才能设置，防止冲突
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            passwordBox.PasswordChanged -= PasswordChanged;
            if (!GetIsUpdating(passwordBox))
            {
                passwordBox.Password = (string)e.NewValue;
            }
            passwordBox.PasswordChanged += PasswordChanged;
        }

        /// <summary>
        /// Attach属性变化处理函数，主要用于passwordbox的密码变化事件添加和删除事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox == null)
                return;
            if ((bool)e.OldValue)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }
            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        /// <summary>
        /// passwordbox的密码变化事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }
    }
}
