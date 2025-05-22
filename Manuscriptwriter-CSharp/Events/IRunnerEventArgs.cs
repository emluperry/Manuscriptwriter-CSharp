using System.Collections.Generic;

namespace MSW.Events
{
    public interface IRunnerEventArgs
    {
        public List<object> Args { get; }

        bool HasValidArguments(IList<object> args);
    }
}