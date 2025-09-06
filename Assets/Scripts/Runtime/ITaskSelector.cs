using System.Collections.Generic;

namespace Project.Runtime
{
    /// <summary>
    /// Interface for selecting a task from a list of candidates for a robot agent.
    /// </summary>
    public interface ITaskSelector
    {
        TaskInstance SelectTask(RobotAgent agent, IEnumerable<TaskInstance> candidates);
    }
}