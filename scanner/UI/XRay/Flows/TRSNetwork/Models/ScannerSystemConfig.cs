using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.Business.DataAccess.Config;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    public class ScannerSystemConfig
    {
        #region Instance

        private static ScannerSystemConfig _instance;

        public static ScannerSystemConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ScannerSystemConfig();
                return _instance;
            }
        }

        public ScannerSystemConfig()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (!ScannerConfig.Read(ConfigPath.ImagesCount, out imagesCount))
            {
                imagesCount = 1;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage1VerticalFlip, out image1VerticalFlip))
            {
                image1VerticalFlip = false;
            }

            if (!ScannerConfig.Read(ConfigPath.Image1MoveRightToLeft, out image1RightToLeft))
            {
                image1RightToLeft = false;
            }
            if (imagesCount > 1)
            {
                if (!ScannerConfig.Read(ConfigPath.ImagesImage2VerticalFlip, out image2VerticalFlip))
                {
                    image2VerticalFlip = false;
                }

                if (!ScannerConfig.Read(ConfigPath.Image2MoveRightToLeft, out image2RightToLeft))
                {
                    image2RightToLeft = false;
                }
            }
        }
        #endregion

        #region Properties
        private int imagesCount;
        private bool image1RightToLeft;
        private bool image2RightToLeft;
        private bool image1VerticalFlip;
        private bool image2VerticalFlip;

        public int ImagesCount
        {
            get { return imagesCount; }
            set { imagesCount = value; }
        }

        public bool Image1RightToLeft
        {
            get { return image1RightToLeft; }

            set { image1RightToLeft = value; }
        }

        public bool Image2RightToLeft
        {
            get { return image2RightToLeft; }

            set { image2RightToLeft = value; }
        }


        public bool Image1VerticalFlip
        {
            get { return image1VerticalFlip; }
            set { image1VerticalFlip = value; }
        }

        public bool Image2VerticalFlip
        {
            get { return image2VerticalFlip; }
            set { image2VerticalFlip = value; }
        }


        #endregion
    }
}
