using System;

namespace UI.XRay.Business.DataAccess.Config
{
    /// <summary>
    /// 读取或设置X-Ray Scanner的配置参数
    /// </summary> 
    public static class ScannerConfig
    {
        /// <summary>
        /// 默认使用注册表作为配置项存储介质。如果需要更改，可以参照接口重新实现
        /// </summary>
        private static IConfigProvider _config = new RegistryConfigProvider();

        /// <summary>
        /// 事件：某一配置项发生变更
        /// </summary>
        public static event EventHandler ConfigChanged
        {
            add { _config.ConfigChanged += value; }
            remove { _config.ConfigChanged -= value; }
        }

        /// <summary>
        /// 以整形读出某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read(string path, out int value)
        {
            return _config.Read(path, out value);
        }

        public static bool Read(string path, out long value)
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 以整形读出某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read(string path, out ushort value)
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 以浮点型读出某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read(string path, out float value)
        {
            return _config.Read(path, out value);
        }

        public static bool Read(string path, out double value)
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 以字符串读出某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read(string path, out string value)
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 以字符串数组读出某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read(string path, out string[] value)
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 以布尔型读出某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read(string path, out bool value)
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 读取一个枚举配置项
        /// </summary>
        /// <typeparam name="T">必须是枚举类型</typeparam>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read<T>(string path, out T value) where T : struct
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 读取枚举类型数组
        /// </summary>
        /// <typeparam name="T">必须是枚举类型</typeparam>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read<T>(string path, out T[] value) where T : struct
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 以字节数组读出某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Read(string path, out byte[] value)
        {
            return _config.Read(path, out value);
        }

        /// <summary>
        /// 将枚举值写入某配置项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write<T>(string path, T value) where  T : struct 
        {
            return _config.Write(path, value);
        }

        /// <summary>
        /// 将整形值写入某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write(string path, int value)
        {
            return _config.Write(path, value);
        }

        /// <summary>
        /// 将整形值写入某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write(string path, ushort value)
        {
            return _config.Write(path, value);
        }

        /// <summary>
        /// 将浮点型值写入某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write(string path, float value)
        {
            return _config.Write(path, value);
        }

        public static bool Write(string path, double value)
        {
            return Write(path, (float) value);
        }

        /// <summary>
        /// 将布尔值写入某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write(string path, bool value)
        {
            return _config.Write(path, value);
        }

        /// <summary>
        /// 将字符串写入某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write(string path, string value)
        {
            return _config.Write(path, value);
        }

        /// <summary>
        /// 将字符串写入某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write(string path, string[] value)
        {
            return _config.Write(path, value as object);
        }

        /// <summary>
        /// 将字节数组写入某配置项
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write(string path, byte[] value)
        {
            return _config.Write(path, value as object);
        }

        /// <summary>
        /// 向配置项中写入枚举数组
        /// </summary>
        /// <typeparam name="T">必须是枚举类型，不能是Int等类型</typeparam>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Write<T>(string path, T[] value) where T : struct
        {
            return _config.Write(path, value);
        }
    }
}
