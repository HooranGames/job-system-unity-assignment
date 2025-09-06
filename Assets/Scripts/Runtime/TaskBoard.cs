using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using Project.Utility;
namespace Project.Runtime
{
    /// <summary>
    /// Singleton class that manages and tracks all active tasks in the environment.
    /// </summary>
    public class TaskBoard : Singleton<TaskBoard>
    {
        public List<TaskInstance> activeTasks = new List<TaskInstance>();



        public event Action<TaskInstance> OnTaskAdded;
        public event Action<TaskInstance> OnTaskRemoved;

        public IEnumerable<TaskInstance> ActiveTasks => activeTasks.Where(t => t != null);
        public int TaskCount => activeTasks.Count;



        public void RegisterTask(TaskInstance task)
        {
            if (task == null || activeTasks.Contains(task))
                return;

            Debug.Log($"TaskBoard: Registering task {task.TaskType?.DisplayName}");
            activeTasks.Add(task);
            task.OnCompleted += HandleTaskCompleted;
            OnTaskAdded?.Invoke(task);
        }

        public void UnregisterTask(TaskInstance task)
        {
            if (task == null || !activeTasks.Contains(task))
                return;

            Debug.Log($"TaskBoard: Unregistering task {task.TaskType?.DisplayName}");
            activeTasks.Remove(task);
            task.OnCompleted -= HandleTaskCompleted;
            OnTaskRemoved?.Invoke(task);
        }

        private void HandleTaskCompleted(TaskInstance task)
        {
            Debug.Log($"TaskBoard: Task completed {task.TaskType?.DisplayName}");
            UnregisterTask(task);
            StartCoroutine(DestroyTaskAfterDelay(task, 0.1f));
        }

        private IEnumerator DestroyTaskAfterDelay(TaskInstance task, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (task != null)
                Destroy(task.gameObject);
        }

        public IEnumerable<TaskInstance> GetAvailableTasksForRobot(RobotAgent robot)
        {
            var availableTasks = activeTasks.Where(task =>
                task != null &&
                !task.IsAssigned &&
                !task.IsCompleted &&
                robot.CanPerformTask(task.TaskType)).ToList();

            Debug.Log($"TaskBoard: Total active tasks: {activeTasks.Count}");
            foreach (var task in activeTasks)
            {
                if (task != null)
                {
                    Debug.Log($"Task: {task.TaskType?.DisplayName}, IsAssigned: {task.IsAssigned}, IsCompleted: {task.IsCompleted}, CanPerform: {robot.CanPerformTask(task.TaskType)}");
                }
            }
            Debug.Log($"TaskBoard: Available tasks for robot: {availableTasks.Count}");

            return availableTasks;
        }
    }
}