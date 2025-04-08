using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Win32;
using UI.Common.Tracers;

namespace UI.XRay.Security.Configer
{
    /// <summary>
    /// 配置导入导出帮助类，用于实现配置的导入导出
    /// </summary>
    public class ConfigImportExportHelper
    {
        /// <summary>
        /// 将配置从文件导入到注册表
        /// </summary>
        /// <param name="configFilePath">源配置文件的全路径</param>
        /// <returns>false否则返回导入成功返回true，</returns>
        public static bool Import(string configFilePath)
        {
            ConfigNode rootConfigNode;

            //从XML反序列化配置项列表
            try
            {
                using (var fs = new FileStream(configFilePath, FileMode.Open))
                {
                    var sr = new XmlSerializer(typeof(ConfigNode));
                    rootConfigNode = (ConfigNode)sr.Deserialize(fs);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "Can't load import config file: " + configFilePath);
                return false;
            }

            if (rootConfigNode != null)
            {
                // 以可写模式打开或创建注册表Software键
                var software = Registry.LocalMachine.CreateSubKey("Software");
                if (software != null)
                {
                    try
                    {
                        // todo 这里要求设备类型配置文件也要从跟目录开始
                        // 调用循环递归函数，递归写入每一个配置
                        WriteConfigValues(rootConfigNode, software);
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e, "Can't import configs.");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 设置配置，即将配值结点写入注册表，这是一个循环递归函数
        /// </summary>
        /// <param name="parentConfigNode">将要被写入注册表的配置结点</param>
        /// <param name="parentKey">目的注册表键，即将配置写入该注册表键下面</param>
        private static void WriteConfigValues(ConfigNode parentConfigNode, RegistryKey parentKey)
        {
            // todo 暂时没有判断是否覆盖，都是覆盖了
            // 为该配置创建注册表键，如果成功则进行下一步处理
            var subKey = parentKey.CreateSubKey(parentConfigNode.Name);
            if (subKey != null)
            {
                // 如果该配置存在值，则遍历值列表，并将值写入注册表
                if (parentConfigNode.Values != null && parentConfigNode.Values.Count > 0)
                {
                    foreach (var value in parentConfigNode.Values)
                    {
                        subKey.SetValue(value.Name, value.Value);
                    }
                }

                // 如果该配置存在子配置，则遍历子配置列表，并调用本函数，递归处理每一个子配置
                if (parentConfigNode.SubConfigNodes != null && parentConfigNode.SubConfigNodes.Count > 0)
                {
                    foreach (var configNode in parentConfigNode.SubConfigNodes)
                    {
                        if (configNode != null)
                        {
                            // 递归调用本函数，处理子配置结点下的子结点
                            WriteConfigValues(configNode, subKey);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 将配置从注册表导出到指定的文件
        /// </summary>
        /// <param name="filePath">指定的目的文件全路径</param>
        /// <returns>导出成功返回true，否则返回false</returns>
        public static bool Export(string filePath)
        {
            try
            {
                // 打开注册表software键
                var software = Registry.LocalMachine.OpenSubKey("Software");
                if (software != null)
                {
                    // 新建配置根节点
                    ConfigNode rootConfigNode = new ConfigNode("HTXRayScanner", null, null);

                    // 打开注册表配置根键
                    var xrayScanner = software.OpenSubKey("HTXRayScanner");
                    if (xrayScanner != null)
                    {
                        // 获取所有的配置子节点
                        GetSubConfigNodes(rootConfigNode, xrayScanner);
                        //xrayScanner.Name

                        // todo 暂时设定根节点下面没有值
                    }

                    //把配置列表序列化到XML文件
                    using (TextWriter tr = new StreamWriter(filePath))
                    {
                        var xs = new XmlSerializer(typeof(ConfigNode));
                        xs.Serialize(tr, rootConfigNode);
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "Can't export config to file: " + filePath);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取配置结点的子配置结点，实际上对应注册表中的子键，这是一个循环递归函数
        /// </summary>
        /// <param name="parentConfigNode">父配置结点</param>
        /// <param name="parentKey">父注册表键</param>
        private static void GetSubConfigNodes(ConfigNode parentConfigNode, RegistryKey parentKey)
        {
            // 获取并判断该注册表键是否存在子键
            var subKeyNames = parentKey.GetSubKeyNames();
            if (subKeyNames != null && subKeyNames.Length > 0)
            {
                var subConfigNodes = new List<ConfigNode>();

                // 循环遍历注册表子键，获取子配置结点
                foreach (string subKeyName in subKeyNames)
                {
                    var subKey = parentKey.OpenSubKey(subKeyName);
                    if (subKey != null)
                    {
                        var subConfigNode = new ConfigNode(subKeyName, null, null);

                        // 递归调用自身函数，以进一步获取注册表子键的子键
                        GetSubConfigNodes(subConfigNode, subKey);
                        subConfigNodes.Add(subConfigNode);
                    }
                }

                parentConfigNode.SubConfigNodes = subConfigNodes;
            }

            // 判断该注册表键是否存在值，如果存在，则获取其值
            if (parentKey.ValueCount > 0)
            {
                parentConfigNode.Values = GetConfigValues(parentKey); ;
            }
        }

        /// <summary>
        /// 获取配置的值，获取注册表中指定键下面包含的值
        /// </summary>
        /// <param name="subKey">注册表中指定的键</param>
        /// <returns>ConfigNode列表，包含的是查找到的所有值</returns>
        private static List<ConfigNode> GetConfigValues(RegistryKey subKey)
        {
            List<ConfigNode> values = new List<ConfigNode>();
            // 遍历注册表键下面的每一个值，获取所有的值结点
            foreach (string valueName in subKey.GetValueNames())
            {
                // 获取值的值
                var value = subKey.GetValue(valueName);
                // 获取值的值类型（在注册表中），暂时不用
                //var valueType = subKey.GetValueKind(valueName);
                values.Add(new ConfigNode(valueName, value, null));
            }
            return values;
        }

        /// <summary>
        /// 定义配置结点类，是一种树状结构，可以表示一个值，也可以表示一个注册表的键
        /// </summary>
        public class ConfigNode
        {
            /// <summary>
            /// 配置的名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 如果个配置结点在注册表中表示的是一个值，那么该属性就是该值的值
            /// </summary>
            public object Value { get; set; }

            /// <summary>
            /// 该配置结点在注册表中键下面可能存在的子键集合
            /// </summary>
            public List<ConfigNode> SubConfigNodes { get; set; }

            /// <summary>
            /// 该配置结点在注册表中键下面可能存在的值集合
            /// </summary>
            public List<ConfigNode> Values { get; set; }

            /// <summary>
            /// 构造函数，主要用于初始化配置结点的名称和值，其中值可以为null
            /// </summary>
            /// <param name="name">配置结点的名称</param>
            /// <param name="value">配置节点的值</param>
            /// <param name="valueType">配置值在注册表中的值，暂时不使用</param>
            public ConfigNode(string name, object value, object valueType)
            {
                Name = name;
                Value = value;
                //ValueType = valueType;
                //SubConfigNodes = new List<ConfigNode>();
                //Values = new List<ConfigNode>();
            }

            /// <summary>
            /// 无参构造函数，主要用于Xml序列化和反序列化，这是序列化的要求
            /// </summary>
            public ConfigNode()
            {

            }
        }
    }
}
