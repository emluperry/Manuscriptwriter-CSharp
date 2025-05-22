using System;
using MSW.Events;

namespace MSW.Reflection
{
    public class Context
    {
        internal IRunnerEvent pauseEvent = null;

        public void WaitForEvent(IRunnerEvent eventHandler)
        {
            this.pauseEvent = eventHandler;
        }
    }
}