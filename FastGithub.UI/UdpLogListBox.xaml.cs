using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FastGithub.UI
{
    public partial class UdpLogListBox : UserControl
    {
        private ICollectionView? view;
        public ObservableCollection<UdpLog> LogList { get; } = new ObservableCollection<UdpLog>();

        public UdpLogListBox()
        {
            InitializeComponent();
            DataContext = this;
            view = CollectionViewSource.GetDefaultView(LogList);
            view.Filter = Match;
            if (!Program.PreviewMode) InitUdpLoggerAsync();
        }

        private bool Match(object item)
        {
            var log = (UdpLog)item;
            if (LevelFilter.SelectedIndex == 1 && log.Level < LogLevel.Warning) return false;
            if (LevelFilter.SelectedIndex == 2 && log.Level < LogLevel.Error) return false;
            var query = SearchBox.Text.Trim();
            return query.Length == 0 || log.Message.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                log.SourceContext.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void AddLog(UdpLog log)
        {
            LogList.Insert(0, log);
            while (LogList.Count > 500) LogList.RemoveAt(LogList.Count - 1);
            UpdateEmptyState();
            if (PauseScroll.IsChecked != true && IsVisible && view != null && view.Contains(log))
                listBox.ScrollIntoView(log);
        }

        private async void InitUdpLoggerAsync()
        {
            while (!Program.IsClosing)
            {
                try
                {
                    var log = await UdpLogger.GetUdpLogAsync();
                    if (log != null && !Program.IsClosing)
                    {
                        // 窗口构造阶段可能尚无同步上下文，显式回到 WPF 线程更新集合。
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (!Program.IsClosing) AddLog(log);
                        });
                    }
                }
                catch (Exception)
                {
                    if (!Program.IsClosing) await Task.Delay(500);
                }
            }
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (view == null) return;
            view.Refresh();
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            var count = view?.Cast<object>().Count() ?? 0;
            EmptyLogs.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyTitle.Text = LogList.Count == 0 ? "等待运行日志" : "没有匹配的日志";
            EmptyHint.Text = LogList.Count == 0 ? "启动服务后，连接和诊断记录会显示在这里。" : "尝试更换关键词或选择其他日志级别。";
            LogFeedback.Text = count + " 条记录 · 最多保留 500 条";
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CopyButton != null) CopyButton.IsEnabled = listBox.SelectedItem != null;
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!(listBox.SelectedItem is UdpLog log)) return;
            try { log.SetToClipboard(); LogFeedback.Text = "已复制选中的日志"; }
            catch (Exception) { LogFeedback.Text = "剪贴板暂时不可用，请稍后重试。"; }
        }

        private void MenuItem_Clear_Click(object sender, RoutedEventArgs e)
        {
            LogList.Clear();
            UpdateEmptyState();
        }

        private void ExportLogs(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "文本文件 (*.txt)|*.txt", FileName = "fastgithub-log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt" };
            if (dialog.ShowDialog() != true) return;
            try
            {
                var logs = view!.Cast<UdpLog>().Select(log => $"{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{log.Level}] {log.SourceContext}\r\n{log.Message}");
                File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine + Environment.NewLine, logs), Encoding.UTF8);
                LogFeedback.Text = "已导出当前筛选结果";
            }
            catch (Exception ex) { LogFeedback.Text = "无法导出日志：" + ex.Message; }
        }
    }
}
