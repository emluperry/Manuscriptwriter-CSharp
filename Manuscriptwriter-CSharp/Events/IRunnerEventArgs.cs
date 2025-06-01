using System.Collections.Generic;

namespace MSW.Events
{
    public interface IRunnerEventArgs
    {
        List<object> Args { get; }

        bool HasValidArguments(IList<object> args);
    }
}