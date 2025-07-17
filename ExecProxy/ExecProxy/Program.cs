using System.Diagnostics;

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
            for(int i = start; i < end; ++i)
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

        static int RunProcessSync(string name, string args, bool admin)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = name,
                    Arguments = args,
                    Verb = admin ? "runas" : "",
                    UseShellExecute = true,
                };

                Process? process = Process.Start(info);

                if (process == null)
                    return -1;

                process.WaitForExit();
                return process.ExitCode;
            }
            catch(Exception e)
            {
                PrintLn($"Exception : {e.Message}");
                return -2;
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
