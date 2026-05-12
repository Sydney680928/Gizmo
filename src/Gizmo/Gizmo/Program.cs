// Copyright 2026 Stéphane Sibué
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Gizmo.Primitives;
using System.Reflection;

namespace Gizmo
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.Title = "GIZMO UI";

            var host = new UiDelegate();

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

            // REPL mode
       
            Console.Clear();

            Console.WriteLine(" ████  ███  █████  █     █   ███ ");
            Console.WriteLine("█       █      █   ██   ██  █   █");
            Console.WriteLine("█  ██   █    ██    █ █ █ █  █   █");
            Console.WriteLine("█   █   █   █      █     █  █   █");
            Console.WriteLine(" ████  ███  █████  █     █   ███ ");
            Console.WriteLine();
            Console.WriteLine("Build TUI apps with MOGWAI scripting");
            Console.WriteLine();

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
                Console.Write("> ");

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
                    Console.WriteLine();
                    Console.WriteLine("GIZMO - Help");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  gizmo <script.mog>  Run a script from a file.");
                    Console.WriteLine();
                    Console.WriteLine("Commands (in interactive mode):");
                    Console.WriteLine("  studio                  Start network communication with MOGWAI Studio or VS Code extension.");
                    Console.WriteLine("  <file.mog> run          Run script <file.mog>");
                    Console.WriteLine("  about                   Show information about GIZMO.");
                    Console.WriteLine("  help                    Show this help informations.");
                    Console.WriteLine("  bye                     Exit the application.");
                    Console.WriteLine();
                }
                else if (cmd == "ABOUT")
                {
                    var appVersion = Assembly.GetExecutingAssembly()
                      .GetName()
                      .Version?.ToString(3) ?? "0.1.0";

                    var mogwaiVersion = MOGWAI.Engine.MogwaiEngine.RuntimeVersion.ToString(3);

                    Console.WriteLine();
                    Console.WriteLine($"GIZMO v{appVersion}");
                    Console.WriteLine("Build TUI apps with MOGWAI scripting");
                    Console.WriteLine($"Built on MOGWAI v{mogwaiVersion} · Terminal.Gui v2");
                    Console.WriteLine();
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
    }
}
