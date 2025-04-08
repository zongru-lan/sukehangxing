using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;
using UI.Common.Tracers;
using UI.XRay.Common.Utilities;

namespace UI.XRay.Business.DataAccess.Config
{
    /// <summary>
    /// X-Ray scanner configuration
    /// </summary>
    public class RegistryConfigProvider : IConfigProvider, IDisposable
    {
        /// <summary>
        /// 注册表路径下的公司名下的产品名称
        /// </summary>
        public const string XRayScannerRoot = "HTXRayScanner";

        #region 注册表监听及变更事件通知

        /// <summary>
        /// 监测注册表的配置
        /// </summary>
        private RegistryMonitor _monitor;

        /// <summary>
        /// 弱事件
        /// </summary>
        private SmartWeakEvent<EventHandler> _event = new SmartWeakEvent<EventHandler>();

        /// <summary>
        /// 弱事件：某一配置项发生变化。订阅此事件后，如果不主动注销，也不会导致内存泄露
        /// </summary>
        public event EventHandler ConfigChanged
        {
            add { _event.Add(value); }
            remove { _event.Remove(value); }
        }

        public RegistryConfigProvider()
        {
            Tracer.TraceEnterFunc("UI.XRay.Business.Logic.RegistryConfigProvider constructor");

            try
            {
                var regKey = OpenRootKeyForWrite();
                _monitor = new RegistryMonitor(regKey);
                _monitor.Error += MonitorOnError;
                _monitor.RegChanged += MonitorOnRegChanged;
                _monitor.Start();
             
                regKey.Close();
                regKey.Dispose();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Failed to construct RegistryConfigProvider");
            }

            Tracer.TraceExitFunc("UI.XRay.Business.Logic.RegistryConfigProvider constructor");
        }

        /// <summary>
        /// 打开配置项所属的根键，用于修改或写入
        /// </summary>
        /// <returns></returns>
        private RegistryKey OpenRootKeyForWrite()
        {
            var software = Registry.LocalMachine.OpenSubKey("Software", true);
            if (software != null)
            {
                var root = software.OpenSubKey(XRayScannerRoot, true);
                if (root == null)
                {
                    root = software.CreateSubKey(XRayScannerRoot, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                return root;
            }

            return null;
        }

        /// <summary>
        /// 以只读方式打开根键
        /// </summary>
        /// <returns></returns>
        private RegistryKey OpenRootKeyForRead()
        {
            var software = Registry.LocalMachine.OpenSubKey("Software");
            if (software != null)
            {
                return software.OpenSubKey(XRayScannerRoot);
            }

            return null;
        }

        /// <summary>
        /// 在注册表监听线程中，激发注册表中的配置发生变化的事件。
        /// </summary>
        private void RaiseConfigChangedEvent()
        {
            _event.Raise(null, EventArgs.Empty);
        }

        #endregion 注册表监听及变更事件通知

        public bool Read(string configPath, out ushort value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;


                if (Read(configPath, out valueOut, out kind) && valueOut != null)
                {
                    if (kind == RegistryValueKind.DWord)
                    {
                        //value = (ushort)(valueOut);
                        UInt16.TryParse(valueOut.ToString(), out value);
                        return true;
                    }

                    if (kind == RegistryValueKind.QWord)
                    {
                        value = (ushort)(Int64)(valueOut);
                        Tracer.TraceWarning("Try to read 64bit integer as 32bit from registry:", configPath, valueOut);
                        return true;
                    }

                    if (kind == RegistryValueKind.String)
                    {
                        ushort result;
                        if (ushort.TryParse(valueOut.ToString(), out result))
                        {
                            value = result;
                            return true;
                        }
                    }

                    var stringBuilder = new StringBuilder(150);
                    stringBuilder.Append("Try to read registry config ")
                        .Append(configPath)
                        .Append(" as 32bit int, while the actual value kind is ")
                        .Append(kind);

                    Tracer.TraceError(stringBuilder.ToString());
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// 以Long的方式读出
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Read(string configPath, out long value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;

                if (Read(configPath, out valueOut, out kind) && valueOut != null)
                {
                    if (kind == RegistryValueKind.DWord)
                    {
                        value = (long)(valueOut);
                        return true;
                    }

                    if (kind == RegistryValueKind.QWord)
                    {
                        value = (long)(valueOut);
                        return true;
                    }

                    if (kind == RegistryValueKind.String)
                    {
                        long result;
                        if (long.TryParse(valueOut.ToString(), out result))
                        {
                            value = result;
                            return true;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = 0;
            return false;
        }


        /// <summary>
        /// 以整数方式读出配置项
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value">配置值，如果读取失败，则被设置为0</param>
        /// <returns>如果未找到配置项，或者配置项无法转换为指定的类型，则返回false</returns>
        public bool Read(string configPath, out int value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;

                if (Read(configPath, out valueOut, out kind) && valueOut != null)
                {
                    if (kind == RegistryValueKind.DWord)
                    {
                        value = (int)(valueOut);
                        return true;
                    }
                    
                    if (kind == RegistryValueKind.QWord)
                    {
                        value = (int)(Int64)(valueOut);
                        Tracer.TraceWarning("Try to read 64bit integer as 32bit from registry:", configPath, valueOut);
                        return true;
                    }

                    if (kind == RegistryValueKind.String)
                    {
                        int result;
                        if (int.TryParse(valueOut.ToString(), out result))
                        {
                            value = result;
                            return true;
                        }
                    }

                    var stringBuilder = new StringBuilder(150);
                    stringBuilder.Append("Try to read registry config ")
                        .Append(configPath)
                        .Append(" as 32bit int, while the actual value kind is ")
                        .Append(kind);

                    Tracer.TraceError(stringBuilder.ToString());
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// 以字符串方式读出配置项
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns>如果未找到配置项，则返回false；如果找到配置项，但是字符串为空或不为空，则返回true</returns>
        public bool Read(string configPath, out string value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;

                if (Read(configPath, out valueOut, out kind))
                {
                    if (kind == RegistryValueKind.String)
                    {
                        value = valueOut as string;
                        return true;
                    }

                    if (kind == RegistryValueKind.QWord || kind == RegistryValueKind.DWord || kind == RegistryValueKind.ExpandString ||
                        kind == RegistryValueKind.None || kind == RegistryValueKind.Unknown)
                    {
                        value = valueOut.ToString();
                        return true;
                    }

                    if (kind == RegistryValueKind.Binary || kind == RegistryValueKind.MultiString)
                    {
                        var stringBuilder = new StringBuilder(150);
                        stringBuilder.Append("Try to read registry config ")
                            .Append(configPath)
                            .Append(" as string, while the actual value kind is ")
                            .Append(kind);

                        Tracer.TraceError(stringBuilder.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = null;
            return false;
        }
        
        /// <summary>
        /// 读取字符串数组
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Read(string configPath, out string[] value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;
                if (Read(configPath, out valueOut, out kind))
                {
                    // 对于字符串数组类型，直接转换为字符串
                    if (kind == RegistryValueKind.MultiString)
                    {
                        value = valueOut as string[];
                        return true;
                    }
                    
                    // 对于二进制类型，将每个十六进制值，转换为一个两位的字符串
                    if (kind == RegistryValueKind.Binary)
                    {
                        var bin = valueOut as byte[];
                        if (bin != null && bin.Length > 0)
                        {
                            value = new string[bin.Length];
                            for (int i = 0; i < bin.Length; i++)
                            {
                                value[i] = bin[i].ToString("X2");
                            }
                        }
                        else
                        {
                            value = null;
                        }

                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = null;
            return false;
        }

        public bool Read(string configPath, out byte[] value)
        {
            object valueOut;
            RegistryValueKind kind;
            if (Read(configPath, out valueOut, out kind))
            {
                if (kind == RegistryValueKind.Binary)
                {
                    value = valueOut as byte[];
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool Read(string configPath, out double value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;

                if (Read(configPath, out valueOut, out kind) && valueOut != null)
                {
                    // 当向注册表中写入浮点型数据时，注册表以字符串的形式保存
                    if (kind == RegistryValueKind.String)
                    {
                        return double.TryParse(valueOut.ToString(), System.Globalization.NumberStyles.Float, new System.Globalization.CultureInfo("zh-CN"), out value);
                    }

                    // 对于整形，也允许以浮点型读出
                    if (kind == RegistryValueKind.DWord)
                    {
                        value = (int)(valueOut);
                        return true;
                    }

                    if (kind == RegistryValueKind.QWord)
                    {
                        value = (int)(Int64)(valueOut);
                        Tracer.TraceWarning("Try to read 64bit integer as double from registry:", configPath, valueOut);
                        return true;
                    }

                    var stringBuilder = new StringBuilder(150);
                    stringBuilder.Append("Try to read registry config ")
                        .Append(configPath)
                        .Append(" as double, while the actual value kind is ")
                        .Append(kind);

                    Tracer.TraceError(stringBuilder.ToString());
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = 0;
            return false;
        }

        public bool Read(string configPath, out float value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;

                if (Read(configPath, out valueOut, out kind) && valueOut != null)
                {
                    // 当向注册表中写入浮点型数据时，注册表以字符串的形式保存
                    if (kind == RegistryValueKind.String)
                    {
                        return float.TryParse(valueOut.ToString(), System.Globalization.NumberStyles.Float, new System.Globalization.CultureInfo("zh-CN"), out value);
                    }

                    // 对于整形，也允许以浮点型读出
                    if (kind == RegistryValueKind.DWord)
                    {
                        value = (int)(valueOut);
                        return true;
                    }

                    if (kind == RegistryValueKind.QWord)
                    {
                        value = (int)(Int64)(valueOut);
                        Tracer.TraceWarning("Try to read 64bit integer as float from registry:", configPath, valueOut);
                        return true;
                    }

                    var stringBuilder = new StringBuilder(150);
                    stringBuilder.Append("Try to read registry config ")
                        .Append(configPath)
                        .Append(" as float, while the actual value kind is ")
                        .Append(kind);

                    Tracer.TraceError(stringBuilder.ToString());
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = 0;
            return false;
        }

        public bool Read(string configPath, out bool value)
        {
            try
            {
                object valueOut;
                RegistryValueKind kind;

                if (Read(configPath, out valueOut, out kind) && valueOut != null)
                {
                    // 当向注册表中写入布尔型数据时，注册表以字符串的形式保存
                    if (kind == RegistryValueKind.String)
                    {
                        return bool.TryParse(valueOut.ToString(), out value);
                    }

                    var stringBuilder = new StringBuilder(150);
                    stringBuilder.Append("Try to read registry config ")
                        .Append(configPath)
                        .Append(" as float, while the actual value kind is ")
                        .Append(kind);

                    Tracer.TraceError(stringBuilder.ToString());
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            value = false;
            return false;
        }

        public bool Read<T>(string configPath, out T value) where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new TypeAccessException("T must be Enum type.");
            }

            string valueString = null;
            if (Read(configPath, out valueString) && !string.IsNullOrEmpty(valueString))
            {
                return Enum.TryParse(valueString, true, out value);
            }

            // 如果失败，则取枚举值中的第一个值作为输出
            var enumValues = Enum.GetValues(typeof (T));
            value = (T)enumValues.GetValue(0);
            return false;
        }

        /// <summary>
        /// 读取一个配置项的值。如果未找到，则返回为false；如果找到，则返回true
        /// </summary>
        /// <param name="configPath"></param>
        /// <returns></returns>
        private bool Read(string configPath, out object value, out RegistryValueKind kind)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                var root = OpenRootKeyForRead();
                try
                {
                    var pathStr = configPath.Split(new char[] {'/'});

                    // 递归找到最后一个子键
                    RegistryKey subKey = root;
                    for (int i = 0; i < pathStr.Length - 1; i++)
                    {
                        subKey = subKey.OpenSubKey(pathStr[i]);
                        if (subKey == null)
                        {
                            goto FalseReturn;
                        }
                    }

                    if (subKey != null)
                    {
                        // 配置项最后一个字符串为键值名称
                        var keyName = pathStr[pathStr.Length - 1];

                        try
                        {
                            value = subKey.GetValue(keyName);
                            kind = subKey.GetValueKind(keyName);
                            return true;
                        }
                        catch (IOException exception)
                        {
                            Tracer.TraceError(configPath + " is not found.");
                            value = null;
                            kind = RegistryValueKind.None;
                            return false;
                        }
                    }
                }
                finally
                {
                    root.Close();
                    root.Dispose();
                }
            }

            FalseReturn:
            value = null;
            kind = RegistryValueKind.None;
            return false;
        }

        public bool Read<T>(string configPath, out T[] value) where T : struct
        {
            if (!typeof (T).IsEnum)
            {
                throw new TypeAccessException("T must be Enum type.");
            }

            // 先以字符串数组的方式读出，然后转换为枚举数组
            string[] strArray;
            if (Read(configPath, out strArray))
            {
                if (strArray != null)
                {
                    var tList = new List<T>(strArray.Length);
                    foreach (var s in strArray)
                    {
                        T t;
                        if (Enum.TryParse<T>(s, true, out t))
                        {
                            tList.Add(t);
                        }
                    }

                    value = tList.ToArray();
                    return true;
                }
            }

            value = null;
            return true;
        }

        public bool Write<T>(string configPath, T[] value) where T : struct
        {
            //if (!typeof(T).IsEnum)
            //{
            //    throw new TypeAccessException("T must be Enum type.");
            //}

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            try
            {
                var strArray = new string[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    strArray[i] = value[i].ToString();
                }

                // 以字符串数组的方式写入注册表
                return Write(configPath, strArray as object);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return false;
        }

        /// <summary>
        /// 写入一个配置项，如果指定的子键下不存在此配置项，则会自动创建；
        /// 将 64 位整数存储为字符串 ( RegistryValueKind.String)。
        /// 将所有字符串值存储为 RegistryValueKind.String，
        /// 将 32 位整数以外的其他 Numeric 类型存储为字符串;
        /// 枚举元素存储为包含元素名称的字符串
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Write(string configPath, object value)
        {
            try
            {
                if (!string.IsNullOrEmpty(configPath))
                {
                    var root = OpenRootKeyForWrite();

                    var pathStr = configPath.Split(new char[] { '/' });

                    // 递归找到最后一个子键
                    RegistryKey subKey = root;
                    RegistryKey parent = root;
                    for (int i = 0; i < pathStr.Length - 1; i++)
                    {
                        subKey = parent.OpenSubKey(pathStr[i], true);

                        // 如果指定的子键不存在，则递归创建出子键
                        if (subKey == null)
                        {
                            subKey = parent.CreateSubKey(pathStr[i], RegistryKeyPermissionCheck.ReadWriteSubTree);
                        }

                        parent = subKey;
                    }

                    if (subKey != null)
                    {
                        // 配置项最后一个字符串为键值名称
                        if (value is double)
                        {
                            var doubleValue = (double)value;
                            var strValue = doubleValue.ToString(new System.Globalization.CultureInfo("zh-CN"));
                            subKey.SetValue(pathStr[pathStr.Length - 1], strValue);
                        }
                        else if (value is float)
                        {
                            var floatValue = (float)value;
                            var strValue = floatValue.ToString(new System.Globalization.CultureInfo("zh-CN"));
                            subKey.SetValue(pathStr[pathStr.Length - 1], strValue);
                        }
                        else
                        {
                            subKey.SetValue(pathStr[pathStr.Length - 1], value);
                        }
                        return true;
                    }

                    root.Close();
                    root.Dispose();
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return false;
        }

        /// <summary>
        /// 注册表变动事件响应：激发事件，通知订阅者
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void MonitorOnRegChanged(object sender, EventArgs eventArgs)
        {
            RaiseConfigChangedEvent();
        }

        private void MonitorOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            Tracer.TraceException(errorEventArgs.GetException(), "RegistryConfigProvider: Registry Monitor Error.");
        }

        #region dispose

        private bool _disposed = false;

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_monitor != null)
                {
                    _monitor.Dispose();
                    _monitor = null;
                }
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RegistryConfigProvider()
        {
            Dispose(false);
        }

        #endregion dispose
        
    }
}
