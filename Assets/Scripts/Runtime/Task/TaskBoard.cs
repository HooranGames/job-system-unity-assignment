using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using Project.Utility;
using Project.Runtime.Interfaces;
using Project.Runtime.Events;
using Project.Runtime.Robot;

namespace Project.Runtime.Task
{
    /// <summary>
    /// Improved TaskBoard that implements ITaskManager interface.
    /// Manages all active tasks in the system while maintaining loose coupling.
    /// Uses Event System for better decoupling and follows SOLID principles.
    /// </summary>
    public class TaskBoard : Singleton<TaskBoard>, ITaskSelector
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private float taskCleanupDelay = 0.1f;

        [Header("Runtime Data")]
        [SerializeField] private List<TaskInstance> activeTasks = new();

        /// <summary>
        /// Event Manager reference for decoupled communication
        /// </summary>
        private IEventManager eventManager;

        #region Properties

        /// <summary>
        /// Read-only access to active tasks (filtered for null entries)
        /// </summary>
        public IEnumerable<TaskInstance> ActiveTasks => activeTasks.Where(t => t != null);

        /// <summary>
        /// Current number of active tasks
        /// </summary>
        public int TaskCount => activeTasks.Count(t => t != null);

        #endregion

        #region ITaskManager Implementation

        /// <summary>
        /// Event triggered when a new task is added to the system
        /// </summary>
        public event Action<TaskInstance> OnTaskAdded;

        /// <summary>
        /// Event triggered when a task is removed from the system
        /// </summary>
        public event Action<TaskInstance> OnTaskRemoved;

        /// <summary>
        /// Registers a task instance to the management system.
        /// Implements proper validation and event notification.
        /// </summary>
        /// <param name="task">Task instance to register</param>
        public void RegisterTask(TaskInstance task)
        {
            // Input validation
            if (task == null)
            {
                Debug.LogWarning("[TaskBoard] Attempted to register null task");
                return;
            }

            if (activeTasks.Contains(task))
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[TaskBoard] Task {task.TaskType?.DisplayName} is already registered");
                }
                return;
            }

            // Register task
            activeTasks.Add(task);
            task.OnCompleted += HandleTaskCompleted;

            // Notify subscribers
            OnTaskAdded?.Invoke(task);

            // Publish event through event system
            eventManager?.Publish(new TaskAddedEvent(task));

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskBoard] Registered task: {task.TaskType?.DisplayName}. Total tasks: {TaskCount}");
            }
        }

        /// <summary>
        /// Unregisters a task instance from the management system.
        /// Properly cleans up references and notifies subscribers.
        /// </summary>
        /// <param name="task">Task instance to unregister</param>
        public void UnregisterTask(TaskInstance task)
        {
            if (task == null)
            {
                Debug.LogWarning("[TaskBoard] Attempted to unregister null task");
                return;
            }

            if (!activeTasks.Contains(task))
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[TaskBoard] Task {task.TaskType?.DisplayName} was not registered");
                }
                return;
            }

            // Unregister task
            activeTasks.Remove(task);
            task.OnCompleted -= HandleTaskCompleted;

            // Notify subscribers
            OnTaskRemoved?.Invoke(task);

            // Publish event through event system
            eventManager?.Publish(new TaskRemovedEvent(task));

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskBoard] Unregistered task: {task.TaskType?.DisplayName}. Remaining tasks: {TaskCount}");
            }
        }

        /// <summary>
        /// Gets available tasks for a specific robot agent.
        /// Implements filtering logic based on robot capabilities and task availability.
        /// </summary>
        /// <param name="agent">The robot agent requesting tasks</param>
        /// <returns>Enumerable of available task instances</returns>
        public IEnumerable<TaskInstance> GetAvailableTasksForAgent(IRobotAgent agent)
        {
            if (agent == null)
            {
                Debug.LogWarning("[TaskBoard] GetAvailableTasksForAgent called with null agent");
                return Enumerable.Empty<TaskInstance>();
            }

            // Filter tasks based on availability and robot capabilities
            var availableTasks = activeTasks.Where(task =>
                task != null &&                         // Task exists
                !task.IsAssigned &&                    // Task is not assigned
                !task.IsCompleted &&                   // Task is not completed
                agent.CanPerformTask(task.TaskType)     // Agent can perform this task type
            ).ToList();

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskBoard] Found {availableTasks.Count} available tasks for robot {agent.RobotType?.RobotName}");

                // Log detailed task status for debugging
                foreach (var task in activeTasks.Where(t => t != null))
                {
                    bool canPerform = agent.CanPerformTask(task.TaskType);
                    Debug.Log($"  - Task: {task.TaskType?.DisplayName} | Assigned: {task.IsAssigned} | Completed: {task.IsCompleted} | CanPerform: {canPerform}");
                }
            }

            return availableTasks;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles task completion events.
        /// Manages cleanup and notification of task completion.
        /// </summary>
        /// <param name="task">The completed task instance</param>
        private void HandleTaskCompleted(TaskInstance task)
        {
            if (task == null) return;

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskBoard] Task completed: {task.TaskType?.DisplayName}");
            }

            // Unregister the completed task
            UnregisterTask(task);

            // Schedule task GameObject destruction with delay
            StartCoroutine(DestroyTaskAfterDelay(task, taskCleanupDelay));

            // Publish completion event
            eventManager?.Publish(new TaskCompletedEvent(task, null)); // Robot reference handled elsewhere
        }

        #endregion

        #region Task Management Utilities

        /// <summary>
        /// Destroys a task GameObject after specified delay.
        /// Provides time for animations or final UI updates before destruction.
        /// </summary>
        /// <param name="task">Task to destroy</param>
        /// <param name="delay">Delay before destruction</param>
        /// <returns>Coroutine for delayed destruction</returns>
        private IEnumerator DestroyTaskAfterDelay(TaskInstance task, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (task != null && task.gameObject != null)
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"[TaskBoard] Destroying completed task: {task.TaskType?.DisplayName}");
                }

                Destroy(task.gameObject);
            }
        }

        /// <summary>
        /// Cleans up null references from the active tasks list.
        /// Should be called periodically to maintain list integrity.
        /// </summary>
        public void CleanupNullTasks()
        {
            int initialCount = activeTasks.Count;
            activeTasks.RemoveAll(task => task == null);
            int removedCount = initialCount - activeTasks.Count;

            if (removedCount > 0 && enableDebugLogging)
            {
                Debug.Log($"[TaskBoard] Cleaned up {removedCount} null task references");
            }
        }

        /// <summary>
        /// Gets tasks by specific category tag.
        /// Useful for filtering tasks by type or priority.
        /// </summary>
        /// <param name="categoryTag">Category tag to filter by</param>
        /// <returns>Tasks matching the category</returns>
        public IEnumerable<TaskInstance> GetTasksByCategory(string categoryTag)
        {
            if (string.IsNullOrEmpty(categoryTag))
            {
                return Enumerable.Empty<TaskInstance>();
            }

            return activeTasks.Where(task =>
                task != null &&
                task.TaskType != null &&
                string.Equals(task.TaskType.CategoryTag, categoryTag, StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// Gets count of tasks by state.
        /// Useful for monitoring system performance and load.
        /// </summary>
        /// <param name="assigned">Count assigned tasks if true, unassigned if false</param>
        /// <returns>Count of tasks in specified state</returns>
        public int GetTaskCountByState(bool assigned)
        {
            return activeTasks.Count(task =>
                task != null &&
                task.IsAssigned == assigned &&
                !task.IsCompleted
            );
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Get event manager reference
            eventManager = EventManager.Instance;

            if (enableDebugLogging)
            {
                Debug.Log("[TaskBoard] TaskBoard initialized successfully");
            }
        }

        private void Start()
        {
            // Periodic cleanup of null references
            InvokeRepeating(nameof(CleanupNullTasks), 10f, 30f);
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            var tasksToCleanup = activeTasks.Where(t => t != null).ToList();

            foreach (var task in tasksToCleanup)
            {
                task.OnCompleted -= HandleTaskCompleted;
            }

            activeTasks.Clear();

            if (enableDebugLogging)
            {
                Debug.Log("[TaskBoard] TaskBoard destroyed and cleaned up");
            }
        }

        #endregion

        #region Debug and Statistics

        /// <summary>
        /// Logs detailed statistics about current task state.
        /// Useful for debugging and performance monitoring.
        /// </summary>
        [ContextMenu("Log Task Statistics")]
        public void LogTaskStatistics()
        {
            CleanupNullTasks(); // Ensure accurate counts

            int totalTasks = activeTasks.Count;
            int assignedTasks = GetTaskCountByState(true);
            int availableTasks = GetTaskCountByState(false);
            int completedTasks = activeTasks.Count(t => t != null && t.IsCompleted);

            Debug.Log($"[TaskBoard] Task Statistics:");
            Debug.Log($"  - Total Active Tasks: {totalTasks}");
            Debug.Log($"  - Assigned Tasks: {assignedTasks}");
            Debug.Log($"  - Available Tasks: {availableTasks}");
            Debug.Log($"  - Completed Tasks: {completedTasks}");

            // Log tasks by category
            var categories = activeTasks
                .Where(t => t != null && t.TaskType != null)
                .GroupBy(t => t.TaskType.CategoryTag)
                .ToList();

            if (categories.Any())
            {
                Debug.Log($"  - Tasks by Category:");
                foreach (var category in categories)
                {
                    Debug.Log($"    * {category.Key}: {category.Count()} tasks");
                }
            }
        }

        /// <summary>
        /// Forces cleanup of all completed tasks.
        /// Useful for testing or manual cleanup.
        /// </summary>
        [ContextMenu("Cleanup Completed Tasks")]
        public void CleanupCompletedTasks()
        {
            var completedTasks = activeTasks.Where(t => t != null && t.IsCompleted).ToList();

            foreach (var task in completedTasks)
            {
                UnregisterTask(task);
                if (task.gameObject != null)
                {
                    Destroy(task.gameObject);
                }
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskBoard] Cleaned up {completedTasks.Count} completed tasks");
            }
        }

        public TaskInstance SelectTask(RobotAgent robotAgent, IEnumerable<TaskInstance> availableTasks)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}