using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;

namespace FastGithub.UI
{
    public class UdpLog
    {
        public DateTime Timestamp { get; set; }

        public LogLevel Level { get; set; }

        public string Message { get; set; } = string.Empty;

        public string SourceContext { get; set; } = string.Empty;

        public string Color => this.Level >= LogLevel.Error ? "#B43D35" : this.Level == LogLevel.Warning ? "#8A6500" : "#5D6B80";

        public string LevelName => this.Level >= LogLevel.Error ? "错误" : this.Level == LogLevel.Warning ? "警告" : this.Level == LogLevel.Information ? "信息" : "调试";

        /// <summary>
        /// 复制到剪贴板
        /// </summary>
        public void SetToClipboard()
        {
            Clipboard.SetText($"{this.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\r\n{this.Message}");
        }

    }

}
