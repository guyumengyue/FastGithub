using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace FastGithub.UI
{
    public partial class MainWindow : Window
    {
        private readonly CoreProcessController core = new CoreProcessController();
        private readonly HttpClient client = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(2) };
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private readonly System.Windows.Forms.NotifyIcon notifyIcon;
        private readonly System.Windows.Forms.MenuItem trayToggle;
        private UiSettings settings = new UiSettings();
        private bool initialized;
        private bool busy;
        private bool polling;
        private bool closing;
        private bool allowClose;
        private bool wasRunning;
        private DateTime startedAt;
        private string proxyAddress = "http://127.0.0.1:38457";

        public MainWindow()
        {
            InitializeComponent();
            var version = this.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            VersionText.Text = "v" + version;
            this.Title = "FastGithub";
            try
            {
                settings = UiSettings.Load();
                proxyAddress = "http://127.0.0.1:" + UiSettings.ReadProxyPort();
            }
            catch (Exception ex) { Feedback.Text = "读取设置失败，使用默认设置：" + ex.Message; }
            TransparentMode.IsChecked = settings.EnablePacketIntercept;
            ExplicitMode.IsChecked = !settings.EnablePacketIntercept;
            AutoStart.IsChecked = settings.AutoStart;
            ProxyAddress.Text = proxyAddress;
            UpdateMode();
            trayToggle = new System.Windows.Forms.MenuItem("启动服务");
            trayToggle.Click += async (s, e) => await ChangeServiceAsync();
            var show = new System.Windows.Forms.MenuItem("显示窗口", (s, e) => ShowWindow());
            var releases = new System.Windows.Forms.MenuItem("查看发布版本", (s, e) => OpenTarget("https://github.com/creazyboyone/FastGithub/releases"));
            var exit = new System.Windows.Forms.MenuItem("退出 FastGithub", (s, e) => Close());
            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "FastGithub · 已停止",
                Visible = !Program.PreviewMode,
                ContextMenu = new System.Windows.Forms.ContextMenu(new[] { show, trayToggle, releases, new System.Windows.Forms.MenuItem("-"), exit }),
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)
            };
            notifyIcon.MouseClick += (s, e) => { if (e.Button == System.Windows.Forms.MouseButtons.Left) ShowWindow(); };
            timer.Tick += async (s, e) => await PollAsync();
            Loaded += async (s, e) =>
            {
                if (initialized) return;
                initialized = true;
                if (Program.PreviewMode)
                {
                    Feedback.Text = "界面预览 · 未启动加速服务";
                    return;
                }
                timer.Start();
                if (settings.AutoStart) await ChangeServiceAsync();
            };
        }

        private void ShowWindow() { Show(); WindowState = WindowState.Normal; Activate(); }

        private void Navigate(object sender, RoutedEventArgs e)
        {
            if (OverviewPage == null) return;
            var page = ((RadioButton)sender).Tag as string;
            OverviewPage.Visibility = page == "Overview" ? Visibility.Visible : Visibility.Collapsed;
            LogsPage.Visibility = page == "Logs" ? Visibility.Visible : Visibility.Collapsed;
            SettingsPage.Visibility = page == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            HelpPage.Visibility = page == "Help" ? Visibility.Visible : Visibility.Collapsed;
            PageTitle.Text = page == "Logs" ? "运行日志" : page == "Settings" ? "连接设置" : page == "Help" ? "使用帮助" : "概览";
            PageSubtitle.Text = page == "Logs" ? "查看运行记录，定位连接问题" : page == "Settings" ? "选择适合当前网络环境的连接方式" : page == "Help" ? "证书信任、代理配置和常见问题" : "查看本地服务状态和实时流量";
        }

        private async void ToggleService(object sender, RoutedEventArgs e) => await ChangeServiceAsync();

        private async Task ChangeServiceAsync()
        {
            if (busy || closing || Program.PreviewMode) return;
            busy = true;
            ToggleButton.IsEnabled = false;
            trayToggle.Enabled = false;
            try
            {
                if (core.IsRunning)
                {
                    SetState("正在停止", "等待当前服务退出并恢复网络设置。", "#8A6500");
                    ToggleButton.Content = "正在停止…";
                    await core.StopAsync();
                    wasRunning = false;
                    SetStopped();
                }
                else
                {
                    TrafficChart.Reset();
                    core.Start(settings.EnablePacketIntercept);
                    wasRunning = true;
                    startedAt = DateTime.Now;
                    SetState("正在启动", "正在准备本地服务，请稍候。", "#8A6500");
                    ToggleButton.Content = "停止服务";
                    ModeOptions.IsEnabled = false;
                    Feedback.Text = "本地服务状态不代表 GitHub 当前可达";
                }
            }
            catch (Exception ex)
            {
                if (!core.IsRunning) SetStopped();
                SetState("操作未完成", ex.Message, "#B43D35");
                Feedback.Text = "请查看运行日志；确认没有其他 FastGithub 实例正在运行。";
            }
            finally
            {
                busy = false;
                ToggleButton.IsEnabled = true;
                ToggleButton.Content = core.IsRunning ? "停止服务" : "启动服务";
                trayToggle.Text = core.IsRunning ? "停止服务" : "启动服务";
                trayToggle.Enabled = true;
                ModeOptions.IsEnabled = !core.IsRunning;
            }
        }

        private async Task PollAsync()
        {
            if (polling || busy || closing) return;
            polling = true;
            var statisticsUri = core.StatisticsUri;
            try
            {
                if (!core.IsRunning)
                {
                    if (wasRunning)
                    {
                        wasRunning = false;
                        SetStopped();
                        SetState("服务已退出", "核心程序已经退出，请查看运行日志后重新启动。", "#B43D35");
                    }
                    return;
                }
                if (statisticsUri == null) return;
                using var response = await client.GetAsync(statisticsUri);
                var statistics = JsonConvert.DeserializeObject<FlowStatistics>(await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync());
                if (statistics == null) throw new InvalidDataException("未收到有效的流量数据");
                if (closing || busy || !core.IsRunning || statisticsUri != core.StatisticsUri) return;
                TrafficChart.Update(statistics);
                SetState("服务运行中", "本地代理已就绪，匹配的流量会通过 FastGithub。", "#24734A");
                notifyIcon.Text = "FastGithub · 服务运行中";
            }
            catch (Exception)
            {
                if (!closing && !busy && core.IsRunning && statisticsUri == core.StatisticsUri)
                {
                    var waiting = DateTime.Now - startedAt < TimeSpan.FromSeconds(12);
                    SetState(waiting ? "正在启动" : "服务未就绪",
                        waiting ? "等待本地服务响应…" : "暂时无法读取运行状态，请查看日志或停止后重试。", "#8A6500");
                    TrafficChart.SetUnavailable();
                }
            }
            finally { polling = false; }
        }

        private void SetState(string state, string hint, string color)
        {
            StateText.Text = state;
            System.Windows.Automation.AutomationProperties.SetName(StateText, state + "。" + hint);
            StateHint.Text = hint;
            StateDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private void SetStopped()
        {
            SetState("已停止", "启动服务后，流量数据将在下方显示。", "#79869A");
            ToggleButton.Content = "启动服务";
            trayToggle.Text = "启动服务";
            ModeOptions.IsEnabled = true;
            TrafficChart.SetUnavailable();
            notifyIcon.Text = "FastGithub · 已停止";
        }

        private void UpdateMode() => ModeText.Text = settings.EnablePacketIntercept ? "透明模式" : "显式代理";

        private void SettingsChanged(object sender, RoutedEventArgs e)
        {
            if (!initialized || busy || closing) return;
            settings.AutoStart = AutoStart.IsChecked == true;
            settings.EnablePacketIntercept = TransparentMode.IsChecked == true;
            UpdateMode();
            if (Program.PreviewMode) return;
            try { settings.Save(); Feedback.Text = "设置已保存，连接模式在下次启动服务时生效。"; }
            catch (Exception ex) { Feedback.Text = "无法保存设置：" + ex.Message; }
        }

        private void OpenTarget(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { Feedback.Text = "无法打开：" + ex.Message; }
        }

        private void OpenFolder(string name)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            if (!Directory.Exists(path)) { Feedback.Text = "该文件夹尚未生成，请先启动一次服务。"; return; }
            OpenTarget(path);
        }
        private void OpenGithub(object sender, RoutedEventArgs e) => OpenTarget("https://github.com");
        private void OpenLogsFolder(object sender, RoutedEventArgs e) => OpenFolder("logs");
        private void OpenCertificatesFolder(object sender, RoutedEventArgs e) => OpenFolder("cacert");
        private void OpenConfigurationFolder(object sender, RoutedEventArgs e) => OpenTarget(AppDomain.CurrentDomain.BaseDirectory);
        private void CopyProxyAddress(object sender, RoutedEventArgs e)
        {
            try { Clipboard.SetText(proxyAddress); Feedback.Text = "已复制代理地址：" + proxyAddress; }
            catch (Exception) { Feedback.Text = "剪贴板暂时不可用，请稍后重试。"; }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = (HwndSource)PresentationSource.FromVisual(this);
            source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (!Program.PreviewMode && msg == 0x112 && (wParam.ToInt32() & 0xFFF0) == 0xF060)
                {
                    Hide();
                    handled = true;
                }
                return IntPtr.Zero;
            });
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (!allowClose && !Program.PreviewMode)
            {
                e.Cancel = true;
                if (!closing)
                {
                    closing = true;
                    timer.Stop();
                    ToggleButton.IsEnabled = false;
                    try
                    {
                        SetState("正在退出", "等待服务正常停止并恢复网络设置。", "#8A6500");
                        await core.StopAsync();
                        allowClose = true;
                        Close();
                    }
                    catch (Exception ex)
                    {
                        closing = false;
                        ToggleButton.IsEnabled = true;
                        timer.Start();
                        ShowWindow();
                        SetState("服务仍在退出", ex.Message, "#B43D35");
                    }
                }
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            Program.IsClosing = true;
            timer.Stop();
            core.RequestStop();
            core.Dispose();
            client.Dispose();
            notifyIcon.Dispose();
            if (!Program.PreviewMode) UdpLogger.Close();
            base.OnClosed(e);
        }
    }
}
