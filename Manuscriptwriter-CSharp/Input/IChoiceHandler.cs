using MSW.Reflection;
using System;
using System.Collections.Generic;

namespace MSW.Input
{
    public interface IChoiceHandler
    {
        Action<object, int> OnChoiceSet { get; set; }
        void ShowChoices(Context ctx, List<string> choices);
    }
}
