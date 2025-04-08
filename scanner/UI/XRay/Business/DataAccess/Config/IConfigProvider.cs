using System;

namespace UI.XRay.Business.DataAccess.Config
{
    public interface IConfigProvider
    {
        /// <summary>
        /// 弱事件：某一配置项发生变化。订阅此事件后，如果不主动注销，也不会导致内存泄露
        /// </summary>
        event EventHandler ConfigChanged;

        /// <summary>
        /// 以整数方式读出配置项
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value">配置值，如果读取失败，则被设置为0</param>
        /// <returns>如果未找到配置项，或者配置项无法转换为指定的类型，则返回false</returns>
        bool Read(string configPath, out int value);

        bool Read(string configPath, out long value);

        bool Read(string configPath, out ushort value);

        /// <summary>
        /// 以字符串方式读出配置项
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns>如果未找到配置项，则返回false；如果找到配置项，但是字符串为空或不为空，则返回true</returns>
        bool Read(string configPath, out string value);

        /// <summary>
        /// 读取一个字符串数组
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Read(string configPath, out string[] value);

        bool Read(string configPath, out byte[] value);

        bool Read(string configPath, out float value);

        bool Read(string configPath, out double value);

        bool Read(string configPath, out bool value);

        /// <summary>
        /// 以枚举的方式读出配置项
        /// </summary>
        /// <typeparam name="T">必须是枚举类型</typeparam>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Read<T>(string configPath, out T value) where T : struct;

        /// <summary>
        /// 以枚举数组的方式，读出配置项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Read<T>(string configPath, out T[] value) where T : struct;

        /// <summary>
        /// 向配置项中写入枚举数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configPath"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Write<T>(string configPath, T[] value) where T : struct;

        /// <summary>
        /// 写入配置项。如果配置项不存在，则创建；如果已经存在，则更新
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="value">支持整形、浮点型、布尔型、字符串数组、byte数组</param>
        /// <returns></returns>
        bool Write(string configPath, object value);
    }
}