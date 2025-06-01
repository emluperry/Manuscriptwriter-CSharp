using MSW.Compiler;
using MSW.Events;
using MSW.Scripting;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MSW.Console
{
    internal class Program
    {
        private static ConsoleDialogue dialogue;

        static void Main(string[] args)
        {
            Manuscript script = RunCompiler(args[0]);

            if (script == null)
            {
                System.Console.WriteLine("Error occurred! Please check the console for more details.");
                return;
            }

            RunManuscript(script);
        }

        static void LogError(string message)
        {
            System.Console.WriteLine(message);
        }

        static Manuscript RunCompiler(string filePath)
        {
            string data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                System.Console.WriteLine("File is empty.");
                return null;
            }

            dialogue = new ConsoleDialogue() { consoleEvent = new RunnerEvent(), inputEvent = new RunnerEvent() };
            var comp = new Compiler.Compiler()
            {
                ErrorLogger = LogError,
                FunctionLibrary = new List<object>() { dialogue }
            };

            return comp.Compile(data);
            
        }

        static void RunManuscript(Manuscript script)
        {
            var runner = new Runner(script) { Logger = LogError, OnFinish = () => { System.Console.WriteLine("Script finished."); } };
            runner.Run();

            RunLoop(runner);

            dialogue.inputEvent.FireEvent(null, new RunnerEventArgs(new List<object>() { "the player", "me" }));
            RunLoop(runner);

            dialogue.inputEvent.FireEvent(null, new RunnerEventArgs(new List<object>() { "the player", "Cat" }));
            RunLoop(runner);
        }

        static void RunLoop(Runner runner)
        {
            while (!runner.IsFinished())
            {
                System.Console.ReadLine();
                dialogue.consoleEvent.FireEvent(null, null);
            }
        }
    }
}
