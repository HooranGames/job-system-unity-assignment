

using Project.Runtime.Task;

namespace Project.Runtime.Events
{
    /// <summary>
    /// Event fired when a task is removed from the system
    /// </summary>
    public class TaskRemovedEvent : BaseEvent
    {
        public TaskInstance Task { get; private set; }

        public TaskRemovedEvent(TaskInstance task)
        {
            Task = task;
        }
    }
}
