using MSW.Events;
using MSW.Reflection;

namespace MSW.Console
{
    public class ConsoleDialogue
    {
        [MSWEvent("{0} speaks with {1}")] public RunnerEvent inputEvent;

        public RunnerEvent consoleEvent;

        [MSWFunction("{0}: {1}")]
        public object RunDialogue(Context context, string name, string line)
        {
            System.Console.WriteLine($"{name} says: {line}");

            context.WaitForEvent(consoleEvent);
            return null;
        }
    }
}