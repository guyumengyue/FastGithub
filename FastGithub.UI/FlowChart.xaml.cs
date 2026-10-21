using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FastGithub.UI
{
    public partial class FlowChart : UserControl
    {
        private readonly LineSeries readSeries = new LineSeries
        {
            Title = "上行速率", PointGeometry = null, LineSmoothness = 0,
            Stroke = new SolidColorBrush(Color.FromRgb(124, 139, 164)), Fill = Brushes.Transparent,
            StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 3 },
            Values = new ChartValues<RateTick>()
        };
        private readonly LineSeries writeSeries = new LineSeries
        {
            Title = "下行速率", PointGeometry = null, LineSmoothness = 0,
            Stroke = new SolidColorBrush(Color.FromRgb(36, 95, 196)),
            Fill = new SolidColorBrush(Color.FromArgb(18, 36, 95, 196)), StrokeThickness = 2,
            Values = new ChartValues<RateTick>()
        };
        public SeriesCollection Series { get; } = new SeriesCollection(Mappers.Xy<RateTick>().X(item => item.Timestamp).Y(item => item.Rate));
        public Func<double, string> XFormatter { get; } = timestamp => new DateTime(1970, 1, 1).AddMilliseconds(timestamp).ToLocalTime().ToString("HH:mm:ss");
        public Func<double, string> YFormatter { get; } = value => FlowStatistics.ToNetworkSizeString((long)value) + "/s";

        public FlowChart()
        {
            InitializeComponent();
            Series.Add(writeSeries);
            Series.Add(readSeries);
            DataContext = this;
        }

        public void Update(FlowStatistics statistics)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            flowChart.Visibility = Visibility.Visible;
            ReadRate.Text = FlowStatistics.ToNetworkSizeString((long)statistics.ReadRate) + "/s";
            WriteRate.Text = FlowStatistics.ToNetworkSizeString((long)statistics.WriteRate) + "/s";
            textBlockRead.Text = "累计发送 " + FlowStatistics.ToNetworkSizeString(statistics.TotalRead);
            textBlockWrite.Text = "累计接收 " + FlowStatistics.ToNetworkSizeString(statistics.TotalWrite);
            var timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            readSeries.Values.Add(new RateTick(statistics.ReadRate, timestamp));
            writeSeries.Values.Add(new RateTick(statistics.WriteRate, timestamp));
            while (readSeries.Values.Count > 60)
            {
                readSeries.Values.RemoveAt(0);
                writeSeries.Values.RemoveAt(0);
            }
            UpdateTime.Text = "更新于 " + DateTime.Now.ToString("HH:mm:ss");
        }

        public void SetUnavailable()
        {
            ReadRate.Text = "—";
            WriteRate.Text = "—";
            UpdateTime.Text = readSeries.Values.Count == 0 ? "最近 60 秒" : "上次运行的数据";
        }

        public void Reset()
        {
            readSeries.Values.Clear();
            writeSeries.Values.Clear();
            textBlockRead.Text = "累计发送 —";
            textBlockWrite.Text = "累计接收 —";
            EmptyState.Visibility = Visibility.Visible;
            flowChart.Visibility = Visibility.Collapsed;
            SetUnavailable();
        }

        private sealed class RateTick
        {
            public double Rate { get; }
            public double Timestamp { get; }
            public RateTick(double rate, double timestamp) { Rate = rate; Timestamp = timestamp; }
        }
    }
}
