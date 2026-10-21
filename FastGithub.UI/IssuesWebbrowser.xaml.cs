using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FastGithub.UI
{
    public partial class IssuesWebbrowser : UserControl
    {
        public IssuesWebbrowser() { InitializeComponent(); }
        private void OpenLink(object sender, RequestNavigateEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception ex) { HelpFeedback.Text = "无法打开浏览器：" + ex.Message; }
            e.Handled = true;
        }
    }
}
