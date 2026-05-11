using MOGWAI.Engine;
using MOGWAI.Interfaces;

namespace Gizmo
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.Title = "MOGWAI CLI";
            Console.Clear();

            Console.WriteLine("█   █   ███    ████  █     █   ███   ███      █   █  ███");
            Console.WriteLine("██ ██  █   █  █      █  █  █  █   █   █       █   █   █ ");
            Console.WriteLine("█ █ █  █   █  █  ██  █  █  █  █████   █       █   █   █ ");
            Console.WriteLine("█   █  █   █  █   █  ██ █ ██  █   █   █       █   █   █ ");
            Console.WriteLine("█   █   ███    ████   █   █   █   █  ███       ███   ███");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            var host = new UiDelegate();

            // Usage: mogwai-ui <script.mog>

            if (args.Length > 0)
            {
                var scriptPath = args[0];

                if (!File.Exists(scriptPath))
                {
                    Console.Error.WriteLine($"File not found: {scriptPath}");
                    return 1;
                }

                var script = await File.ReadAllTextAsync(scriptPath);
                var result = await host.RunScript(script);

                if (result.IsError)
                    return 1;

                return 0;
            }

            // edit mode

            Console.WriteLine("Type 'help' for usage instructions.");
            Console.WriteLine();

            Console.CancelKeyPress += (_, e) =>
            {
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    host.Engine.Halt();
                    e.Cancel = true;
                }
            };

            while (true)
            {
                Console.WriteLine();
                Console.Write("MOGWAI > ");

                var input = Console.ReadLine() ?? string.Empty;
                var cmd = input.Trim().ToUpper();

                if (cmd == "BYE")
                {
                    break;
                }
                else if (cmd == "STUDIO")
                {
                    await host.Engine.StartNetworkCommunication();

                    while (host.Engine.IsSocketServerRunning)
                        await Task.Delay(250);

                    Console.WriteLine();
                    Console.WriteLine("Type 'help' for usage instructions.");
                    Console.WriteLine();
                }
                else if (cmd == "HELP")
                {
                    ShowHelp();
                }
                else
                {
                    try
                    {
                        var result = await host.Engine.RunAsync(input, true);

                        Console.WriteLine();
                        Console.WriteLine(result.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return 0;
        }

        static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("MOGWAI CLI - Help");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  mogwai_ui <script.mog>  Run a MOGWAI script from a file.");
            Console.WriteLine();
            Console.WriteLine("Commands (in interactive mode):");
            Console.WriteLine("  studio                  Start network communication with MOGWAI Studio or VS Code extension.");
            Console.WriteLine("  <file.mog> run          Run MOGWAI script <file.mog>");
            Console.WriteLine("  bye                     Exit the application.");
        }
    }
}
