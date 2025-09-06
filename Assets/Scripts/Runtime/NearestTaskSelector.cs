using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Project.Runtime
{
    /// <summary>
    /// Selects the nearest valid task for a robot agent from a list of candidates.
    /// </summary>
    public class NearestTaskSelector : ITaskSelector
    {
        public TaskInstance SelectTask(RobotAgent agent, IEnumerable<TaskInstance> candidates)
        {
            if (agent == null || candidates == null)
                return null;

            var validTasks = candidates.Where(task =>
                task != null &&
                !task.IsAssigned &&
                agent.CanPerformTask(task.TaskType));

            if (!validTasks.Any())
                return null;

            return validTasks.OrderBy(task =>
                Vector3.Distance(agent.transform.position, task.transform.position))
                .FirstOrDefault();
        }
    }
}