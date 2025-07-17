using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ExecProxy
{
    internal class Program
    {
        static void PrintLn(string s)
        {
            Console.WriteLine($"[ExecProxy] : {s}");
        }

        static bool ParseBool(string s)
        {
            try
            {
                return bool.Parse(s);
            }
            catch
            {
                return false;
            };
        }

        static string JoinArgs(string[] args, int start, int end)
        {
            string ans = "";
            // foreach (var arg in args)
            for(int i = start; i <= end; ++i)
            {
                var arg = args[i];
                ans += $"\"{arg}\" ";
            }
            return ans;
        }

        static string JoinArgs(string[] args)
        {
            return JoinArgs(args, 0, args.Length - 1);
        }

        static int RunProcessSyncInternal(string name, string args, bool admin)
        {
            try
            {
                ProcessStartInfo info;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Use Windows' RunAs Verb to make the UAC prompt appear.
                    ProcessStartInfo infoWin32 = new ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = args,
                        Verb = admin ? "runas" : "",
                        UseShellExecute = true,
                    };
                    info = infoWin32;
                }
                else
                {
                    // Can't use RunAs Verb in Unix systems such as Linux or macOS, since that's Windows only, so we use sudo and pray it works!
                    ProcessStartInfo infoUnix = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = $"{name} {args}",
                        UseShellExecute = true,
                    };
                    info = infoUnix;
                }

                Process? process = Process.Start(info);

                if (process == null)
                    return -1;

                process.OutputDataReceived += (sender, e) =>
                {
                    if(e.Data != null)
                        Console.Out.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if(e.Data != null)
                        Console.Error.WriteLine(e.Data);
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                process.Dispose();
                return process.ExitCode;
            }
            catch(Exception e)
            {
                PrintLn($"Exception : {e.Message}");
                return -2;
            }
        }

        static int RunProcessSync(string name, string args, bool admin)
        {
            if (admin)
            {
                string currentProcessExecutableName = Environment.GetCommandLineArgs()[0];
                string argsExtended = $"false {name} {args}";
                return RunProcessSyncInternal(currentProcessExecutableName, argsExtended, true);
            }
            else
            {
                return RunProcessSyncInternal(name, args, false);
            }
        }

        // Execution command format: execp admin_mode pname arg1 arg2 arg3 ...
        // Example: execp true some_program -x "some arg"
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintLn("Usage : ExecProxy admin-mode program-name [args]");
                return 1;
            }

            bool isAdmin = ParseBool(args[0]);
            string processName = args[1];
            string processArgs = JoinArgs(args, 2, args.Length - 1);

            int ans = RunProcessSync(processName, processArgs, isAdmin);
            PrintLn($"Process Exited with Code : {ans}");

            return ans;
        }
    }
}
