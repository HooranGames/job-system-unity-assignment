
using Project.Runtime.Task;

namespace Project.Runtime.Events
{
    /// <summary>
    /// Event fired when a task is added to the system
    /// </summary>
    public class TaskAddedEvent : BaseEvent
    {
        public TaskInstance Task { get; private set; }

        public TaskAddedEvent(TaskInstance task)
        {
            Task = task;
        }
    }
}
