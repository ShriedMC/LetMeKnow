using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("LetMeKnow")]
[assembly: AssemblyDescription("Desktop Notification Utility")]
[assembly: AssemblyCompany("LetMeKnow")]
[assembly: AssemblyProduct("LetMeKnow")]
[assembly: AssemblyCopyright("Copyright © 2026")]
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
        [STAThread]
        static void Main(string[] args)
        {
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

            if (first == "--test" || first == "test")
            {
                ShowNotification("Test", "Desktop notifications are working.", "Sent from lmk test", false);
                return;
            }

            // Internal background process: --render <who> <what> <details> <isUrgent>
            if (first == "--render" && args.Length >= 3)
            {
                string rWho = args[1];
                string rWhat = args[2];
                string rDetails = args.Length >= 4 ? args[3] : "";
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
            string exePath = Assembly.GetExecutingAssembly().Location;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = exePath;
            psi.Arguments = string.Format("--render \"{0}\" \"{1}\" \"{2}\" {3}",
                EscapeArg(who), EscapeArg(what), EscapeArg(details), isUrgent ? "true" : "false");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                Process.Start(psi);
                Console.WriteLine("[lmk] sent: [{0}] {1}", who, what);
            }
            catch (Exception)
            {
                RenderNotificationWindow(who, what, details, isUrgent);
            }
        }

        static string EscapeArg(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "";
            return arg.Replace("\"", "\\\"");
        }

        static void RenderNotificationWindow(string who, string what, string details, bool isUrgent)
        {
            Config cfg = Config.Load();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (cfg.SoundEnabled)
            {
                try
                {
                    if (isUrgent && cfg.LouderAlertEnabled)
                    {
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            try
                            {
                                System.Media.SystemSounds.Exclamation.Play();
                                Console.Beep(880, 120);
                                Console.Beep(1175, 240);
                            }
                            catch { }
                        });
                    }
                    else
                    {
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                }
                catch { }
            }

            if (cfg.WindowsActionCenter)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        SendWindowsToast(who, what, details);
                    });
                }
                catch { }
            }

            using (ToastForm form = new ToastForm(who, what, details, cfg, isUrgent))
            {
                Application.Run(form);
            }
        }

        static void SendWindowsToast(string who, string what, string details)
        {
            try
            {
                string body = string.IsNullOrEmpty(details) ? what : what + "`n" + details;
                string psCode = string.Format(@"
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
", who.Replace("\"", "`\""), body.Replace("\"", "`\""));

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + psCode.Replace("\"", "\\\"") + "\"";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                }
            }
            catch { }
        }

        static void PrintHelp()
        {
            Console.WriteLine("lmk - desktop notifications for CLI and coding agents");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  lmk <who> <what> [<details>]    Send notification");
            Console.WriteLine("  lmk --urgent <who> <what>       Send high-priority notification");
            Console.WriteLine("  lmk --config                    Run setup wizard");
            Console.WriteLine("  lmk --test                      Send test notification");
            Console.WriteLine("  lmk --install                   Install globally (PATH & PowerShell)");
            Console.WriteLine("  lmk --uninstall                 Remove application and settings");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  lmk Claude \"Need Stripe API key in .env\"");
            Console.WriteLine("  lmk Antigravity \"Review needed on line 42\"");
            Console.WriteLine("  lmk --urgent Cursor \"Build failed: TS2307\"");
        }
    }

    public static class Wizard
    {
        public static void RunSetupWizard()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            try { Console.Title = "LetMeKnow Setup"; } catch { }
            try { Console.Clear(); } catch { }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ┌────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  LetMeKnow Setup                                       │");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  │  Desktop notification bridge for AI agents             │");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  └────────────────────────────────────────────────────────┘");
            Console.ResetColor();
            Console.WriteLine();

            Config cfg = Config.Load();

            // 1. Audio alerts
            PrintStepHeader("1/4", "Sound Alerts");
            cfg.SoundEnabled = PromptYesNo("Enable audio chime for notifications?", cfg.SoundEnabled);
            Console.WriteLine();

            // 2. Urgent sound
            PrintStepHeader("2/4", "Urgent Alert Sound");
            cfg.LouderAlertEnabled = PromptYesNo("Play louder alert chime on errors and blockers?", cfg.LouderAlertEnabled);
            Console.WriteLine();

            // 3. Duration
            PrintStepHeader("3/4", "Display Duration");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  How long alerts stay visible on screen:");
            Console.ResetColor();
            Console.WriteLine("    1 · 5 seconds");
            Console.WriteLine("    2 · 8 seconds (recommended)");
            Console.WriteLine("    3 · 15 seconds");
            Console.WriteLine("    4 · Persistent (until clicked)");
            int choice = PromptChoice("Select [1-4] (default 2): ", 2, 1, 4);
            if (choice == 1) { cfg.DisplayDurationSeconds = 5; cfg.AutoDismiss = true; }
            else if (choice == 2) { cfg.DisplayDurationSeconds = 8; cfg.AutoDismiss = true; }
            else if (choice == 3) { cfg.DisplayDurationSeconds = 15; cfg.AutoDismiss = true; }
            else if (choice == 4) { cfg.DisplayDurationSeconds = 300; cfg.AutoDismiss = false; }
            Console.WriteLine();

            // 4. Action Center
            PrintStepHeader("4/4", "Windows Action Center");
            cfg.WindowsActionCenter = PromptYesNo("Also log alerts to Windows Notification Center (Win + N)?", cfg.WindowsActionCenter);
            Console.WriteLine();

            cfg.Save();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔ Preferences saved to config.ini");
            Console.ResetColor();
            Console.WriteLine();

            // Test
            if (PromptYesNo("Send test notification now?", true))
            {
                Program.ShowNotification("LetMeKnow", "Test notification delivered.", "Configuration saved successfully.", false);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✔ Test notification sent to your desktop.");
                Console.ResetColor();
            }

            Console.WriteLine();
            PrintStepHeader("✦", "Global Installation");
            if (PromptYesNo("Install lmk globally (adds to User PATH and PowerShell)?", true))
            {
                Installer.Install(cfg);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("  Setup complete.");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Press any key to finish...");
            Console.ResetColor();
            try { Console.ReadKey(); } catch { }
        }

        private static void PrintStepHeader(string step, string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  [" + step + "] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(title);
            Console.ResetColor();
        }

        private static bool PromptYesNo(string prompt, bool defaultValue)
        {
            string hint = defaultValue ? "(Y/n)" : "(y/N)";
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("  › " + prompt + " ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(hint + " ");
            Console.ResetColor();

            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            input = input.Trim().ToLower();
            if (input == "y" || input == "yes") return true;
            if (input == "n" || input == "no") return false;
            return defaultValue;
        }

        private static int PromptChoice(string prompt, int defaultValue, int min, int max)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("  › " + prompt);
            Console.ResetColor();

            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            int val;
            if (int.TryParse(input.Trim(), out val))
            {
                if (val >= min && val <= max) return val;
            }
            return defaultValue;
        }
    }

    public class ToastForm : Form
    {
        private System.Windows.Forms.Timer autoCloseTimer;
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

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(screen.Right - cardWidth - 20, screen.Bottom - cardHeight - 20);

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
            btnClose.Text = "×";
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

            if (config.AutoDismiss && config.DisplayDurationSeconds > 0)
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
    }

    public static class Installer
    {
        public static void Install(Config cfg)
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string targetDir = Path.Combine(localAppData, "LetMeKnow");
                Directory.CreateDirectory(targetDir);

                string sourceExe = Assembly.GetExecutingAssembly().Location;
                string targetExe = Path.Combine(targetDir, "lmk.exe");
                string targetLetMeKnowExe = Path.Combine(targetDir, "LetMeKnow.exe");

                if (!sourceExe.Equals(targetExe, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(sourceExe, targetExe, true);
                    File.Copy(sourceExe, targetLetMeKnowExe, true);
                }

                cfg.Save();
                AddToUserPath(targetDir);
                CreateStartMenuShortcut(targetLetMeKnowExe);
                InstallPowerShellModule(targetExe);
                RegisterPowerShellProfile(targetExe);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ┌────────────────────────────────────────────────────────┐");
                Console.WriteLine("  │  ✔ LetMeKnow is ready to use                           │");
                Console.WriteLine("  └────────────────────────────────────────────────────────┘");
                Console.ResetColor();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("    Command:   lmk <agent-name> <message>");
                Console.WriteLine("    Location:  " + Path.Combine(targetDir, "lmk.exe"));
                Console.WriteLine("    Settings:  " + Path.Combine(targetDir, "config.ini"));
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("    Quick test:");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("    lmk Claude \"Need Stripe API key in .env\"");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Installation error: " + ex.Message);
            }
        }

        public static void Uninstall()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string targetDir = Path.Combine(localAppData, "LetMeKnow");

                RemoveFromUserPath(targetDir);
                RemoveStartMenuShortcut();
                RemovePowerShellProfile();
                RemovePowerShellModule();

                string tempBat = Path.Combine(Path.GetTempPath(), "uninstall_letmeknow.bat");
                File.WriteAllText(tempBat, string.Format(@"
@echo off
timeout /t 1 /nobreak >nul
rmdir /s /q ""{0}""
del ""%~f0""
", targetDir));

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = tempBat;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);

                Console.WriteLine("LetMeKnow uninstalled successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Uninstall error: " + ex.Message);
            }
        }

        private static void AddToUserPath(string dir)
        {
            string currentPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? "";
            string[] parts = currentPath.Split(';');
            foreach (string p in parts)
            {
                if (string.Equals(p.Trim(), dir.Trim(), StringComparison.OrdinalIgnoreCase))
                    return;
            }
            string newPath = currentPath.TrimEnd(';') + ";" + dir;
            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.User);
            Console.WriteLine("Added to User PATH.");
        }

        private static void RemoveFromUserPath(string dir)
        {
            string currentPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? "";
            string[] parts = currentPath.Split(';');
            List<string> kept = new List<string>();
            foreach (string p in parts)
            {
                if (!string.Equals(p.Trim(), dir.Trim(), StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(p))
                {
                    kept.Add(p);
                }
            }
            string newPath = string.Join(";", kept.ToArray());
            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.User);
            Console.WriteLine("Removed from User PATH.");
        }

        private static void CreateStartMenuShortcut(string exePath)
        {
            try
            {
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string lnkPath = Path.Combine(programs, "LetMeKnow.lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(lnkPath);
                    shortcut.TargetPath = exePath;
                    shortcut.Description = "LetMeKnow";
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                    shortcut.Save();
                }
            }
            catch { }
        }

        private static void RemoveStartMenuShortcut()
        {
            try
            {
                string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string lnkPath = Path.Combine(programs, "LetMeKnow.lnk");
                if (File.Exists(lnkPath))
                {
                    File.Delete(lnkPath);
                }
            }
            catch { }
        }

        private static void InstallPowerShellModule(string exePath)
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] moduleDirs = new string[] {
                    Path.Combine(userProfile, "OneDrive", "Documents", "WindowsPowerShell", "Modules", "LetMeKnow"),
                    Path.Combine(userProfile, "Documents", "WindowsPowerShell", "Modules", "LetMeKnow")
                };

                string psm1 = string.Format(@"<#
.SYNOPSIS
    Sends desktop notification via LetMeKnow.
.SYNTAX
    lmk [-Who] <string> [[-What] <string>] [[-Details] <string>]
#>
function lmk {{
    [CmdletBinding()]
    [Alias(""letmeknow"")]
    param(
        [Parameter(Position = 0, Mandatory = $true)]
        [string]$Who,

        [Parameter(Position = 1, Mandatory = $false)]
        [string]$What = """",

        [Parameter(Position = 2, Mandatory = $false)]
        [string]$Details = """"
    )

    $exe = '{0}'
    if ($Details) {{
        & $exe $Who $What $Details
    }} elseif ($What) {{
        & $exe $Who $What
    }} else {{
        & $exe $Who
    }}
}}

Export-ModuleMember -Function lmk -Alias letmeknow
", exePath);

                foreach (string d in moduleDirs)
                {
                    if (Directory.Exists(Path.GetDirectoryName(d)))
                    {
                        Directory.CreateDirectory(d);
                        File.WriteAllText(Path.Combine(d, "LetMeKnow.psm1"), psm1);
                    }
                }
            }
            catch { }
        }

        private static void RemovePowerShellModule()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] moduleDirs = new string[] {
                    Path.Combine(userProfile, "OneDrive", "Documents", "WindowsPowerShell", "Modules", "LetMeKnow"),
                    Path.Combine(userProfile, "Documents", "WindowsPowerShell", "Modules", "LetMeKnow")
                };
                foreach (string d in moduleDirs)
                {
                    if (Directory.Exists(d))
                    {
                        Directory.Delete(d, true);
                    }
                }
            }
            catch { }
        }

        private static void RegisterPowerShellProfile(string exePath)
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] possibleProfiles = new string[] {
                    Path.Combine(userProfile, "OneDrive", "Documents", "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1"),
                    Path.Combine(userProfile, "Documents", "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1")
                };

                foreach (string profilePath in possibleProfiles)
                {
                    if (File.Exists(profilePath))
                    {
                        string content = File.ReadAllText(profilePath);
                        if (!content.Contains("function lmk") && !content.Contains("LetMeKnow"))
                        {
                            string snippet = string.Format(@"

# LetMeKnow (lmk)
Import-Module LetMeKnow -ErrorAction SilentlyContinue
if (-not (Get-Command lmk -ErrorAction SilentlyContinue)) {{
    function lmk {{ & '{0}' @args }}
}}
", exePath);
                            File.AppendAllText(profilePath, snippet);
                        }
                    }
                }
            }
            catch { }
        }

        private static void RemovePowerShellProfile()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] possibleProfiles = new string[] {
                    Path.Combine(userProfile, "OneDrive", "Documents", "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1"),
                    Path.Combine(userProfile, "Documents", "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1")
                };

                foreach (string p in possibleProfiles)
                {
                    if (File.Exists(p))
                    {
                        string content = File.ReadAllText(p);
                        if (content.Contains("function lmk") || content.Contains("LetMeKnow"))
                        {
                            string[] lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                            List<string> cleanLines = new List<string>();
                            foreach (string line in lines)
                            {
                                if (line.Contains("# LetMeKnow") || line.Contains("function lmk") || line.Contains("lmk.exe"))
                                    continue;
                                cleanLines.Add(line);
                            }
                            File.WriteAllText(p, string.Join("\r\n", cleanLines.ToArray()));
                        }
                    }
                }
            }
            catch { }
        }
    }
}
