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
            Console.WriteLine("  ┌────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  LetMeKnow Alert History                               │");
            Console.WriteLine("  └────────────────────────────────────────────────────────┘");
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
                    Console.WriteLine("  • " + lines[i]);
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
            Console.WriteLine("  lmk --mcp-config                Show MCP configuration for coding agents");
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
            PrintStepHeader("1/6", "Sound Alerts");
            cfg.SoundEnabled = PromptYesNo("Enable audio chime for notifications?", cfg.SoundEnabled);
            Console.WriteLine();

            // 2. Urgent sound
            PrintStepHeader("2/6", "Urgent Alert Sound");
            cfg.LouderAlertEnabled = PromptYesNo("Play louder alert chime on errors and blockers?", cfg.LouderAlertEnabled);
            Console.WriteLine();

            // 3. Floating HUD Card
            PrintStepHeader("3/6", "On-Screen Floating Card");
            cfg.FloatingCardEnabled = PromptYesNo("Show floating HUD card on your screen?", cfg.FloatingCardEnabled);
            if (cfg.FloatingCardEnabled)
            {
                cfg.FollowMouseCursor = PromptYesNo("Display card on the active monitor where your mouse is?", cfg.FollowMouseCursor);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("  How long card stays visible on screen:");
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
            }
            Console.WriteLine();

            // 4. Action Center
            PrintStepHeader("4/6", "Windows Action Center");
            cfg.WindowsActionCenter = PromptYesNo("Also log alerts to Windows Notification Center (Win + N)?", cfg.WindowsActionCenter);
            Console.WriteLine();

            // 5. Tray Fallback
            PrintStepHeader("5/6", "System Tray Fallback");
            cfg.TrayNotificationFallback = PromptYesNo("Enable system tray balloon alerts (fallback if Action Center is blocked)?", cfg.TrayNotificationFallback);
            Console.WriteLine();

            // 6. MCP Server
            PrintStepHeader("6/6", "Model Context Protocol (MCP)");
            cfg.McpEnabled = PromptYesNo("Enable MCP Server for AI tools (Claude Desktop, Cursor, Antigravity)?", cfg.McpEnabled);
            if (cfg.McpEnabled)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Tip: Run 'lmk --mcp-config' to view your ready-to-copy Claude/Cursor config.");
                Console.ResetColor();
            }
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
            PrintStepHeader("✦", "AI Coding Agent Integration");
            Console.WriteLine("  Directly configure coding assistants in this folder:");
            Console.WriteLine("    [1] All common agents (Claude Code, Cursor, Codex/Copilot, Windsurf)");
            Console.WriteLine("    [2] Claude Code (CLAUDE.md + global MCP)");
            Console.WriteLine("    [3] Cursor (.cursorrules + .cursor/mcp.json)");
            Console.WriteLine("    [4] Codex / GitHub Copilot (.github/copilot-instructions.md)");
            Console.WriteLine("    [5] Windsurf (.windsurfrules)");
            Console.WriteLine("    [6] Copy Universal Agent Prompt to clipboard (for any AI agent)");
            Console.WriteLine("    [7] Skip agent configuration");
            Console.WriteLine();
            int agentChoice = PromptChoice("Select an option [1-7] (default 1): ", 1, 1, 7);
            if (agentChoice == 1)
            {
                AgentInstaller.InstallAll(Directory.GetCurrentDirectory());
            }
            else if (agentChoice == 2)
            {
                AgentInstaller.InstallClaudeCode(Directory.GetCurrentDirectory());
            }
            else if (agentChoice == 3)
            {
                AgentInstaller.InstallCursor(Directory.GetCurrentDirectory());
            }
            else if (agentChoice == 4)
            {
                AgentInstaller.InstallCodex(Directory.GetCurrentDirectory());
            }
            else if (agentChoice == 5)
            {
                AgentInstaller.InstallWindsurf(Directory.GetCurrentDirectory());
            }
            else if (agentChoice == 6)
            {
                AgentInstaller.PrintUniversalPrompt();
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
                    string trayTitle = (isUrgent ? "🚨 " : "🤖 ") + who;
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
                RegisterAppUserModelId(targetLetMeKnowExe);
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

                RemoveAppUserModelId();
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

        private static void RegisterAppUserModelId(string exePath)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppUserModelId\LetMeKnow"))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "LetMeKnow", Microsoft.Win32.RegistryValueKind.String);
                        key.SetValue("ShowInSettings", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        key.SetValue("IconUri", exePath, Microsoft.Win32.RegistryValueKind.String);
                    }
                }
            }
            catch { }
        }

        private static void RemoveAppUserModelId()
        {
            try
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\AppUserModelId\LetMeKnow", false);
            }
            catch { }
        }

        private static void AddToUserPath(string dir)
        {
            string currentPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? "";
            string[] parts = currentPath.Split(';');
            List<string> cleanParts = new List<string>();
            foreach (string p in parts)
            {
                if (!string.IsNullOrEmpty(p.Trim()) && !string.Equals(p.Trim(), dir.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    cleanParts.Add(p.Trim());
                }
            }
            string newPath = dir + (cleanParts.Count > 0 ? ";" + string.Join(";", cleanParts.ToArray()) : "");
            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.User);
            Console.WriteLine("Added to User PATH (high priority).");
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
    lmk <agent-name> <message> [<details>]
    lmk --urgent <agent-name> <message>
#>
function lmk {{
    [CmdletBinding()]
    [Alias(""letmeknow"")]
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Args
    )

    $exe = '{0}'
    if ($Args) {{
        & $exe @Args
    }} else {{
        & $exe
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

    public static class McpServer
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static void Run()
        {
            Program.IsMcpMode = true;
            try
            {
                Console.OutputEncoding = new UTF8Encoding(false);
                Console.InputEncoding = new UTF8Encoding(false);
            }
            catch { }

            // Write diagnostics to stderr so stdio JSON-RPC remains clean
            Console.Error.WriteLine("[lmk-mcp] Server started (stdio JSON-RPC 2.0).");

            string line;
            while ((line = Console.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                try
                {
                    Dictionary<string, object> req = Serializer.Deserialize<Dictionary<string, object>>(trimmed);
                    if (req == null) continue;

                    object idObj = req.ContainsKey("id") ? req["id"] : null;
                    string method = req.ContainsKey("method") ? (req["method"] as string ?? "") : "";
                    Dictionary<string, object> parameters = req.ContainsKey("params") && req["params"] is Dictionary<string, object>
                        ? (Dictionary<string, object>)req["params"]
                        : new Dictionary<string, object>();

                    HandleMessage(idObj, method, parameters);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[lmk-mcp] Error processing request: " + ex.Message);
                    SendError(null, -32700, "Parse error: " + ex.Message);
                }
            }
        }

        private static void HandleMessage(object id, string method, Dictionary<string, object> parameters)
        {
            if (method == "initialize")
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                result["protocolVersion"] = "2024-11-05";

                Dictionary<string, object> capabilities = new Dictionary<string, object>();
                capabilities["tools"] = new Dictionary<string, object>();
                result["capabilities"] = capabilities;

                Dictionary<string, object> serverInfo = new Dictionary<string, object>();
                serverInfo["name"] = "letmeknow";
                serverInfo["version"] = "1.0.0";
                result["serverInfo"] = serverInfo;

                SendResult(id, result);
                return;
            }

            if (method == "notifications/initialized")
            {
                return;
            }

            if (method == "ping")
            {
                SendResult(id, new Dictionary<string, object>());
                return;
            }

            if (method == "tools/list")
            {
                List<Dictionary<string, object>> tools = new List<Dictionary<string, object>>();

                // Tool 1: notify_human
                Dictionary<string, object> tNotify = new Dictionary<string, object>();
                tNotify["name"] = "notify_human";
                tNotify["description"] = "Send a desktop notification to alert the human user that you need assistance, clarification, credentials, or review. Supports normal or urgent priority.";

                Dictionary<string, object> schemaNotify = new Dictionary<string, object>();
                schemaNotify["type"] = "object";
                Dictionary<string, object> propsNotify = new Dictionary<string, object>();

                Dictionary<string, object> pAgent = new Dictionary<string, object>();
                pAgent["type"] = "string";
                pAgent["description"] = "Your agent name (e.g. Claude, Cursor, Antigravity)";
                propsNotify["agent_name"] = pAgent;

                Dictionary<string, object> pMsg = new Dictionary<string, object>();
                pMsg["type"] = "string";
                pMsg["description"] = "Short summary of what you need from the human (e.g. 'Need Stripe secret key in .env to run tests')";
                propsNotify["message"] = pMsg;

                Dictionary<string, object> pDetails = new Dictionary<string, object>();
                pDetails["type"] = "string";
                pDetails["description"] = "Optional extra context, file paths, or line numbers";
                propsNotify["details"] = pDetails;

                Dictionary<string, object> pUrgent = new Dictionary<string, object>();
                pUrgent["type"] = "boolean";
                pUrgent["description"] = "True if critical blocker, error, or immediate attention needed";
                propsNotify["urgent"] = pUrgent;

                schemaNotify["properties"] = propsNotify;
                schemaNotify["required"] = new string[] { "agent_name", "message" };
                tNotify["inputSchema"] = schemaNotify;
                tools.Add(tNotify);

                // Tool 2: ask_human
                Dictionary<string, object> tAsk = new Dictionary<string, object>();
                tAsk["name"] = "ask_human";
                tAsk["description"] = "Request human intervention, input, or clarification. Desktop notification will pop up on user's screen.";

                Dictionary<string, object> schemaAsk = new Dictionary<string, object>();
                schemaAsk["type"] = "object";
                Dictionary<string, object> propsAsk = new Dictionary<string, object>();

                Dictionary<string, object> pQuestion = new Dictionary<string, object>();
                pQuestion["type"] = "string";
                pQuestion["description"] = "What you are asking or need the human to do";
                propsAsk["question"] = pQuestion;

                Dictionary<string, object> pAskDetails = new Dictionary<string, object>();
                pAskDetails["type"] = "string";
                pAskDetails["description"] = "Optional context or suggestions";
                propsAsk["details"] = pAskDetails;

                Dictionary<string, object> pAskUrgent = new Dictionary<string, object>();
                pAskUrgent["type"] = "boolean";
                pAskUrgent["description"] = "True if this is a blocking urgent request";
                propsAsk["urgent"] = pAskUrgent;

                schemaAsk["properties"] = propsAsk;
                schemaAsk["required"] = new string[] { "question" };
                tAsk["inputSchema"] = schemaAsk;
                tools.Add(tAsk);

                Dictionary<string, object> result = new Dictionary<string, object>();
                result["tools"] = tools;
                SendResult(id, result);
                return;
            }

            if (method == "tools/call")
            {
                string toolName = parameters.ContainsKey("name") ? (parameters["name"] as string ?? "") : "";
                Dictionary<string, object> args = parameters.ContainsKey("arguments") && parameters["arguments"] is Dictionary<string, object>
                    ? (Dictionary<string, object>)parameters["arguments"]
                    : new Dictionary<string, object>();

                Config cfg = Config.Load();
                if (!cfg.McpEnabled)
                {
                    List<Dictionary<string, object>> errContent = new List<Dictionary<string, object>>();
                    Dictionary<string, object> textItem = new Dictionary<string, object>();
                    textItem["type"] = "text";
                    textItem["text"] = "Notice: LetMeKnow MCP server is currently disabled in config.ini. Run 'lmk --mcp-toggle' or 'lmk --config' to enable desktop alerts.";
                    errContent.Add(textItem);

                    Dictionary<string, object> disabledResult = new Dictionary<string, object>();
                    disabledResult["content"] = errContent;
                    disabledResult["isError"] = true;
                    SendResult(id, disabledResult);
                    return;
                }

                if (toolName == "notify_human")
                {
                    string agentName = "AI Agent";
                    if (args.ContainsKey("agent_name") && args["agent_name"] != null) agentName = args["agent_name"].ToString();
                    else if (args.ContainsKey("who") && args["who"] != null) agentName = args["who"].ToString();

                    string message = "";
                    if (args.ContainsKey("message") && args["message"] != null) message = args["message"].ToString();
                    else if (args.ContainsKey("summary") && args["summary"] != null) message = args["summary"].ToString();
                    else if (args.ContainsKey("what") && args["what"] != null) message = args["what"].ToString();
                    else if (args.ContainsKey("question") && args["question"] != null) message = args["question"].ToString();

                    string details = "";
                    if (args.ContainsKey("details") && args["details"] != null) details = args["details"].ToString();

                    bool isUrgent = false;
                    if (args.ContainsKey("urgent") && args["urgent"] != null)
                    {
                        if (args["urgent"] is bool) isUrgent = (bool)args["urgent"];
                        else bool.TryParse(args["urgent"].ToString(), out isUrgent);
                    }
                    else if (args.ContainsKey("urgency") && args["urgency"] != null)
                    {
                        string u = args["urgency"].ToString().ToLower();
                        isUrgent = (u == "high" || u == "critical" || u == "urgent");
                    }

                    Program.ShowNotification(agentName, message, details, isUrgent);

                    List<Dictionary<string, object>> content = new List<Dictionary<string, object>>();
                    Dictionary<string, object> textItem = new Dictionary<string, object>();
                    textItem["type"] = "text";
                    textItem["text"] = string.Format("Desktop notification delivered to human: [{0}] {1}", agentName, message);
                    content.Add(textItem);

                    Dictionary<string, object> res = new Dictionary<string, object>();
                    res["content"] = content;
                    SendResult(id, res);
                    return;
                }
                else if (toolName == "ask_human")
                {
                    string question = "";
                    if (args.ContainsKey("question") && args["question"] != null) question = args["question"].ToString();
                    else if (args.ContainsKey("message") && args["message"] != null) question = args["message"].ToString();
                    else if (args.ContainsKey("summary") && args["summary"] != null) question = args["summary"].ToString();
                    else if (args.ContainsKey("what") && args["what"] != null) question = args["what"].ToString();

                    string details = "";
                    if (args.ContainsKey("details") && args["details"] != null) details = args["details"].ToString();

                    bool isUrgent = false;
                    if (args.ContainsKey("urgent") && args["urgent"] != null)
                    {
                        if (args["urgent"] is bool) isUrgent = (bool)args["urgent"];
                        else bool.TryParse(args["urgent"].ToString(), out isUrgent);
                    }
                    else if (args.ContainsKey("urgency") && args["urgency"] != null)
                    {
                        string u = args["urgency"].ToString().ToLower();
                        isUrgent = (u == "high" || u == "critical" || u == "urgent");
                    }

                    string agentName = "AI Agent";
                    if (args.ContainsKey("agent_name") && args["agent_name"] != null) agentName = args["agent_name"].ToString();
                    else if (args.ContainsKey("who") && args["who"] != null) agentName = args["who"].ToString();

                    Program.ShowNotification(agentName, question, details, isUrgent);

                    List<Dictionary<string, object>> content = new List<Dictionary<string, object>>();
                    Dictionary<string, object> textItem = new Dictionary<string, object>();
                    textItem["type"] = "text";
                    textItem["text"] = string.Format("Question displayed on desktop: [{0}] {1}", agentName, question);
                    content.Add(textItem);

                    Dictionary<string, object> res = new Dictionary<string, object>();
                    res["content"] = content;
                    SendResult(id, res);
                    return;
                }
                else
                {
                    SendError(id, -32602, "Unknown tool: " + toolName);
                    return;
                }
            }

            if (id != null)
            {
                SendError(id, -32601, "Method not found: " + method);
            }
        }

        private static void SendResult(object id, object result)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            res["jsonrpc"] = "2.0";
            res["id"] = id;
            res["result"] = result;
            string json = Serializer.Serialize(res);
            Console.WriteLine(json);
            Console.Out.Flush();
        }

        private static void SendError(object id, int code, string message)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            res["jsonrpc"] = "2.0";
            res["id"] = id;

            Dictionary<string, object> err = new Dictionary<string, object>();
            err["code"] = code;
            err["message"] = message;
            res["error"] = err;

            string json = Serializer.Serialize(res);
            Console.WriteLine(json);
            Console.Out.Flush();
        }

        public static void PrintMcpConfig()
        {
            Config cfg = Config.Load();
            string exePath = Assembly.GetExecutingAssembly().Location;
            string jsonPath = exePath.Replace("\\", "\\\\");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ┌────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  LetMeKnow MCP Configuration                           │");
            Console.WriteLine("  └────────────────────────────────────────────────────────┘");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("  MCP Server Status: ");
            if (cfg.McpEnabled)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ENABLED");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("DISABLED (Run 'lmk --mcp-toggle' to enable)");
            }
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  1. Claude Code:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Run terminal command:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    claude mcp add letmeknow \"" + exePath + "\" -- --mcp");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Or auto-configure right now:  lmk --install-claude");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  2. Cursor (.cursor/mcp.json):");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  {");
            Console.WriteLine("    \"mcpServers\": {");
            Console.WriteLine("      \"letmeknow\": {");
            Console.WriteLine("        \"command\": \"" + jsonPath + "\",");
            Console.WriteLine("        \"args\": [\"--mcp\"]");
            Console.WriteLine("      }");
            Console.WriteLine("    }");
            Console.WriteLine("  }");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.WriteLine("  Or auto-configure right now:  lmk --install-cursor");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  3. Codex / GitHub Copilot / Generic MCP:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  \"letmeknow\": { \"command\": \"" + jsonPath + "\", \"args\": [\"--mcp\"] }");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.WriteLine("  Or auto-configure right now:  lmk --install-codex");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  4. Windsurf (~/.codeium/windsurf/mcp_config.json):");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  \"mcpServers\": {");
            Console.WriteLine("    \"letmeknow\": {");
            Console.WriteLine("      \"command\": \"" + jsonPath + "\",");
            Console.WriteLine("      \"args\": [\"--mcp\"]");
            Console.WriteLine("    }");
            Console.WriteLine("  }");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.WriteLine("  Or auto-configure right now:  lmk --install-windsurf");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  5. Claude Desktop (%APPDATA%\\Claude\\claude_desktop_config.json):");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  \"mcpServers\": {");
            Console.WriteLine("    \"letmeknow\": {");
            Console.WriteLine("      \"command\": \"" + jsonPath + "\",");
            Console.WriteLine("      \"args\": [\"--mcp\"]");
            Console.WriteLine("    }");
            Console.WriteLine("  }");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ----------------------------------------------------------------------------");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  Commands:");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    lmk --install-agents all    Directly configure all agents in this folder");
            Console.WriteLine("    lmk --prompt                Display & copy universal AI agent prompt");
            Console.WriteLine("    lmk --mcp                   Start the stdio MCP server directly");
            Console.WriteLine("    lmk --mcp-toggle            Toggle MCP enabled / disabled");
            Console.WriteLine("    lmk --config                Run interactive wizard to update preferences");
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    public static class AgentInstaller
    {
        public static string GetUniversalPrompt()
        {
            return @"I have installed LetMeKnow (`lmk`), a desktop notification utility and MCP server on this machine.

Your task right now is to configure yourself to use `lmk` whenever you need human input or get stuck.

Follow these steps immediately:
1. Identify what coding assistant and editor environment you are running as (e.g. Claude Code, Cursor, Codex / GitHub Copilot, Windsurf, Antigravity, Aider, Devin, or custom agent).
2. Locate or create your project/workspace instruction or rules file:
   - For Claude Code: `CLAUDE.md` in workspace root or `~/.claude/CLAUDE.md`
   - For Cursor: `.cursorrules` or `.cursor/rules/letmeknow.mdc` and `.cursor/mcp.json`
   - For Codex / GitHub Copilot: `.github/copilot-instructions.md`
   - For Windsurf: `.windsurfrules`
   - For any other agent: your primary instructions/rules/memory configuration file.
3. Add the following rule to your configuration:
   ```markdown
   ### Human Desktop Alerts (LetMeKnow)
   Whenever you need user input, API credentials, approval for destructive actions, or human review:
   - Run: lmk ""<YourAgentName>"" ""<What you need>""
   - Urgent / Blocker: lmk --urgent ""<YourAgentName>"" ""<Blocker summary>""

   If you have MCP tool access:
   - Call tool `notify_human` or `ask_human` with your name and message.
   ```
4. Verify your setup right now by executing this command in your terminal:
   lmk ""<YourAgentName>"" ""Desktop alerts configured successfully""
5. Confirm to me once saved and verified.";
        }

        public static void PrintUniversalPrompt()
        {
            string prompt = GetUniversalPrompt();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ┌────────────────────────────────────────────────────────┐");
            Console.WriteLine("  │  Universal AI Agent Setup Prompt                       │");
            Console.WriteLine("  └────────────────────────────────────────────────────────┘");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Copy and paste this prompt into ANY AI coding assistant:");
            Console.WriteLine("  --------------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(prompt);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  --------------------------------------------------------");
            Console.ResetColor();

            try
            {
                Clipboard.SetText(prompt);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✔ Copied to clipboard! Just paste it into your AI agent.");
                Console.ResetColor();
            }
            catch { }
            Console.WriteLine();
        }

        public static void InstallClaudeCode(string targetDir)
        {
            try
            {
                string claudeMd = Path.Combine(targetDir, "CLAUDE.md");
                string section = @"

### Human Desktop Alerts (LetMeKnow)
When you need human input, credentials, clarification, or human review:
- Run: lmk ""Claude"" ""<What you need>""
- Urgent: lmk --urgent ""Claude"" ""<Error summary or blocker>""
- Example: lmk ""Claude"" ""Need STRIPE_SECRET_KEY in .env to run tests""
";
                InjectFile(claudeMd, section, "CLAUDE.md (Claude Code)");

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string claudeJson = Path.Combine(userProfile, ".claude.json");
                string exePath = Assembly.GetExecutingAssembly().Location;
                TryInjectMcpConfig(claudeJson, exePath, "Claude Code (~/.claude.json)");

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Tip: You can also run: claude mcp add letmeknow \"{0}\" -- --mcp", exePath);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error configuring Claude Code: " + ex.Message);
            }
        }

        public static void InstallCursor(string targetDir)
        {
            try
            {
                string cursorRules = Path.Combine(targetDir, ".cursorrules");
                string section = @"

### Human Desktop Alerts (LetMeKnow)
When you need human input, credentials, clarification, or human review:
- Run: lmk ""Cursor"" ""<What you need>""
- Urgent: lmk --urgent ""Cursor"" ""<Error summary or blocker>""
- Example: lmk ""Cursor"" ""Build failed on line 42 in auth.py""
";
                InjectFile(cursorRules, section, ".cursorrules (Cursor)");

                // Modern Cursor MDC rules file: .cursor/rules/letmeknow.mdc
                string rulesDir = Path.Combine(targetDir, ".cursor", "rules");
                Directory.CreateDirectory(rulesDir);
                string mdcFile = Path.Combine(rulesDir, "letmeknow.mdc");
                string mdcContent = @"---
description: Human desktop notification instructions for LetMeKnow
globs: *
---
# Human Desktop Alerts
When you need user input, clarification, credentials, or human review:
- Run: lmk ""Cursor"" ""<What you need>""
- Urgent: lmk --urgent ""Cursor"" ""<Issue summary>""
";
                File.WriteAllText(mdcFile, mdcContent, Encoding.UTF8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✔ Created .cursor/rules/letmeknow.mdc");
                Console.ResetColor();

                string mcpJson = Path.Combine(targetDir, ".cursor", "mcp.json");
                string exePath = Assembly.GetExecutingAssembly().Location;
                TryInjectMcpConfig(mcpJson, exePath, "Cursor (.cursor/mcp.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error configuring Cursor: " + ex.Message);
            }
        }

        public static void InstallCodex(string targetDir)
        {
            try
            {
                string ghDir = Path.Combine(targetDir, ".github");
                Directory.CreateDirectory(ghDir);
                string copilotFile = Path.Combine(ghDir, "copilot-instructions.md");
                string section = @"

### Human Desktop Alerts (LetMeKnow)
When you need human input, API credentials, approval for destructive actions, or human review:
- Run: lmk ""Codex"" ""<What you need>""
- Urgent: lmk --urgent ""Codex"" ""<Error summary or blocker>""
- Example: lmk ""Codex"" ""Need database migration approval before proceeding""
";
                InjectFile(copilotFile, section, ".github/copilot-instructions.md (Codex / Copilot)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error configuring Codex / Copilot: " + ex.Message);
            }
        }

        public static void InstallWindsurf(string targetDir)
        {
            try
            {
                string windsurfRules = Path.Combine(targetDir, ".windsurfrules");
                string section = @"

### Human Desktop Alerts (LetMeKnow)
When you need user input, credentials, clarification, or human review:
- Run: lmk ""Windsurf"" ""<What you need>""
- Urgent: lmk --urgent ""Windsurf"" ""<Issue summary>""
";
                InjectFile(windsurfRules, section, ".windsurfrules (Windsurf)");

                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string windsurfMcp = Path.Combine(userProfile, ".codeium", "windsurf", "mcp_config.json");
                string exePath = Assembly.GetExecutingAssembly().Location;
                TryInjectMcpConfig(windsurfMcp, exePath, "Windsurf (mcp_config.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error configuring Windsurf: " + ex.Message);
            }
        }

        public static void InstallAll(string targetDir)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  Configuring LetMeKnow for common AI coding agents...");
            Console.ResetColor();
            InstallClaudeCode(targetDir);
            InstallCursor(targetDir);
            InstallCodex(targetDir);
            InstallWindsurf(targetDir);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔ All common agent configurations complete.");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void InjectFile(string filePath, string content, string displayName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            if (File.Exists(filePath))
            {
                string existing = File.ReadAllText(filePath);
                if (existing.Contains("lmk") || existing.Contains("LetMeKnow"))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("  • {0} already contains LetMeKnow instructions.", displayName);
                    Console.ResetColor();
                    return;
                }
                File.AppendAllText(filePath, content, Encoding.UTF8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✔ Updated {0}", displayName);
                Console.ResetColor();
            }
            else
            {
                File.WriteAllText(filePath, content.TrimStart(), Encoding.UTF8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✔ Created {0}", displayName);
                Console.ResetColor();
            }
        }

        private static void TryInjectMcpConfig(string jsonPath, string exePath, string displayName)
        {
            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> root = null;
                if (File.Exists(jsonPath))
                {
                    try
                    {
                        string txt = File.ReadAllText(jsonPath);
                        root = js.Deserialize<Dictionary<string, object>>(txt);
                    }
                    catch { }
                }

                if (root == null)
                {
                    root = new Dictionary<string, object>();
                }

                Dictionary<string, object> mcpServers = null;
                if (root.ContainsKey("mcpServers") && root["mcpServers"] is Dictionary<string, object>)
                {
                    mcpServers = (Dictionary<string, object>)root["mcpServers"];
                }
                else
                {
                    mcpServers = new Dictionary<string, object>();
                    root["mcpServers"] = mcpServers;
                }

                Dictionary<string, object> lmkServer = new Dictionary<string, object>();
                lmkServer["command"] = exePath;
                lmkServer["args"] = new string[] { "--mcp" };
                mcpServers["letmeknow"] = lmkServer;

                Directory.CreateDirectory(Path.GetDirectoryName(jsonPath));
                File.WriteAllText(jsonPath, js.Serialize(root), Encoding.UTF8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✔ Configured MCP for {0}", displayName);
                Console.ResetColor();
            }
            catch { }
        }
    }
}
