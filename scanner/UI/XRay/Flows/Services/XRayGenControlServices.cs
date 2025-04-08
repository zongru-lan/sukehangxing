using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Control;
using UI.XRay.XRayGenControl.Workflow;

namespace UI.XRay.Flows.Services
{
    public class XRayGenControlServices
    {
        private readonly XRayGenWorkflowController _xrayGenController = new XRayGenWorkflowController();

        private bool _initType;

        private bool _initPort;

        /// <summary>
        /// 初始化参数，并验证类型
        /// </summary>
        /// <param name="xrayGenCount"></param>
        /// <param name="xrayGenType"></param>
        /// <param name="kv"></param>
        /// <param name="ma"></param>
        /// <returns></returns>
        public bool InitXRayGenSetting(int xrayGenCount, XRayGeneratorType xrayGenType, float kv, float ma)
        {
            var setting = new XRayGeneratorSettings(xrayGenType, kv, ma);
            _xrayGenController.InitXRayGenSetting(xrayGenCount, setting);
            _initType = _xrayGenController.InitXRayGeneratorType(xrayGenType);
            return _initType;
        }

        public bool InitPort(XRayGeneratorIndex index, string xgPortName)
        {
            //串口与类型相关
            if (!_initType)
            {
                return false;
            }
            _initPort = _xrayGenController.InitController(index, xgPortName);
            return _initPort;
        }

        public bool InitPorts(string xg1PortName, string xg2PortName)
        {
            //串口与类型相关
            if (!_initType)
            {
                return false;
            }
            _initPort = _xrayGenController.InitControllers(xg1PortName, xg2PortName);
            return _initPort;
        }

        public bool TestConnection(XRayGeneratorIndex index)
        {
            if (!_initType)
            {
                return false;
            }
            return _xrayGenController.TestConnection(index);
        }

        public void Dispose()
        {
            _xrayGenController.ShutDown();
        }
    }
}
