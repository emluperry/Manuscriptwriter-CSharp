using MSW.Input;
using MSW.Reflection;
using System;
using System.Collections.Generic;

namespace MSW.Console
{
    internal class ConsoleChoiceHandler : IChoiceHandler
    {
        public Action<object, int> OnChoiceSet { get; set; }

        public void ShowChoices(Context ctx, List<string> choices)
        {
            int max = choices.Count;
            for(int i = 0; i < max; i++)
            {
                System.Console.WriteLine($"[{i}] {choices[i]}");
            }

            string input = string.Empty;
            int index = -1;

            do
            {
                input = System.Console.ReadLine();
            }
            while (input == string.Empty || !int.TryParse(input, out index) || index < 0 || index >= max);

            this.OnChoiceSet?.Invoke(this, index);
        }
    }
}
