using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace LetMeKnow
{
    public static class Wizard
    {
        public static void RunSetupWizard()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            try { Console.Title = "LetMeKnow Setup"; } catch { }
            try { Console.Clear(); } catch { }

            PrintWelcomeHeader();

            Config cfg = Config.Load();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  Select an option:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    [1] Quick Setup (Recommended - 1 Click)  <-- Press Enter");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("        * Installs lmk globally to User PATH & PowerShell");
                Console.WriteLine("        * Enables chime & floating card on your active monitor");
                Console.WriteLine("        * Auto-configures AI coding agents (Claude, Cursor, Codex, Windsurf)");
                Console.WriteLine("        * Copies universal setup prompt to your clipboard");
                Console.WriteLine("        * Delivers a test desktop alert");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("    [2] Configure AI Coding Agents");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("        * Auto-configure specific agents or view universal prompt");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("    [3] Customize Notification Settings");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("        * Fine-tune audio, display duration, and monitor behavior");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("    [4] Model Context Protocol (MCP) Setup");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("        * View & export config snippets for Cursor, Claude Code, etc.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("    [5] Send Test Notification");
                Console.WriteLine("    [0] Exit");
                Console.ResetColor();
                Console.WriteLine();

                int choice = PromptChoice("Choose option [0-5] (default 1): ", 1, 0, 5);

                if (choice == 1)
                {
                    RunQuickSetup(cfg);
                    break;
                }
                else if (choice == 2)
                {
                    RunAgentMenu();
                }
                else if (choice == 3)
                {
                    RunCustomSettings(cfg);
                }
                else if (choice == 4)
                {
                    McpServer.PrintMcpConfig();
                }
                else if (choice == 5)
                {
                    Program.ShowNotification("LetMeKnow", "Test notification delivered.", "Configuration test successful.", false);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [OK] Test notification sent to your desktop.");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                else if (choice == 0)
                {
                    Console.WriteLine("  Goodbye!");
                    return;
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Press any key to return to menu...");
                Console.ResetColor();
                try { Console.ReadKey(); } catch { }
                try { Console.Clear(); } catch { }
                PrintWelcomeHeader();
            }
        }

        private static void PrintWelcomeHeader()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.WriteLine("  |  LetMeKnow (lmk) Setup                                 |");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  |  Desktop notification bridge for AI coding agents      |");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  LetMeKnow alerts you on your desktop whenever your AI coding");
            Console.WriteLine("  assistants need help, approval, credentials, or finish tasks.");
            Console.WriteLine();
        }

        private static void RunQuickSetup(Config cfg)
        {
            Console.WriteLine();
            PrintStepHeader("1/4", "Applying Recommended Preferences");
            cfg.SoundEnabled = true;
            cfg.LouderAlertEnabled = true;
            cfg.DisplayDurationSeconds = 8;
            cfg.FloatingCardEnabled = true;
            cfg.FollowMouseCursor = true;
            cfg.WindowsActionCenter = true;
            cfg.TrayNotificationFallback = true;
            cfg.McpEnabled = true;
            cfg.Save();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [OK] Optimized notification settings saved.");
            Console.ResetColor();
            Console.WriteLine();

            PrintStepHeader("2/4", "Global Installation");
            Installer.Install(cfg);
            Console.WriteLine();

            PrintStepHeader("3/4", "Configuring AI Coding Agents");
            AgentInstaller.InstallAll(Directory.GetCurrentDirectory());
            AgentInstaller.CopyUniversalPromptToClipboard();
            Console.WriteLine();

            PrintStepHeader("4/4", "Verification");
            Program.ShowNotification("LetMeKnow", "Setup complete! LetMeKnow is ready.", "We copied the AI agent setup prompt to your clipboard.", false);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [OK] Test notification sent to your screen.");
            Console.ResetColor();
            Console.WriteLine();

            PrintNextStepsGuide();
        }

        private static void PrintNextStepsGuide()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.WriteLine("  |  All Done! How to use LetMeKnow:                       |");
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  1. Give your AI coding agent the instructions:");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("     * We just copied the universal prompt to your clipboard!");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("     * Simply switch to Cursor, Claude, Codex, etc., press Ctrl+V, and hit Enter.");
            Console.WriteLine();
            Console.WriteLine("  2. Command syntax for terminals and scripts:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("     lmk \"<AgentName>\" \"<What you need>\"");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("     Example: lmk \"Claude\" \"Need STRIPE_SECRET_KEY in .env\"");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("     lmk --urgent \"<AgentName>\" \"<Critical error>\"");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("     Example: lmk --urgent \"Cursor\" \"Build failed on line 42\"");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  3. Helpful commands:");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("     * lmk --prompt        Display & re-copy universal prompt anytime");
            Console.WriteLine("     * lmk --history       View recent notification history");
            Console.WriteLine("     * lmk --mcp-config    View MCP tool settings for your editor");
            Console.WriteLine("     * lmk --config        Return to this setup menu anytime");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Press any key to finish...");
            Console.ResetColor();
            try { Console.ReadKey(); } catch { }
        }

        private static void RunAgentMenu()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  Configure AI Coding Agents");
            Console.WriteLine("  --------------------------------------------------------");
            Console.ResetColor();
            Console.WriteLine("    [1] Auto-configure all common agents in this project");
            Console.WriteLine("    [2] Claude Code (CLAUDE.md & ~/.claude.json MCP)");
            Console.WriteLine("    [3] Cursor (.cursorrules & .cursor/mcp.json)");
            Console.WriteLine("    [4] Codex / GitHub Copilot (.github/copilot-instructions.md)");
            Console.WriteLine("    [5] Windsurf (.windsurfrules & MCP)");
            Console.WriteLine("    [6] Show & Copy Universal Prompt to Clipboard");
            Console.WriteLine("    [0] Back to main menu");
            Console.WriteLine();

            int choice = PromptChoice("Select [0-6] (default 1): ", 1, 0, 6);
            string curDir = Directory.GetCurrentDirectory();

            if (choice == 1) AgentInstaller.InstallAll(curDir);
            else if (choice == 2) AgentInstaller.InstallClaudeCode(curDir);
            else if (choice == 3) AgentInstaller.InstallCursor(curDir);
            else if (choice == 4) AgentInstaller.InstallCodex(curDir);
            else if (choice == 5) AgentInstaller.InstallWindsurf(curDir);
            else if (choice == 6) AgentInstaller.PrintUniversalPrompt();
        }

        private static void RunCustomSettings(Config cfg)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  Customize Notification Settings");
            Console.WriteLine("  --------------------------------------------------------");
            Console.ResetColor();

            PrintStepHeader("1/4", "Audio Alerts");
            cfg.SoundEnabled = PromptYesNo("Play audio chime on notifications?", cfg.SoundEnabled);
            if (cfg.SoundEnabled)
            {
                cfg.LouderAlertEnabled = PromptYesNo("Play louder alert chime on urgent alerts?", cfg.LouderAlertEnabled);
            }
            Console.WriteLine();

            PrintStepHeader("2/4", "On-Screen Floating Card");
            cfg.FloatingCardEnabled = PromptYesNo("Show dark HUD notification card in bottom corner?", cfg.FloatingCardEnabled);
            if (cfg.FloatingCardEnabled)
            {
                cfg.FollowMouseCursor = PromptYesNo("Follow active cursor screen (shows card on active monitor)?", cfg.FollowMouseCursor);
                Console.WriteLine("  Card display duration:");
                Console.WriteLine("    1. Quick (5 seconds)");
                Console.WriteLine("    2. Standard (8 seconds)");
                Console.WriteLine("    3. Extended (15 seconds)");
                Console.WriteLine("    4. Keep until dismissed");
                int durChoice = PromptChoice("Select [1-4] (default 2): ", 2, 1, 4);
                if (durChoice == 1) { cfg.DisplayDurationSeconds = 5; cfg.AutoDismiss = true; }
                else if (durChoice == 2) { cfg.DisplayDurationSeconds = 8; cfg.AutoDismiss = true; }
                else if (durChoice == 3) { cfg.DisplayDurationSeconds = 15; cfg.AutoDismiss = true; }
                else if (durChoice == 4) { cfg.DisplayDurationSeconds = 300; cfg.AutoDismiss = false; }
            }
            Console.WriteLine();

            PrintStepHeader("3/4", "Windows Notification Center & Tray");
            cfg.WindowsActionCenter = PromptYesNo("Also log alerts to Windows Notification Center (Win + N)?", cfg.WindowsActionCenter);
            cfg.TrayNotificationFallback = PromptYesNo("Show system tray balloon fallback?", cfg.TrayNotificationFallback);
            Console.WriteLine();

            PrintStepHeader("4/4", "Model Context Protocol (MCP)");
            cfg.McpEnabled = PromptYesNo("Enable MCP Server for AI tools (Claude, Cursor, Codex)?", cfg.McpEnabled);
            Console.WriteLine();

            cfg.Save();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [OK] Custom preferences saved to config.ini");
            Console.ResetColor();
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
            Console.Write("  > " + prompt + " ");
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
            Console.Write("  > " + prompt);
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

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [OK] LetMeKnow registered globally in PATH & PowerShell.");
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
            foreach (string p in parts)
            {
                if (string.Equals(p.Trim(), dir.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            string newPath = dir.Trim() + ";" + currentPath;
            Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.User);
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
}
