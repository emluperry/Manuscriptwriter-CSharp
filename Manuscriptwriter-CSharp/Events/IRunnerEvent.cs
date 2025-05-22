using System;

namespace MSW.Events
{
    public interface IRunnerEvent
    {
        void FireEvent(object sender, IRunnerEventArgs args);

        void RegisterEvent(EventHandler<IRunnerEventArgs> e);

        void UnregisterEvent(EventHandler<IRunnerEventArgs> e);

        void ClearAllEvents();
    }
}