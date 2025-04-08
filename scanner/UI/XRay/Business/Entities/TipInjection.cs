using System;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// ��ʾһ��Tipע����������ж���ע��ʱʹ�õ�Tipͼ��ע�����ʼλ���Լ�Tipͼ���Flip��Rotate������
    /// </summary>
    [Serializable]
    public class TipInjection
    {
        /// <summary>
        /// ����һ��Tipע���ʵ��
        /// </summary>
        /// <param name="tipImage">Tipע��ʱ��ʹ�õ�ͼ����󣬼�Tipͼ��</param>
        /// <param name="regionInImage">Tipע��λ�ã�������ͼ���е�λ����Ϣ</param>
        public TipInjection(XRayScanlinesImage tipImage, MarkerRegion regionInImage)
        {
            TipImage = tipImage;
            RegionInImage = regionInImage;
        }

        /// <summary>
        /// ע��ʱ��ʹ�õ�Tipͼ��
        /// </summary>
        public XRayScanlinesImage TipImage { get; set; }

        /// <summary>
        /// Tipע�����ʼ̽��ͨ�����
        /// </summary>
        public int StartChannel
        {
            get { return RegionInImage.FromChannel; }
        }

        /// <summary>
        /// Tipע�����ʼɨ���߱�ţ����ڱ�ע��ͼ���е�ɨ���ߵı��
        /// </summary>
        public int StartScanLine
        {
            get { return RegionInImage.FromLine; }
        }

        /// <summary>
        /// Tipͼ��������ͼ���еľ���λ����Ϣ
        /// </summary>
        public MarkerRegion RegionInImage { get; private set; }
    }

    /// <summary>
    /// ��ʾһ��Tipע������¼���������ע���Tipͼ���Լ�ע�����ʼλ��
    /// </summary>
    public class TipInjectionEventArgs : EventArgs
    {
        public TipInjectionEventArgs(XRayScanlinesImage tipImage, MarkerRegion injectRegion, MarkerRegion injectRegion2)
        {
            TipImage = tipImage;
            InjectRegion = injectRegion;
            InjectRegion2 = injectRegion2;
        }

        /// <summary>
        /// ע��ʱ��ʹ�õ�Tipͼ��
        /// </summary>
        public XRayScanlinesImage TipImage { get; private set; }

        /// <summary>
        /// �˴�ע����ȫ�������е�λ��
        /// </summary>
        public MarkerRegion InjectRegion { get; private set; }

        /// <summary>
        /// �ӽ�2
        /// </summary>
        public MarkerRegion InjectRegion2 { get; private set; }
    }
}