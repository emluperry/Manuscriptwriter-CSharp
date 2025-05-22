using System;
using System.Collections.Generic;

namespace MSW.Events
{
    public class RunnerEventArgs : EventArgs, IRunnerEventArgs
    {
        public List<object> Args { get; }

        public RunnerEventArgs(List<object> args)
        {
            this.Args = args;
        }

        public bool HasValidArguments(IList<object> args)
        {
            return this.Args.Count.Equals(args.Count);
        }
    }
}