using System.Collections.Generic;
using System.IO;
using MSW.Compiler;
using MSW.Events;
using MSW.Scripting;

namespace MSW.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RunCompiler(args[0]);
        }

        static void LogError(string message)
        {
            System.Console.WriteLine(message);
        }

        static void RunCompiler(string filePath)
        {
            string data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                System.Console.WriteLine("File is empty.");
                return;
            }

            var dialogue = new ConsoleDialogue() { consoleEvent = new RunnerEvent(), inputEvent = new RunnerEvent() };
            var comp = new Compiler.Compiler()
            {
                ErrorLogger = LogError,
                FunctionLibrary = new List<object>() { dialogue }
            };

            Manuscript script = comp.Compile(data);
            if (script == null)
            {
                System.Console.WriteLine("Error occurred! Please check the console for more details.");
                return;
            }

            var runner = new Runner(script) { Logger = LogError, OnFinish = () => { System.Console.WriteLine("Script finished."); } };
            runner.Run();

            while (!runner.IsFinished())
            {
                System.Console.ReadLine();
                dialogue.consoleEvent.FireEvent(null, null);
            }

            dialogue.inputEvent.FireEvent(null, new RunnerEventArgs(new List<object>() { "the player", "me" }));
            dialogue.inputEvent.FireEvent(null, new RunnerEventArgs(new List<object>() { "the player", "Cat" }));
        }
    }
}
