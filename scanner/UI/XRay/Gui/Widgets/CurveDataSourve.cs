using System.ComponentModel;
using System.Windows.Media;

namespace UI.XRay.Gui.Widgets
{

    /// <summary>
    /// Data source for CurveControl
    /// </summary>
    public class CurveDataSource : INotifyPropertyChanged
    {
        private double[] _DataPoints;

        /// <summary>
        /// Curve data array. x for array index, from 0 to Length-1; y for element value.
        /// </summary>
        public double[] DataPoints {
            get { return _DataPoints; }
            set
            {
                _DataPoints = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("DataPoints"));
                }
            }}

        /// <summary>
        /// the name of the curve. should be unique.
        /// </summary>
        public string CurveName { get; private set; }

        /// <summary>
        /// desired color of this curve
        /// </summary>
        public Color CurveColor { get; private set; }

        /// <summary>
        /// is the curve visible or not.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Constructs an curve with input properties.
        /// </summary>
        /// <param name="dataPoints">曲线的数据</param>
        /// <param name="curveName">曲线的名字</param>
        /// <param name="curveColor">曲线的颜色</param>
        /// <param name="enabled">曲线是否使能，默认是使能的</param>
        public CurveDataSource(double[] dataPoints, string curveName, Color curveColor, bool enabled = true)
        {
            DataPoints = dataPoints;
            CurveName = curveName;
            CurveColor = curveColor;
            Visible = enabled;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
