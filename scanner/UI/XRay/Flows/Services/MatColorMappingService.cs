using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant.Classify;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 物质分类-颜色映射服务
    /// </summary>
    public class MatColorMappingService
    {
        public static MatColorMappingService Service { get; private set; }

        static MatColorMappingService()
        {
            Service = new MatColorMappingService();
        }

        private MaterialColorMapper _mapper;

        private bool _showUnpenetratableInRed = false;

        protected MatColorMappingService()
        {
            _mapper = new MaterialColorMapper();

            UpdateUnpenEffect();
            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            UpdateUnpenEffect();
        }

        public ushort Map(ushort material)
        {
            return _mapper.Map(material);
        }

        public void Map(ushort[] material, out ushort[] colorIndex)
        {
            _mapper.Map(material, out colorIndex);
        }

        /// <summary>
        /// 更新穿不透效果
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void UpdateUnpenEffect()
        {
            if (!ScannerConfig.Read(ConfigPath.ImagesShowUnpenetratableRed, out _showUnpenetratableInRed))
            {
                _showUnpenetratableInRed = false;
            }

            if (_mapper != null)
            {
                _mapper.ShowUnpenetratableInRed = _showUnpenetratableInRed;
            }
        }
    }
}
