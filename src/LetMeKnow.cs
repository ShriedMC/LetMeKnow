using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("LetMeKnow")]
[assembly: AssemblyDescription("Desktop Notification Utility")]
[assembly: AssemblyCompany("LetMeKnow")]
[assembly: AssemblyProduct("LetMeKnow")]
[assembly: AssemblyCopyright("Copyright (c) 2026")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace LetMeKnow
{
    public class Config
    {
        public bool SoundEnabled = true;
        public bool LouderAlertEnabled = true;
        public int DisplayDurationSeconds = 8;
        public bool WindowsActionCenter = true;
        public bool AutoDismiss = true;
        public bool FloatingCardEnabled = true;
        public bool FollowMouseCursor = true;
        public bool McpEnabled = true;
        public bool TrayNotificationFallback = true;

        private static string GetConfigPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "LetMeKnow", "config.ini");
        }

        public static Config Load()
        {
            Config cfg = new Config();
            string path = GetConfigPath();
            if (File.Exists(path))
            {
                try
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        int idx = trimmed.IndexOf('=');
                        if (idx > 0)
                        {
                            string key = trimmed.Substring(0, idx).Trim();
                            string val = trimmed.Substring(idx + 1).Trim();

                            if (key.Equals("SoundEnabled", StringComparison.OrdinalIgnoreCase))
                                cfg.SoundEnabled = ParseBool(val, true);
                            else if (key.Equals("LouderAlertEnabled", StringComparison.OrdinalIgnoreCase))
                                cfg.LouderAlertEnabled = ParseBool(val, true);
                            else if (key.Equals("DisplayDurationSeconds", StringComparison.OrdinalIgnoreCase))
                            {
                                int secs;
                                if (int.TryParse(val, out secs)) cfg.DisplayDurationSeconds = secs;
                            }
                            else if (key.Equals("WindowsActionCenter", StringComparison.OrdinalIgnoreCase))
                                cfg.WindowsActionCenter = ParseBool(val, true);
                            else if (key.Equals("AutoDismiss", StringComparison.OrdinalIgnoreCase))
                                cfg.AutoDismiss = ParseBool(val, true);
                            else if (key.Equals("FloatingCardEnabled", StringComparison.OrdinalIgnoreCase))
                                cfg.FloatingCardEnabled = ParseBool(val, true);
                            else if (key.Equals("FollowMouseCursor", StringComparison.OrdinalIgnoreCase))
                                cfg.FollowMouseCursor = ParseBool(val, true);
                            else if (key.Equals("McpEnabled", StringComparison.OrdinalIgnoreCase))
                                cfg.McpEnabled = ParseBool(val, true);
                            else if (key.Equals("TrayNotificationFallback", StringComparison.OrdinalIgnoreCase))
                                cfg.TrayNotificationFallback = ParseBool(val, true);
                        }
                    }
                }
                catch { }
            }
            return cfg;
        }

        public void Save()
        {
            try
            {
                string path = GetConfigPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# LetMeKnow configuration");
                sb.AppendLine("SoundEnabled=" + SoundEnabled.ToString().ToLower());
                sb.AppendLine("LouderAlertEnabled=" + LouderAlertEnabled.ToString().ToLower());
                sb.AppendLine("DisplayDurationSeconds=" + DisplayDurationSeconds);
                sb.AppendLine("WindowsActionCenter=" + WindowsActionCenter.ToString().ToLower());
                sb.AppendLine("AutoDismiss=" + AutoDismiss.ToString().ToLower());
                sb.AppendLine("FloatingCardEnabled=" + FloatingCardEnabled.ToString().ToLower());
                sb.AppendLine("FollowMouseCursor=" + FollowMouseCursor.ToString().ToLower());
                sb.AppendLine("McpEnabled=" + McpEnabled.ToString().ToLower());
                sb.AppendLine("TrayNotificationFallback=" + TrayNotificationFallback.ToString().ToLower());
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static bool ParseBool(string val, bool def)
        {
            if (val.Equals("true", StringComparison.OrdinalIgnoreCase) || val.Equals("1") || val.Equals("yes", StringComparison.OrdinalIgnoreCase))
                return true;
            if (val.Equals("false", StringComparison.OrdinalIgnoreCase) || val.Equals("0") || val.Equals("no", StringComparison.OrdinalIgnoreCase))
                return false;
            return def;
        }
    }

    class Program
    {
        public static bool IsMcpMode = false;

        [STAThread]
        static void Main(string[] args)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }

            if (args.Length == 0)
            {
                Wizard.RunSetupWizard();
                return;
            }

            string first = args[0].ToLower().Trim();

            if (first == "--config" || first == "config" || first == "--setup" || first == "setup")
            {
                Wizard.RunSetupWizard();
                return;
            }

            if (first == "-h" || first == "--help" || first == "help")
            {
                PrintHelp();
                return;
            }

            if (first == "--install" || first == "install")
            {
                Installer.Install(Config.Load());
                return;
            }

            if (first == "--uninstall" || first == "uninstall")
            {
                Installer.Uninstall();
                return;
            }

            if (first == "--mcp" || first == "mcp")
            {
                McpServer.Run();
                return;
            }

            if (first == "--mcp-config" || first == "mcp-config" || first == "--mcp-setup")
            {
                McpServer.PrintMcpConfig();
                return;
            }

            if (first == "--mcp-toggle" || first == "mcp-toggle")
            {
                Config cfg = Config.Load();
                cfg.McpEnabled = !cfg.McpEnabled;
                cfg.Save();
                Console.WriteLine("MCP Server is now: {0}", cfg.McpEnabled ? "ENABLED" : "DISABLED");
                return;
            }

            if (first == "--mcp-prompt" || first == "mcp-prompt")
            {
                McpServer.PrintAndCopyMcpPrompt();
                return;
            }

            if (first == "--mcp-connect" || first == "mcp-connect" || first == "--mcp-install")
            {
                McpServer.AutoConnectAll(Directory.GetCurrentDirectory());
                return;
            }

            if (first == "--prompt" || first == "prompt" || first == "-p" || first == "--universal-prompt")
            {
                AgentInstaller.PrintUniversalPrompt();
                return;
            }

            if (first == "--install-agents" || first == "--install-agent" || first == "--setup-agents")
            {
                string targetAgent = args.Length > 1 ? args[1].ToLower().Trim() : "all";
                string currentDir = Directory.GetCurrentDirectory();
                if (targetAgent == "claude" || targetAgent == "claude-code")
                    AgentInstaller.InstallClaudeCode(currentDir);
                else if (targetAgent == "cursor")
                    AgentInstaller.InstallCursor(currentDir);
                else if (targetAgent == "codex" || targetAgent == "copilot")
                    AgentInstaller.InstallCodex(currentDir);
                else if (targetAgent == "windsurf")
                    AgentInstaller.InstallWindsurf(currentDir);
                else
                    AgentInstaller.InstallAll(currentDir);
                return;
            }

            if (first == "--install-claude" || first == "--setup-claude")
            {
                AgentInstaller.InstallClaudeCode(Directory.GetCurrentDirectory());
                return;
            }

            if (first == "--install-cursor" || first == "--setup-cursor")
            {
                AgentInstaller.InstallCursor(Directory.GetCurrentDirectory());
                return;
            }

            if (first == "--install-codex" || first == "--setup-codex" || first == "--install-copilot")
            {
                AgentInstaller.InstallCodex(Directory.GetCurrentDirectory());
                return;
            }

            if (first == "--install-windsurf" || first == "--setup-windsurf")
            {
                AgentInstaller.InstallWindsurf(Directory.GetCurrentDirectory());
                return;
            }

            if (first == "--install-all" || first == "--install-all-agents")
            {
                AgentInstaller.InstallAll(Directory.GetCurrentDirectory());
                return;
            }

            if (first == "--history" || first == "history" || first == "--logs" || first == "logs")
            {
                ShowHistory();
                return;
            }

            if (first == "--test" || first == "test")
            {
                ShowNotification("Test", "Desktop notifications are working.", "Sent from lmk test", false);
                return;
            }

            // Internal background process: --render <who> <what> <details> <isUrgent>
            if (first == "--render" && args.Length >= 3)
            {
                string rWho = DecodeArg(args[1]);
                string rWhat = DecodeArg(args[2]);
                string rDetails = args.Length >= 4 ? DecodeArg(args[3]) : "";
                bool rUrgent = args.Length >= 5 && args[4].Equals("true", StringComparison.OrdinalIgnoreCase);
                RenderNotificationWindow(rWho, rWhat, rDetails, rUrgent);
                return;
            }

            bool isUrgent = false;
            List<string> cleanArgs = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a.Equals("-u", StringComparison.OrdinalIgnoreCase) || a.Equals("--urgency", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && (args[i + 1].Equals("high", StringComparison.OrdinalIgnoreCase) || args[i + 1].Equals("critical", StringComparison.OrdinalIgnoreCase)))
                    {
                        isUrgent = true;
                        i++;
                    }
                }
                else if (a.Equals("--urgent", StringComparison.OrdinalIgnoreCase) || a.Equals("--loud", StringComparison.OrdinalIgnoreCase))
                {
                    isUrgent = true;
                }
                else
                {
                    cleanArgs.Add(a);
                }
            }

            string who = "Alert";
            string what = "Attention needed.";
            string details = "";

            if (cleanArgs.Count == 1)
            {
                what = cleanArgs[0];
            }
            else if (cleanArgs.Count == 2)
            {
                who = cleanArgs[0];
                what = cleanArgs[1];
            }
            else if (cleanArgs.Count >= 3)
            {
                who = cleanArgs[0];
                what = cleanArgs[1];
                details = string.Join(" ", cleanArgs.ToArray(), 2, cleanArgs.Count - 2);
            }

            if (!isUrgent)
            {
                string combined = (what + " " + details).ToLower();
                if (combined.Contains("error") || combined.Contains("fail") || combined.Contains("urgent") ||
                    combined.Contains("critical") || combined.Contains("blocked") || combined.Contains("conflict") ||
                    combined.Contains("crash"))
                {
                    isUrgent = true;
                }
            }

            ShowNotification(who, what, details, isUrgent);
        }

        public static void ShowNotification(string who, string what, string details, bool isUrgent)
        {
            Config cfg = Config.Load();

            // Always log to persistent history file so user can review missed alerts
            LogNotification(who, what, details, isUrgent);

            // 1. Play audio chime synchronously so user hears it immediately before CLI exits
            if (cfg.SoundEnabled)
            {
                try
                {
                    if (isUrgent && cfg.LouderAlertEnabled)
                    {
                        System.Media.SystemSounds.Exclamation.Play();
                        try
                        {
                            Console.Beep(880, 100);
                            Console.Beep(1175, 180);
                        }
                        catch { }
                    }
                    else
                    {
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                }
                catch { }
            }

            // 2. Windows Action Center notification (logs to Action Center Win + N)
            if (cfg.WindowsActionCenter)
            {
                SendWindowsToast(who, what, details);
            }

            // 3. Floating HUD desktop card (toggable via FloatingCardEnabled)
            if (cfg.FloatingCardEnabled)
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                string renderArgs = string.Format("--render {0} {1} {2} {3}",
                    EncodeArg(who), EncodeArg(what), EncodeArg(details), isUrgent ? "true" : "false");

                bool launched = false;

                // Strategy 1: COM Shell.Application (explorer.exe)
                // Detaches completely from calling process and any agent job sandbox, running under desktop shell
                try
                {
                    Type shellType = Type.GetTypeFromProgID("Shell.Application");
                    if (shellType != null)
                    {
                        dynamic shell = Activator.CreateInstance(shellType);
                        shell.ShellExecute(exePath, renderArgs, "", "open", 1);
                        launched = true;
                    }
                }
                catch { }

                // Strategy 2: ProcessStartInfo with UseShellExecute = true (Win32 ShellExecuteEx)
                if (!launched)
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = exePath;
                        psi.Arguments = renderArgs;
                        psi.UseShellExecute = true;
                        psi.WindowStyle = ProcessWindowStyle.Normal;
                        using (Process p = Process.Start(psi))
                        {
                            if (p != null) launched = true;
                        }
                    }
                    catch { }
                }

                // Strategy 3: cmd.exe /c start "" "{exePath}" {renderArgs}
                // Spawns detached process directly under Windows desktop shell
                if (!launched)
                {
                    try
                    {
                        ProcessStartInfo psiCmd = new ProcessStartInfo();
                        psiCmd.FileName = "cmd.exe";
                        psiCmd.Arguments = string.Format("/c start \"\" \"{0}\" {1}", exePath, renderArgs);
                        psiCmd.CreateNoWindow = true;
                        psiCmd.UseShellExecute = false;
                        psiCmd.WindowStyle = ProcessWindowStyle.Hidden;
                        using (Process p = Process.Start(psiCmd))
                        {
                            if (p != null)
                            {
                                p.WaitForExit(1000);
                                launched = true;
                            }
                        }
                    }
                    catch { }
                }

                // Strategy 4: Fallback inline rendering if background spawns fail
                if (!launched)
                {
                    try
                    {
                        RenderNotificationWindow(who, what, details, isUrgent);
                    }
                    catch { }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            if (!IsMcpMode)
            {
                Console.WriteLine("[lmk] sent: [{0}] {1}", who, what);
            }
            else
            {
                Console.Error.WriteLine("[lmk-mcp] sent: [{0}] {1}", who, what);
            }
        }

        static string EncodeArg(string val)
        {
            if (string.IsNullOrEmpty(val)) return "\"\"";
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            return Convert.ToBase64String(bytes);
        }

        static string DecodeArg(string val)
        {
            if (string.IsNullOrEmpty(val) || val == "\"\"") return "";
            try
            {
                byte[] bytes = Convert.FromBase64String(val);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return val;
            }
        }

        static void RenderNotificationWindow(string who, string what, string details, bool isUrgent)
        {
            Config cfg = Config.Load();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (ToastForm form = new ToastForm(who, what, details, cfg, isUrgent))
            {
                Application.Run(form);
            }
        }

        static void LogNotification(string who, string what, string details, bool isUrgent)
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logPath = Path.Combine(localAppData, "LetMeKnow", "history.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                string entry = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}{3}{4}",
                    DateTime.Now,
                    who,
                    isUrgent ? "(URGENT) " : "",
                    what,
                    string.IsNullOrEmpty(details) ? "" : " - " + details);
                File.AppendAllText(logPath, entry + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        static void ShowHistory()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logPath = Path.Combine(localAppData, "LetMeKnow", "history.log");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.WriteLine("  |  LetMeKnow Alert History                               |");
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.ResetColor();
            Console.WriteLine();

            if (!File.Exists(logPath))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No notifications recorded yet.");
                Console.ResetColor();
                Console.WriteLine();
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(logPath);
                int count = Math.Min(lines.Length, 10);
                for (int i = lines.Length - count; i < lines.Length; i++)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("  * " + lines[i]);
                }
                Console.ResetColor();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading history: " + ex.Message);
            }
        }

        static void SendWindowsToast(string who, string what, string details)
        {
            try
            {
                string body = string.IsNullOrEmpty(details) ? what : what + "\n" + details;
                string safeWho = XmlEscape(who);
                string safeBody = XmlEscape(body);

                string psScript = string.Format(@"
try {{
    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
    [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
    $xml = @""
<toast duration='long'>
    <visual>
        <binding template='ToastGeneric'>
            <text>{0}</text>
            <text>{1}</text>
            <text placement='attribution'>LetMeKnow</text>
        </binding>
    </visual>
    <audio silent='true' />
</toast>
""@
    $doc = New-Object Windows.Data.Xml.Dom.XmlDocument
    $doc.LoadXml($xml)
    $toast = New-Object Windows.UI.Notifications.ToastNotification $doc
    $appId = 'LetMeKnow'
    try {{
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($appId).Show($toast)
    }} catch {{
        $appId = '{{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}}\WindowsPowerShell\v1.0\powershell.exe'
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($appId).Show($toast)
    }}
}} catch {{}}
", safeWho, safeBody);

                byte[] bytes = Encoding.Unicode.GetBytes(psScript);
                string base64 = Convert.ToBase64String(bytes);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + base64;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(2000);
                }
            }
            catch { }
        }

        static string XmlEscape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }


        static void PrintHelp()
        {
            Console.WriteLine("lmk - desktop notifications for CLI, coding agents, and MCP");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  lmk <who> <what> [<details>]    Send notification");
            Console.WriteLine("  lmk --urgent <who> <what>       Send high-priority notification");
            Console.WriteLine("  lmk --mcp                       Run Model Context Protocol (MCP) server");
            Console.WriteLine("  lmk --mcp-prompt                Display & copy AI prompt to auto-connect MCP");
            Console.WriteLine("  lmk --mcp-connect               Auto-connect MCP to detected coding agents");
            Console.WriteLine("  lmk --mcp-config                Show raw MCP configuration snippets");
            Console.WriteLine("  lmk --mcp-toggle                Toggle MCP integration on / off");
            Console.WriteLine("  lmk --install-agents [name]     Directly configure agents (all/claude/cursor/codex)");
            Console.WriteLine("  lmk --prompt                    Display & copy universal AI agent prompt");
            Console.WriteLine("  lmk --history                   View recent notification history");
            Console.WriteLine("  lmk --config                    Run setup wizard (interactive settings)");
            Console.WriteLine("  lmk --test                      Send test notification");
            Console.WriteLine("  lmk --install                   Install globally (PATH & PowerShell)");
            Console.WriteLine("  lmk --uninstall                 Remove application and settings");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  lmk Claude \"Need Stripe API key in .env\"");
            Console.WriteLine("  lmk Antigravity \"Review needed on line 42\"");
            Console.WriteLine("  lmk --urgent Cursor \"Build failed: TS2307\"");
            Console.WriteLine("  lmk --install-agents all");
            Console.WriteLine("  lmk --prompt");
            Console.WriteLine("  lmk --mcp");
        }
    }

    public class ToastForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private System.Windows.Forms.Timer autoCloseTimer;
        private System.Windows.Forms.Timer topTimer;
        private NotifyIcon trayIcon;
        private string who;
        private string what;
        private string details;
        private Config config;
        private bool isUrgent;

        public ToastForm(string who, string what, string details, Config config, bool isUrgent)
        {
            this.who = who;
            this.what = what;
            this.details = details;
            this.config = config;
            this.isUrgent = isUrgent;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(28, 28, 30);

            int cardWidth = 420;
            int cardHeight = string.IsNullOrEmpty(details) ? 105 : 150;
            this.Size = new Size(cardWidth, cardHeight);

            // Multi-monitor support: follow active cursor screen
            Rectangle screen = (config != null && config.FollowMouseCursor)
                ? Screen.FromPoint(Cursor.Position).WorkingArea
                : Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - cardWidth - 20, screen.Bottom - cardHeight - 20);

            this.Shown += delegate
            {
                this.BringToFront();
                ForceTopMost();
            };

            // Periodic reinforcement to stay above fullscreen IDEs / games
            topTimer = new System.Windows.Forms.Timer();
            topTimer.Interval = 500;
            topTimer.Tick += delegate { ForceTopMost(); };
            topTimer.Start();

            // Tray balloon notification fallback
            if (config != null && config.TrayNotificationFallback)
            {
                try
                {
                    trayIcon = new NotifyIcon();
                    trayIcon.Icon = SystemIcons.Information;
                    trayIcon.Visible = true;
                    string trayTitle = (isUrgent ? "[URGENT] " : "[ALERT] ") + who;
                    string trayBody = string.IsNullOrEmpty(details) ? what : what + "\n" + details;
                    if (trayBody.Length > 240) trayBody = trayBody.Substring(0, 237) + "...";
                    trayIcon.ShowBalloonTip(4000, trayTitle, trayBody, isUrgent ? ToolTipIcon.Warning : ToolTipIcon.Info);
                }
                catch { }
            }

            // Subtle border
            Panel border = new Panel();
            border.Dock = DockStyle.Fill;
            border.BackColor = Color.FromArgb(45, 45, 48);
            this.Controls.Add(border);

            Panel inner = new Panel();
            inner.Size = new Size(cardWidth - 2, cardHeight - 2);
            inner.Location = new Point(1, 1);
            inner.BackColor = Color.FromArgb(28, 28, 30);
            border.Controls.Add(inner);

            // Accent bar
            Panel stripe = new Panel();
            stripe.Size = new Size(4, cardHeight);
            stripe.Location = new Point(0, 0);
            stripe.BackColor = isUrgent ? Color.FromArgb(220, 53, 69) : Color.FromArgb(0, 122, 255);
            inner.Controls.Add(stripe);

            // App identifier header
            Label lblHeader = new Label();
            lblHeader.Text = isUrgent ? "LetMeKnow - Urgent" : "LetMeKnow";
            lblHeader.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblHeader.ForeColor = isUrgent ? Color.FromArgb(240, 100, 110) : Color.FromArgb(142, 142, 147);
            lblHeader.Location = new Point(16, 10);
            lblHeader.Size = new Size(200, 16);
            inner.Controls.Add(lblHeader);

            // Dismiss button
            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            btnClose.ForeColor = Color.FromArgb(142, 142, 147);
            btnClose.BackColor = Color.Transparent;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(24, 24);
            btnClose.Location = new Point(cardWidth - 30, 4);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += delegate { this.Close(); };
            inner.Controls.Add(btnClose);

            // Title (Agent / Sender)
            Label lblTitle = new Label();
            lblTitle.Text = who;
            lblTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(255, 255, 255);
            lblTitle.Location = new Point(16, 28);
            lblTitle.Size = new Size(cardWidth - 50, 22);
            inner.Controls.Add(lblTitle);

            // Message text
            Label lblMsg = new Label();
            lblMsg.Text = what;
            lblMsg.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblMsg.ForeColor = Color.FromArgb(220, 220, 224);
            lblMsg.Location = new Point(16, 52);
            lblMsg.Size = new Size(cardWidth - 32, 42);
            inner.Controls.Add(lblMsg);

            if (!string.IsNullOrEmpty(details))
            {
                Label lblDetails = new Label();
                lblDetails.Text = details;
                lblDetails.Font = new Font("Consolas", 8.5f, FontStyle.Regular);
                lblDetails.ForeColor = Color.FromArgb(160, 160, 165);
                lblDetails.Location = new Point(16, 96);
                lblDetails.Size = new Size(cardWidth - 32, 44);
                inner.Controls.Add(lblDetails);
                lblDetails.Click += delegate { this.Close(); };
            }

            inner.Click += delegate { this.Close(); };
            lblHeader.Click += delegate { this.Close(); };
            lblTitle.Click += delegate { this.Close(); };
            lblMsg.Click += delegate { this.Close(); };

            if (config != null && config.AutoDismiss && config.DisplayDurationSeconds > 0)
            {
                autoCloseTimer = new System.Windows.Forms.Timer();
                autoCloseTimer.Interval = config.DisplayDurationSeconds * 1000;
                autoCloseTimer.Tick += delegate
                {
                    autoCloseTimer.Stop();
                    this.Close();
                };
                autoCloseTimer.Start();
            }
        }

        private void ForceTopMost()
        {
            try
            {
                if (this.IsHandleCreated)
                {
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
            }
            catch { }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000;
                cp.ExStyle |= 0x00000080;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (autoCloseTimer != null) { autoCloseTimer.Dispose(); autoCloseTimer = null; }
                if (topTimer != null) { topTimer.Dispose(); topTimer = null; }
                if (trayIcon != null)
                {
                    try { trayIcon.Visible = false; trayIcon.Dispose(); } catch { }
                    trayIcon = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
