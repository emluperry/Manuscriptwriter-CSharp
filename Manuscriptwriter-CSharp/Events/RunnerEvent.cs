using System;

namespace MSW.Events
{
    public class RunnerEvent : IRunnerEvent
    {
        private EventHandler<IRunnerEventArgs> runtimeEvent;

        public virtual void FireEvent(object sender, IRunnerEventArgs args)
        {
            runtimeEvent?.Invoke(sender, args);
        }

        public virtual void RegisterEvent(EventHandler<IRunnerEventArgs> e)
        {
            runtimeEvent += e;
        }

        public virtual void UnregisterEvent(EventHandler<IRunnerEventArgs> e)
        {
            runtimeEvent -= e;
        }

        public virtual void ClearAllEvents()
        {
            var invokes = this.runtimeEvent.GetInvocationList();
            foreach (var invoke in invokes)
            {
                this.runtimeEvent -= (EventHandler<IRunnerEventArgs>)invoke;
            }
        }
    }
}