param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '../FastGithub.UI/bin/Release/net45/win-x64/FastGithub.UI.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/ui')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Windows.Forms
$env:FASTGITHUB_UI_PREVIEW = '1'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$source = Join-Path $PSScriptRoot '../FastGithub.UI'
foreach ($name in @('Newtonsoft.Json','LiveCharts','LiveCharts.Wpf')) {
    [System.Reflection.Assembly]::LoadFrom((Join-Path $source "Resource/$name.dll")) | Out-Null
}
$app = New-Object System.Windows.Application
$assembly = [System.Reflection.Assembly]::LoadFrom([System.IO.Path]::GetFullPath($AssemblyPath))
[System.Windows.Application]::ResourceAssembly = $assembly
$window = $assembly.CreateInstance('FastGithub.UI.MainWindow')

function Capture([string]$name) {
    $window.UpdateLayout()
    $window.Dispatcher.Invoke([System.Action]{},[System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
    $visual = $window.Content
    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap([int]$visual.ActualWidth,[int]$visual.ActualHeight,96,96,[System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [System.IO.File]::Create((Join-Path $OutputDirectory "$name.png"))
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}

function FindNavigation($root, [string]$tag) {
    if ($root -is [System.Windows.Controls.RadioButton] -and $root.Tag -eq $tag) { return $root }
    for ($index = 0; $index -lt [System.Windows.Media.VisualTreeHelper]::GetChildrenCount($root); $index++) {
        $found = FindNavigation ([System.Windows.Media.VisualTreeHelper]::GetChild($root, $index)) $tag
        if ($found) { return $found }
    }
}

try {
    $window.Show()
    Capture 'overview-empty'
    $window.Width = 860
    $window.Height = 620
    Capture 'overview-compact'
    $window.Width = 980
    $window.Height = 720
    $window.FindName('Feedback').Text = '界面预览 · 以下流量和日志均为示例数据'
    $chart = $window.FindName('TrafficChart')
    $statistics = $assembly.CreateInstance('FastGithub.UI.FlowStatistics')
    $statistics.TotalRead = 18362170
    $statistics.TotalWrite = 348036112
    $statistics.ReadRate = 93452
    $statistics.WriteRate = 2576384
    $chart.Update($statistics)
    foreach ($series in $chart.Series) { $series.Values.Clear() }
    $tickType = $assembly.GetType('FastGithub.UI.FlowChart+RateTick')
    $epoch = New-Object DateTime(1970,1,1)
    $now = ([DateTime]::UtcNow - $epoch).TotalMilliseconds
    for ($index = 0; $index -lt 60; $index++) {
        $timestamp = $now - (59 - $index) * 1000
        $down = [double](1200000 + 900000 * [Math]::Sin($index / 5.0) + 500000 * [Math]::Sin($index / 2.8))
        $up = [double](70000 + 50000 * [Math]::Sin($index / 7.0))
        $chart.Series[0].Values.Add([Activator]::CreateInstance($tickType, @($down,$timestamp))) | Out-Null
        $chart.Series[1].Values.Add([Activator]::CreateInstance($tickType, @($up,$timestamp))) | Out-Null
    }
    Capture 'overview-sample'
    (FindNavigation $window 'Settings').IsChecked = $true
    Capture 'settings'
    $window.Width = 860
    $window.Height = 620
    Capture 'settings-compact'
    $window.Width = 980
    $window.Height = 720
    (FindNavigation $window 'Logs').IsChecked = $true
    Capture 'logs-empty'
    $logView = $window.FindName('LogView')
    foreach ($level in @(2,3,4)) {
        $log = $assembly.CreateInstance('FastGithub.UI.UdpLog')
        $log.Timestamp = [DateTime]::Now
        $log.Level = [Enum]::ToObject($assembly.GetType('FastGithub.UI.LogLevel'), $level)
        $log.SourceContext = 'FastGithub.Example'
        $log.Message = if ($level -eq 2) { '示例日志：本地代理已就绪，正在等待匹配的连接。' } elseif ($level -eq 3) { '示例警告：上游连接暂时未响应，将继续尝试其他可用地址。' } else { '示例错误：无法建立上游 TLS 连接。请检查当前网络和证书信任。详细上下文与较长的异常信息会自动换行，便于阅读与复制。' }
        $logView.AddLog($log)
    }
    Capture 'logs-sample'
    $logView.FindName('SearchBox').Text = '没有匹配项'
    Capture 'logs-filter-empty'
    $logView.FindName('SearchBox').Text = ''
    $logView.FindName('LevelFilter').SelectedIndex = 2
    $view = [System.Windows.Data.CollectionViewSource]::GetDefaultView($logView.LogList)
    if (@($view).Count -ne 1) { throw 'Error level filtering failed' }
    (FindNavigation $window 'Help').IsChecked = $true
    Capture 'help'
    $window.Width = 860
    $window.Height = 620
    Capture 'help-compact'
    $loggerType = $assembly.GetType('FastGithub.UI.UdpLogger')
    $port = $loggerType.GetProperty('Port').GetValue($null)
    $logView.GetType().GetMethod('InitUdpLoggerAsync', [System.Reflection.BindingFlags]'NonPublic,Instance').Invoke($logView, @()) | Out-Null
    $payload = [System.Text.Encoding]::UTF8.GetBytes('{"Timestamp":"2026-10-20T10:00:00","Level":2,"Message":"Native UDP regression","SourceContext":"Preview.Test"}')
    $sender = New-Object System.Net.Sockets.UdpClient
    try { $sender.Send($payload, $payload.Length, '127.0.0.1', $port) | Out-Null } finally { $sender.Dispose() }
    $deadline = [DateTime]::UtcNow.AddSeconds(3)
    while ($logView.LogList.Count -lt 4 -and [DateTime]::UtcNow -lt $deadline) {
        $window.Dispatcher.Invoke([System.Action]{}, [System.Windows.Threading.DispatcherPriority]::ApplicationIdle)
        [System.Threading.Thread]::Sleep(10)
    }
    if ($logView.LogList.Count -ne 4) { throw 'UDP log dispatch did not reach the WPF collection' }
    Write-Output 'Native screenshots, search/filter, and UDP-to-WPF dispatch checks passed. No core process was started.'
}
finally {
    $window.Close()
    if ($loggerType) { $loggerType.GetMethod('Close').Invoke($null, @()) | Out-Null }
    $app.Shutdown()
}


