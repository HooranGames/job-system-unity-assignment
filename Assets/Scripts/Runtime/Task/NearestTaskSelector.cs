using System;
using System.Collections.Generic;
using System.Linq;
using Project.Runtime.Interfaces;
using Project.Runtime.Robot;
using UnityEngine;
namespace Project.Runtime.Task
{
    /// <summary>
    /// Selects the nearest valid task for a robot agent from a list of candidates.
    /// </summary>
    public class NearestTaskSelector : ITaskSelector
    {
        public event Action<TaskInstance> OnTaskAdded;
        public event Action<TaskInstance> OnTaskRemoved;

        public IEnumerable<TaskInstance> GetAvailableTasksForAgent(IRobotAgent agent)
        {
            throw new NotImplementedException();
        }

        public void RegisterTask(TaskInstance task)
        {
            throw new NotImplementedException();
        }

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

        public void UnregisterTask(TaskInstance task)
        {
            throw new NotImplementedException();
        }
    }
}