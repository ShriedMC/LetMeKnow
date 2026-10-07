using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace LetMeKnow
{
    public static class AgentInstaller
    {
        public static string GetUniversalPrompt()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
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
4. If your environment supports MCP (Model Context Protocol):
   Connect to the local LetMeKnow MCP server on this machine:
   - Command: """ + exePath + @"""
   - Args: [""--mcp""]
   (For Cursor: add to `.cursor/mcp.json`; For Claude Code CLI: `claude mcp add letmeknow """ + exePath + @""" -- --mcp`; For Windsurf: `~/.codeium/windsurf/mcp_config.json`).
5. Verify your setup right now by executing this command in your terminal:
   lmk ""<YourAgentName>"" ""Desktop alerts configured successfully""
6. Confirm to me once saved and verified.";
        }

        public static void PrintUniversalPrompt()
        {
            string prompt = GetUniversalPrompt();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.WriteLine("  |  Universal AI Agent Setup Prompt                       |");
            Console.WriteLine("  +--------------------------------------------------------+");
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

            CopyUniversalPromptToClipboard();
            Console.WriteLine();
        }

        public static void CopyUniversalPromptToClipboard()
        {
            try
            {
                Clipboard.SetText(GetUniversalPrompt());
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [OK] Copied to clipboard! Just paste it into your AI agent.");
                Console.ResetColor();
            }
            catch { }
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
                Console.WriteLine("  [+] Created .cursor/rules/letmeknow.mdc");
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
            Console.WriteLine("  [OK] All common agent configurations complete.");
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
                    Console.WriteLine("  * {0} already contains LetMeKnow instructions.", displayName);
                    Console.ResetColor();
                    return;
                }
                File.AppendAllText(filePath, content, Encoding.UTF8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [+] Updated {0}", displayName);
                Console.ResetColor();
            }
            else
            {
                File.WriteAllText(filePath, content.TrimStart(), Encoding.UTF8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [+] Created {0}", displayName);
                Console.ResetColor();
            }
        }

        internal static void TryInjectMcpConfig(string jsonPath, string exePath, string displayName)
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
                Console.WriteLine("  [+] Configured MCP for {0}", displayName);
                Console.ResetColor();
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
                pAgent["description"] = "Your agent name (e.g. Claude, Cursor, Antigravity, Codex)";
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
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.WriteLine("  |  LetMeKnow MCP Configuration                           |");
            Console.WriteLine("  +--------------------------------------------------------+");
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
            Console.WriteLine("    lmk --mcp-connect           Auto-connect MCP to detected coding agents");
            Console.WriteLine("    lmk --mcp-prompt            Display & copy AI prompt to auto-connect MCP");
            Console.WriteLine("    lmk --install-agents all    Directly configure all agents in this folder");
            Console.WriteLine("    lmk --prompt                Display & copy universal AI agent prompt");
            Console.WriteLine("    lmk --mcp                   Start the stdio MCP server directly");
            Console.WriteLine("    lmk --mcp-toggle            Toggle MCP enabled / disabled");
            Console.WriteLine("    lmk --config                Run interactive wizard to update preferences");
            Console.ResetColor();
            Console.WriteLine();
        }

        public static string GetMcpPrompt()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            return @"Please connect to the LetMeKnow MCP desktop alert tool on this computer:
- Server Name: letmeknow
- Command: """ + exePath + @"""
- Args: [""--mcp""]

Tools Provided:
- `notify_human`: Send desktop alert cards and audio chimes to the user when you need input, credentials, review, or are blocked.
- `ask_human`: Solicit input or decision from the user.

Instructions for AI Agent:
1. If you are Cursor, add this server into `.cursor/mcp.json`.
2. If you are Claude Code, run: claude mcp add letmeknow """ + exePath + @""" -- --mcp
3. If you are Windsurf, add this server into ~/.codeium/windsurf/mcp_config.json.
4. If you support MCP in another tool, register the command and args above.
5. Once added, test the tool by calling `notify_human` with a confirmation message.";
        }

        public static void PrintAndCopyMcpPrompt()
        {
            string prompt = GetMcpPrompt();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.WriteLine("  |  AI Agent Auto-Connect Prompt (MCP)                    |");
            Console.WriteLine("  +--------------------------------------------------------+");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Paste this prompt directly into your AI assistant chat:");
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
                Console.WriteLine("  [OK] Copied to clipboard! Just paste it into your AI agent.");
                Console.ResetColor();
            }
            catch { }
            Console.WriteLine();
        }

        public static void AutoConnectAll(string targetDir)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  Auto-Connecting MCP to AI Coding Assistants...");
            Console.ResetColor();

            string exePath = Assembly.GetExecutingAssembly().Location;

            // 1. Cursor (.cursor/mcp.json)
            try
            {
                string cursorMcp = Path.Combine(targetDir, ".cursor", "mcp.json");
                AgentInstaller.TryInjectMcpConfig(cursorMcp, exePath, "Cursor (.cursor/mcp.json)");
            }
            catch { }

            // 2. Windsurf (~/.codeium/windsurf/mcp_config.json)
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string windsurfMcp = Path.Combine(userProfile, ".codeium", "windsurf", "mcp_config.json");
                AgentInstaller.TryInjectMcpConfig(windsurfMcp, exePath, "Windsurf (~/.codeium/windsurf/mcp_config.json)");
            }
            catch { }

            // 3. Claude Desktop (%APPDATA%\Claude\claude_desktop_config.json)
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string claudeDesktopDir = Path.Combine(appData, "Claude");
                if (Directory.Exists(claudeDesktopDir))
                {
                    string claudeDesktopMcp = Path.Combine(claudeDesktopDir, "claude_desktop_config.json");
                    AgentInstaller.TryInjectMcpConfig(claudeDesktopMcp, exePath, "Claude Desktop");
                }
            }
            catch { }

            // 4. Claude Code CLI helper
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  * For Claude Code terminal CLI, run:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    claude mcp add letmeknow \"" + exePath + "\" -- --mcp");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [OK] Auto-connect completed! No manual JSON configuration needed.");
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
