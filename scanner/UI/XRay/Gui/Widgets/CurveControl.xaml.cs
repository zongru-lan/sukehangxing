//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// 
// Description: 定义曲线控件CurveControl类：实现其逻辑处理。该控件使用Winform中的Chart控件绘制曲线
//              能够实现冻结曲线以及对某条曲线是否显示，使用简单，设置好其背景色、坐标轴颜色、网格数量，即可通过曲线数据源更新曲线
//              并能够放大曲线，放大后鼠标双击即可复原
//              如果要清除曲线，则给源赋值为null，即暂时如果要清除某条曲线，需要先把源设置为null，然后再把源设置为实际的曲线源
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Media;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.XRay.Gui.Widgets
{
    /// <summary>
    /// 事件参数：表示曲线上某个数据点的信息
    /// </summary>
    public class CurveDataPointToolTipEventArgs : EventArgs
    {
        public CurveDataPointToolTipEventArgs(string curveName, int xIndex, double yVal)
        {
            CurveName = curveName;
            XIndex = xIndex;
            YValue = yVal;
        }

        /// <summary>
        /// 此数据点对应的曲线的名称
        /// </summary>
        public string CurveName { get; private set; }

        /// <summary>
        /// 此数据点对应的 X 轴的位置，从1开始，而不是从0开始
        /// </summary>
        public int XIndex { get; private set; }

        /// <summary>
        /// 此数据点对应的Y轴的数值
        /// </summary>
        public double YValue { get; private set; }

        /// <summary>
        /// 设置或获取此数据点上方将要显示的提示文字
        /// </summary>
        public string Text { get; set; }
    }

    static class ColorExtentions
    {
        public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }

    /// <summary>
    /// CurveControl.xaml 的交互逻辑
    /// </summary>
    public partial class CurveControl : UserControl, IDisposable
    {
        private CurveDataSource _defaultCurve = new CurveDataSource(new double[192], "Please add a curve", Colors.DeepPink);

        /// <summary>
        /// 曲线数据源属性，是CurvesDataSourceProperty的依赖属性
        /// </summary>
        public ObservableCollection<CurveDataSource> CurvesDataSource
        {
            get { return (ObservableCollection<CurveDataSource>)GetValue(CurvesDataSourceProperty); }
            set { SetValue(CurvesDataSourceProperty, value); }
        }

        /// <summary>
        /// 坐标轴的颜色属性，是AxisColorProperty的依赖属性
        /// </summary>
        public Color AxisColor
        {
            get { return (Color)GetValue(AxisColorProperty); }
            set { SetValue(AxisColorProperty, value); }
        }

        /// <summary>
        /// 绘图区域的视图背景色，是BackgroundColorProperty的依赖属性
        /// </summary>
        public Color ChartAreaBackgroundColor
        {
            get { return (Color)GetValue(ChartAreaBackgroundColorProperty); }
            set { SetValue(ChartAreaBackgroundColorProperty, value); }
        }

        public Color ChartControlBackgroundColor
        {
            get { return (Color)GetValue(ChartControlBackgroundColorProperty); }
            set { SetValue(ChartControlBackgroundColorProperty, value); }
        }

        public Color ChartAxisLabelColor
        {
            get { return (Color) GetValue(ChartAxisLabelColorProperty); }
            set { SetValue(ChartAxisLabelColorProperty, value); }
        }

        public double YMaxValue
        {
            get { return (double)GetValue(YMaxValueProperty); }
            set { SetValue(YMaxValueProperty, value); }
        }

        public double YMinValue
        {
            get { return (double)GetValue(YMinValueProperty); }
            set { SetValue(YMinValueProperty, value); }
        }

        /// <summary>
        /// X轴向的数据分块的大小，用于分区显示数据，便于观察，一般表示每个DA板的通道数
        /// </summary>
        public int BlockSize
        {
            get { return (int)GetValue(BlockSizeProperty); }
            set { SetValue(BlockSizeProperty, value); }
        }

        /// <summary>
        /// 是否静态显示曲线，不接受更新的曲线数据
        /// </summary>
        public bool Freezed
        {
            get { return (bool) GetValue(FreezedProperty); }
            set { SetValue(FreezedProperty,value); }
        }

        /// <summary>
        /// 在用户没有添加任何曲线的时候，是否显示一条默认的曲线
        /// </summary>
        public bool ShowDefaultCurve
        {
            get { return (bool)GetValue(ShowDefaultCurveProperty); }
            set { SetValue(ShowDefaultCurveProperty, value); }
        }

        /// <summary>
        /// X轴向的数据分块的个数，即DA板的个数
        /// </summary>
        private int BlocksCount { get; set; }

        /// <summary>
        /// 图标的坐标轴字体的颜色，默认为红色
        /// </summary>
        public static DependencyProperty BlockSizeProperty = DependencyProperty.Register(
            "BlockSize",
            typeof(int),
            typeof(CurveControl),
            new PropertyMetadata(64, OnDependencyPropertyChanged));

        /// <summary>
        /// 定义依赖项属性：是否静态显示曲线。在静态显示的时候，占用极低的CPU和GPU，不接收动态更新的数据
        /// </summary>
        public static DependencyProperty FreezedProperty = DependencyProperty.Register(
            "Freezed",
            typeof (bool),
            typeof (CurveControl),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

        /// <summary>
        /// 定义依赖性属性：在没有任何曲线的时候，是否显示一条默认的曲线
        /// </summary>
        public static DependencyProperty ShowDefaultCurveProperty = DependencyProperty.Register(
            "ShowDefaultCurve",
            typeof(bool),
            typeof(CurveControl),
            new PropertyMetadata(true, OnDependencyPropertyChanged));

        /// <summary>
        /// 图标的坐标轴字体的颜色，默认为红色
        /// </summary>
        public static DependencyProperty YMaxValueProperty = DependencyProperty.Register(
            "YMaxValue",
            typeof(double),
            typeof(CurveControl),
            new PropertyMetadata(65535.0, OnDependencyPropertyChanged));

        public static DependencyProperty YMinValueProperty = DependencyProperty.Register(
            "YMinValue",
            typeof(double),
            typeof(CurveControl),
            new PropertyMetadata(0.0, OnDependencyPropertyChanged));

        /// <summary>
        /// 图标的坐标轴字体的颜色，默认为红色
        /// </summary>
        public static DependencyProperty ChartAxisLabelColorProperty = DependencyProperty.Register(
            "ChartAxisLabelColor",
            typeof (Color),
            typeof (CurveControl),
            new PropertyMetadata(Colors.Red, OnDependencyPropertyChanged));

        /// <summary>
        /// 坐标轴颜色的依赖项属性，依赖于AxisColor属性，默认是白色
        /// </summary>
        public static DependencyProperty AxisColorProperty = DependencyProperty.Register("AxisColor", 
            typeof(System.Windows.Media.Color),
            typeof(CurveControl), 
            new PropertyMetadata(Colors.DimGray, OnDependencyPropertyChanged));

        /// <summary>
        /// 曲线数据源依赖项属性，依赖于CurvesDataSource属性
        /// </summary>
        public static DependencyProperty CurvesDataSourceProperty = DependencyProperty.Register("CurvesDataSource",
            typeof(ObservableCollection<CurveDataSource>), typeof(CurveControl),
            new PropertyMetadata(null, OnCurveDataSourceDependencyPropertyChanged));

        /// <summary>
        /// 控件绘图区域的颜色，默认是黑色
        /// </summary>
        public static DependencyProperty ChartAreaBackgroundColorProperty = DependencyProperty.Register("ChartAreaBackgroundColor", typeof(Color),
            typeof(CurveControl), new PropertyMetadata(Colors.LightBlue, OnDependencyPropertyChanged));

        /// <summary>
        /// 绘图控件的背景色默认是黑色
        /// </summary>
        public static DependencyProperty ChartControlBackgroundColorProperty = DependencyProperty.Register("ChartControlBackgroundColor", typeof(Color),
            typeof(CurveControl), new PropertyMetadata(Colors.LightBlue, OnDependencyPropertyChanged));


        /// <summary>
        /// 事件：当鼠标放置到某个数据点上方时，弹出提示框，显示当前数据点的索引和数值。
        /// 订阅此事件，并在此事件中可以
        /// </summary>
        public event EventHandler<CurveDataPointToolTipEventArgs> GetDataPointToolTipText;

        /// <summary>
        /// 构造函数，完成控件的初始化，包括Chart的初始化
        /// </summary>
        public CurveControl()
        {
            InitializeComponent();
            InitializeChart();
            CurveChart.AxisViewChanged += CurveChart_AxisViewChanged;
            CurveChart.GetToolTipText += CurveChartOnGetToolTipText;
            CurveChart.MouseDown += CurveChart_MouseDown;
            CurveChart.MouseDoubleClick += CurveChart_MouseDoubleClick;
            CurveChart.ImeMode = ImeMode.Off;
            CurveChart.TabStop = false;
        }

        /// <summary>
        /// 当鼠标悬停在曲线上时，自动显示当前位置的数据信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurveChartOnGetToolTipText(object sender, ToolTipEventArgs e)
        {
            if (e.HitTestResult.ChartElementType == ChartElementType.DataPoint)
            {
                // 获取数据点的索引，从0开始
                int i = e.HitTestResult.PointIndex;
                DataPoint dp = e.HitTestResult.Series.Points[i];

                var xIndex = i + 1;
                var yVal = dp.YValues[0];
                var curveName = e.HitTestResult.Series.Name;

                // 如果订阅了事件，则从事件中获取用户设定的输出文本，用这种方法可以显示出DA板的位置
                if (GetDataPointToolTipText != null)
                {
                    var arg = new CurveDataPointToolTipEventArgs(curveName, xIndex, yVal);
                    GetDataPointToolTipText(this, arg);
                    e.Text = arg.Text;
                }
                else
                {
                    e.Text = string.Format("{0}[{1},{2}]", curveName, i + 1, dp.YValues[0]);
                }
            }
        }

        /// <summary>
        /// 更新曲线的显示方式：动态或静态
        /// </summary>
        private void UpdateFreezedMode()
        {
            if (Freezed)
            {
                SetStaticMode();
            }
            else
            {
                SetFastMode();
            }
        }

        /// <summary>
        /// 设置为快速模式，此时仅显示折线，不显示每个点的形状
        /// </summary>
        private void SetFastMode()
        {
            foreach (var series in CurveChart.Series)
            {
                // 数据以FastLine类型显示速度最快，对它的修饰比较少
                series.ChartType = SeriesChartType.FastLine;
            }
        }

        /// <summary>
        /// 设置为静态模式：当曲线不需要动态更新时，即被冻结时，切换为静态模式，点的位置更清晰
        /// </summary>
        private void SetStaticMode()
        {
            foreach (var series in CurveChart.Series)
            {
                // 数据以FastLine类型显示速度最快，对它的修饰比较少
                series.ChartType = SeriesChartType.Line;

                series.MarkerStyle = MarkerStyle.Square;
                series.MarkerColor = series.Color;
                series.MarkerSize = 4;
            }
        }

        /// <summary>
        /// 当条件发生变化时，显示或隐藏默认的曲线
        /// </summary>
        private void ShowHideDefaultCurve()
        {
            var defaultCurve = CurveChart.Series.FindByName(_defaultCurve.CurveName);

            if (defaultCurve != null)
            {
                if (ShowDefaultCurve && CurveChart.Series.Count == 1)
                {

                    defaultCurve.Enabled = true;
                }
                else
                {
                    defaultCurve.Enabled = false;
                }
            }
        }

        /// <summary>
        /// 在增加或删除一条曲线后，更新DA板的个数
        /// </summary>
        private void UpdateBlockLabels()
        {
            CurveChart.ChartAreas[0].AxisX.CustomLabels.Clear();

            foreach (var series1 in CurveChart.Series)
            {
                var count = series1.Points.Count/BlockSize;
                BlocksCount = Math.Max(count, BlocksCount);
            }

            // 在X轴下方，显示自定义标签，在自定义标签中，显示每块DA板的编号。
            for (int i = 1; i <= BlocksCount; i++)
            {
                var label = new CustomLabel(BlockSize*(i - 1),
                    BlockSize*i, i.ToString(), 100, LabelMarkStyle.Box);

                // 自定义表现的刻度线的颜色，与轴线的颜色相同
                label.MarkColor = CurveChart.ChartAreas[0].AxisX.MajorGrid.LineColor;

                CurveChart.ChartAreas[0].AxisX.CustomLabels.Add(label);
            }
        }

        /// <summary>
        /// 当绑定的曲线数据源集合中的某一项发生变化时：添加、删除或更新了某一条数据曲线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurvesDataSourceOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            // 从曲线集合中移除了一条曲线，同时将曲线从图标中移除
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var oldCurve in e.OldItems)
                {
                    // 找到曲线对象，并删除
                    var curve = oldCurve as CurveDataSource;
                    var series = CurveChart.Series.FindByName(curve.CurveName);
                    CurveChart.Series.Remove(series);
                }
                ShowHideDefaultCurve();
                UpdateBlockLabels();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // 增加了一条曲线，则将曲线增加到图表中
                foreach (var newItem in e.NewItems)
                {
                    var curve = newItem as CurveDataSource;

                    // 如果此曲线名称尚未存在，则添加作为新的曲线
                    if (CurveChart.Series.IsUniqueName(curve.CurveName))
                    {
                        // 曲线已经存在，则先删除原来的曲线，再添加为新的曲线
                        CurveChart.Series.Remove(CurveChart.Series.FindByName(curve.CurveName));
                        AddCurve(curve);
                        CurveChart.Series[curve.CurveName].Enabled = curve.Visible;
                    }
                    else
                    {
                        // 图表中已经存在同名的曲线，不再增加作为新的曲线
                        // 曲线已经存在，则先删除原来的曲线，再添加为新的曲线
                        CurveChart.Series.Remove(CurveChart.Series.FindByName(curve.CurveName));
                        AddCurve(curve);
                        CurveChart.Series[curve.CurveName].Enabled = curve.Visible;
                    }
                }
                ShowHideDefaultCurve();
                UpdateBlockLabels();
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && !Freezed)
            {
                // 用户替换其中的一条曲线，一般是用户更改了曲线的数据，需要动态更新曲线
                foreach (var oldItem in e.OldItems)
                {
                    var curve = oldItem as CurveDataSource;
                    CurveChart.Series[curve.CurveName].Points.DataBindY(curve.DataPoints);
                    CurveChart.Series[curve.CurveName].Enabled = curve.Visible;
                }
                UpdateBlockLabels();
            }

        }

        /// <summary>
        /// 坐标视图改变事件处理函数，主要用于坐标视图放大倍数变化后，刷新网格线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurveChart_AxisViewChanged(object sender, ViewEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            if (CurveChart.ChartAreas[0].AxisX.ScaleView.IsZoomed)
            {
                ShowBlockPartitions(false);
            }
            else
            {
                ShowBlockPartitions(true);
            }
        }

        private void OnCurveDataSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            var old = e.OldValue as ObservableCollection<CurveDataSource>;
            if (old != null)
            {
                // 在源发生改变的时候，如果原来的绑定不为空，则需要先解除与原来绑定的源之间的委托
                old.CollectionChanged -= CurvesDataSourceOnCollectionChanged;
            }

            // 因为数据源已经变化，需要从图表中清除所有旧的曲线
            CurveChart.Series.Clear();

            // 默认情况下，显示测试曲线
            AddCurve(_defaultCurve);

            // 如果新的数据源不为空，则订阅数据源集合的变化的事件，在其变化的事件中更新曲线：增加、删除或变更曲线数据
            if (CurvesDataSource != null)
            {
                CurvesDataSource.CollectionChanged += CurvesDataSourceOnCollectionChanged;

                // 处理新绑定的数据源中的现有数据
                foreach (var curveSource in CurvesDataSource)
                {
                    // 先判断是否已经有同名曲线
                    if (!CurveChart.Series.IsUniqueName(curveSource.CurveName))
                    {
                        // 拥有同名曲线存在，则直接刷新已经存在的曲线的数据
                        CurveChart.Series[curveSource.CurveName].Points.DataBindY(curveSource.DataPoints);
                    }
                    else 
                    {
                        // 该曲线不存在，则添加该曲线
                        AddCurve(curveSource);
                    }

                    // 根据数据源中曲线的是否可见状态，更新曲线的显示状态
                    CurveChart.Series[curveSource.CurveName].Enabled = curveSource.Visible;
                }
            }

            // 绑定发生改变，显示或隐藏默认的曲线
            ShowHideDefaultCurve();
            UpdateBlockLabels();
        }

        /// <summary>
        /// 依赖项属性
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnCurveDataSourceDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var curveControl = d as CurveControl;
            if (curveControl != null && e.Property == CurvesDataSourceProperty)
            {
                curveControl.OnCurveDataSourceChanged(e);
            }
        }

        /// <summary>
        /// 静态函数，作为自定义依赖项数据变化的处理。当自定义的依赖项属性发生变化时，在这里更新
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var curveControl = d as CurveControl;
            if (curveControl != null)
            {
                var chartArea = curveControl.CurveChart.ChartAreas.FirstOrDefault();

                if (e.Property == ChartAreaBackgroundColorProperty && chartArea != null)
                {
                    chartArea.BackColor = curveControl.ChartAreaBackgroundColor.ToDrawingColor();
                }
                else if (e.Property == ChartAxisLabelColorProperty)
                {
                    if (chartArea != null)
                    {
                        chartArea.AxisX.LabelStyle.ForeColor = curveControl.AxisColor.ToDrawingColor();
                        chartArea.AxisY.LabelStyle.ForeColor = curveControl.AxisColor.ToDrawingColor();
                    }
                }
                else if (e.Property == ChartControlBackgroundColorProperty)
                {
                    curveControl.CurveChart.BackColor = curveControl.ChartControlBackgroundColor.ToDrawingColor();
                }
                else if (e.Property == AxisColorProperty && chartArea != null)
                {
                    // 更新坐标轴以及刻度值的颜色
                    chartArea.AxisX.LineColor = curveControl.AxisColor.ToDrawingColor();
                    chartArea.AxisY.LineColor = curveControl.AxisColor.ToDrawingColor();
                }
                else if (e.Property == YMaxValueProperty && chartArea != null)
                {
                    // 设置Y轴的最大值
                    chartArea.AxisY.Maximum = curveControl.YMaxValue;
                }
                else if (e.Property == YMinValueProperty && chartArea != null)
                {
                    // 设置Y轴的最小值
                    chartArea.AxisY.Minimum = curveControl.YMinValue;
                }
                else if (e.Property == BlockSizeProperty && chartArea != null)
                {
                    // 设置在非缩放模式下的X轴的刻度间隔，分区显示数据

                    if (!chartArea.AxisX.ScaleView.IsZoomed)
                    {
                        chartArea.AxisX.Interval = curveControl.BlockSize;
                        chartArea.AxisX.MajorGrid.Interval = curveControl.BlockSize;
                    }
                }
                else if (e.Property == FreezedProperty)
                {
                    curveControl.UpdateFreezedMode();
                }
                else if (e.Property == ShowDefaultCurveProperty)
                {
                    curveControl.ShowHideDefaultCurve();
                }
            }
        }

        /// <summary>
        /// 是否显示用于区分各个数据块的分割线。在图表不缩放的时候，按数据区分块显示
        /// 用于显示DA板之间的分割线。
        /// </summary>
        /// <param name="show">true 表示按DA板分割显示</param>
        private void ShowBlockPartitions(bool show)
        {
            var curveChartArea = CurveChart.ChartAreas.FirstOrDefault();
            if (curveChartArea != null)
            {
                if (show)
                {
                    // 按DA板分割显示。
                    // 需要将坐标轴的Interval和MajorGrid的Interval设置成一样的值，这样保证坐标轴的刻度值和网格线对应
                    CurveChart.ChartAreas[0].AxisX.Interval = BlockSize;
                    curveChartArea.AxisX.MajorGrid.Interval = BlockSize;
                    curveChartArea.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                    curveChartArea.AxisX.MajorGrid.LineWidth = 1;
                    // 在非放大模式下，Y轴显示为：网格间隔为2000，刻度间隔为4000
                    curveChartArea.AxisY.MajorGrid.Interval = 2000;
                    curveChartArea.AxisY.Interval = 4000;
                }
                else
                {
                    const int gridsCount = 32;

                    // 在放大模式下，X轴和Y轴都各显示 gridsCount 个网格
                    int xGridInterval = (int)CurveChart.ChartAreas[0].AxisX.ScaleView.Size / gridsCount;
                    xGridInterval = xGridInterval >= 1 ? xGridInterval : 1;

                    

                    // 需要将坐标轴的Interval和MajorGrid的Interval设置成一样的值，这样保证坐标轴的刻度值和网格线对应
                    CurveChart.ChartAreas[0].AxisX.Interval = xGridInterval*2;
                    curveChartArea.AxisX.MajorGrid.Interval = xGridInterval;

                    curveChartArea.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                    curveChartArea.AxisX.MajorGrid.LineWidth = 1;

                    var yInterval = (int)curveChartArea.AxisY.ScaleView.Size / gridsCount;

                    // Y轴每两条网格线之间的单位，都是10的倍数
                    yInterval = yInterval >= 1 ? yInterval : 1;
                    yInterval = ((int)(yInterval / 10)) * 10;   

                    curveChartArea.AxisY.MajorGrid.Interval = yInterval;
                    curveChartArea.AxisY.Interval = yInterval;
                }
            }
        }

        /// <summary>
        /// 初始化Chart控件，设置Chart控件一些必要参数
        /// </summary>
        private void InitializeChart()
        {
            // 设置制图区
            var curveChartArea = new ChartArea {Name = "curveChartArea"};
            CurveChart.ChartAreas.Add(curveChartArea);

            // 设置制图区框
            curveChartArea.BorderDashStyle = ChartDashStyle.Solid;

            // 设置光标，使其可以选中放大
            //curveChartArea.CursorX.IsUserEnabled = true;
            curveChartArea.CursorX.IsUserSelectionEnabled = true;

            //curveChartArea.CursorY.IsUserEnabled = true;
            curveChartArea.CursorY.IsUserSelectionEnabled = true;

            // 设置绘图区域背景色
            curveChartArea.BackColor = ChartAreaBackgroundColor.ToDrawingColor();
            CurveChart.BackColor = ChartControlBackgroundColor.ToDrawingColor();

            // 设置坐标轴，及网格线
            // 横轴的主网格线，用于按每隔64个点显示，用于区分DA板
            curveChartArea.AxisX.LineColor = AxisColor.ToDrawingColor();
            curveChartArea.AxisX.MajorGrid.LineColor = AxisColor.ToDrawingColor();
            curveChartArea.AxisX.LabelStyle.ForeColor = ChartAxisLabelColor.ToDrawingColor();

            // 将横轴的最小字体设置为16，防止系统默认处理时使用特别小的字体
            //curveChartArea.AxisX.LabelAutoFitMinFontSize = 14;

            // 将横轴的标签文本脚本设置为0，防止系统默认处理时，可能会自适应的使用90度，不适于阅读
            curveChartArea.AxisX.LabelStyle.Angle = 0;

            curveChartArea.IsSameFontSizeForAllAxes = true;

            curveChartArea.AxisY.Maximum = YMaxValue;
            curveChartArea.AxisY.Minimum = YMinValue;

            curveChartArea.AxisY.LineColor = AxisColor.ToDrawingColor();
            curveChartArea.AxisY.MajorGrid.LineColor = AxisColor.ToDrawingColor();
            curveChartArea.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            curveChartArea.AxisY.LabelStyle.ForeColor = ChartAxisLabelColor.ToDrawingColor();

            // 显示滚动条，方便用户查看不同位置的点
            curveChartArea.AxisX.ScrollBar.Enabled = true;
            curveChartArea.AxisY.ScrollBar.Enabled = true;

            // 设置图例
            var curveChartLegend = new Legend
            {
                Name = "curveChartLegend",
                LegendStyle = LegendStyle.Row,
                Docking = Docking.Top
            };

            CurveChart.Legends.Add(curveChartLegend);

            // 右键菜单
            CurveChart.ContextMenu = new ContextMenu();

            AddCurve(_defaultCurve);

            // 默认分区显示，可以按DA板分区
            ShowBlockPartitions(true);
            UpdateBlockLabels();
            ShowHideDefaultCurve();
        }

        /// <summary>
        /// 使用默认的参数，添加一条新的曲线显示
        /// </summary>
        private void AddCurve(CurveDataSource curveSource)
        {
            // 添加一条曲线是新增一个Series
            var series = new Series
            {
                ChartArea = CurveChart.ChartAreas[0].Name,
                Legend = CurveChart.Legends[0].Name,
                ChartType = SeriesChartType.FastLine,
                Name = curveSource.CurveName,
                YValuesPerPoint = 1,
                Color = curveSource.CurveColor.ToDrawingColor(),
                BorderWidth = 2
            };

            series.Points.DataBindY(curveSource.DataPoints);
            CurveChart.Series.Add(series);
        }

        /// <summary>
        /// 鼠标双击处理函数，使曲线退出放大状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurveChart_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // 当鼠标双击区域为绘图空白区、曲线或者网格线时，取消缩放。
            var hitType = CurveChart.HitTest(e.X, e.Y).ChartElementType;
            if (hitType == ChartElementType.PlottingArea||
                hitType == ChartElementType.DataPoint ||
                hitType == ChartElementType.Gridlines)
            {
                // 使视图X、Y方向显示为实际大小，即没有放大
                CurveChart.ChartAreas[0].AxisX.ScaleView.Zoom(double.NaN, double.NaN);
                CurveChart.ChartAreas[0].AxisY.ScaleView.Zoom(double.NaN, double.NaN);
                ShowBlockPartitions(true);
            }
        }

        /// <summary>
        /// 鼠标按下事件处理函数，主要用于处理鼠标右键，初始化右键菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurveChart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (CurvesDataSource != null)
                {
                    // 鼠标右键被按下，更新右键菜单内容，包含冻结以及每条曲线的显示。右键菜单显示是在右键抬起时的系统处理
                    CurveChart.ContextMenu.MenuItems.Clear();

                    //// 冻结状态菜单
                    //MenuItem menuItem = new MenuItem("Freeze", ShortcutMenuClick) { Checked = Freezed };
                    //CurveChart.ContextMenu.MenuItems.Add(menuItem);

                    // 为每一条曲线增加其显示状态菜单
                    foreach (var curveSource in CurvesDataSource)
                    {
                        var menuItem = new MenuItem(curveSource.CurveName, ShortcutMenuClick)
                        {
                            Checked = curveSource.Visible
                        };
                        CurveChart.ContextMenu.MenuItems.Add(menuItem);
                    }
                }
            }
        }

        /// <summary>
        /// 更新曲线的可见性：
        /// </summary>
        private void UpdateCurvesVisibility()
        {
            foreach (var curve in CurvesDataSource)
            {
                var series = CurveChart.Series.FindByName(curve.CurveName);
                if (series != null)
                {
                    series.Enabled = curve.Visible;
                }
            }
        }

        /// <summary>
        /// 右键菜单（快捷菜单）点击事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShortcutMenuClick(object sender, EventArgs e)
        {
            var selectedMenuItem = sender as MenuItem;

            if (selectedMenuItem != null)
            {
                // 冻结或者取消冻结曲线
                if (selectedMenuItem.Text.Equals("Freeze"))
                {
                    Freezed = !Freezed;
                }
                else
                {
                    // 显示或者不显示某条曲线
                    CurvesDataSource.Single(t => t.CurveName.Equals(selectedMenuItem.Text)).Visible = !selectedMenuItem.Checked;
                    UpdateCurvesVisibility();
                }
            }
        }

        protected bool _disposed = false;

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                CurveChart.AxisViewChanged -= CurveChart_AxisViewChanged;
                CurveChart.GetToolTipText -= CurveChartOnGetToolTipText;
                CurveChart.MouseDown -= CurveChart_MouseDown;
                CurveChart.MouseDoubleClick -= CurveChart_MouseDoubleClick;

                CurveChart.Dispose();

                ChartHost.Dispose();
                this.Content = null;
                ChartHost = null;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

}
