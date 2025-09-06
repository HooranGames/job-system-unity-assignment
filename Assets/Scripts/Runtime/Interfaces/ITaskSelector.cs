using System;
using System.Collections.Generic;
using Project.Runtime.Robot;
using Project.Runtime.Task;

namespace Project.Runtime.Interfaces
{
    /// <summary>
    /// Interface for task management operations.
    /// Abstracts the task management logic from robot agents.
    /// </summary>
    public interface ITaskSelector
    {
        /// <summary>
        /// Gets available tasks for a specific robot agent
        /// </summary>
        /// <param name="agent">The robot agent requesting tasks</param>
        /// <returns>Enumerable of available task instances</returns>
        IEnumerable<TaskInstance> GetAvailableTasksForAgent(IRobotAgent agent);

        /// <summary>
        /// Registers a task instance to the management system
        /// </summary>
        /// <param name="task">Task instance to register</param>
        void RegisterTask(TaskInstance task);

        /// <summary>
        /// Unregisters a task instance from the management system
        /// </summary>
        /// <param name="task">Task instance to unregister</param>
        void UnregisterTask(TaskInstance task);
        TaskInstance SelectTask(RobotAgent robotAgent, IEnumerable<TaskInstance> availableTasks);

        /// <summary>
        /// Event triggered when a new task is added to the system
        /// </summary>
        event Action<TaskInstance> OnTaskAdded;

        /// <summary>
        /// Event triggered when a task is removed from the system
        /// </summary>
        event Action<TaskInstance> OnTaskRemoved;
    }
}