using System.Collections.Generic;
using System.Linq;
using Project.Runtime;
using Project.Runtime.Factories;
using Project.Runtime.Interfaces;
using Project.Runtime.Task;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    /// <summary>
    /// Improved TaskSpawnerUI that uses Factory pattern and proper dependency management.
    /// Follows Single Responsibility Principle by focusing only on task spawning UI.
    /// Uses Factory pattern for entity creation and proper validation.
    /// </summary>
    public class TaskSpawnerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Dropdown taskTypeDropdown;
        [SerializeField] private Transform tasksContainer;

        [Header("Configuration")]
        [SerializeField] private List<TaskTypeSO> availableTaskTypes = new List<TaskTypeSO>();
        [SerializeField] private bool enableDebugLogging = true;

        #region Private Fields

        /// <summary>Currently selected task type from dropdown</summary>
        private TaskTypeSO selectedTaskType;

        /// <summary>Tasks container as RectTransform for UI calculations</summary>
        private RectTransform tasksContainerRect;

        /// <summary>Entity factory for creating task instances</summary>
        private IEntityFactory entityFactory;

        /// <summary>Task manager for registering created tasks</summary>
        private ITaskSelector taskManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
            SetupDependencies();
            SetupUI();
        }

        private void Update()
        {
            HandleTaskCreationInput();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes core components and validates configuration.
        /// </summary>
        private void InitializeComponents()
        {
            // Get tasks container as RectTransform
            tasksContainerRect = tasksContainer as RectTransform;

            if (tasksContainerRect == null)
            {
                Debug.LogError("[TaskSpawnerUI] Tasks container must be a RectTransform");
                enabled = false;
                return;
            }

            // Validate configuration
            if (availableTaskTypes == null || availableTaskTypes.Count == 0)
            {
                Debug.LogWarning("[TaskSpawnerUI] No available task types configured");
            }

            if (taskTypeDropdown == null)
            {
                Debug.LogError("[TaskSpawnerUI] Task type dropdown is not assigned");
                enabled = false;
                return;
            }

            if (enableDebugLogging)
            {
                Debug.Log("[TaskSpawnerUI] TaskSpawnerUI initialized successfully");
            }
        }

        /// <summary>
        /// Sets up dependency references using service locator pattern.
        /// </summary>
        private void SetupDependencies()
        {
            // Get entity factory
            entityFactory = EntityFactory.Instance;
            if (entityFactory == null)
            {
                Debug.LogError("[TaskSpawnerUI] Cannot find EntityFactory instance");
            }

            // Get task manager
            taskManager = TaskBoard.Instance;
            if (taskManager == null)
            {
                Debug.LogError("[TaskSpawnerUI] Cannot find TaskManager instance");
            }
        }

        /// <summary>
        /// Sets up UI components and event listeners.
        /// </summary>
        private void SetupUI()
        {
            PopulateDropdown();
        }

        #endregion

        #region UI Setup

        /// <summary>
        /// Populates the task type dropdown with available options.
        /// Includes proper validation and error handling.
        /// </summary>
        private void PopulateDropdown()
        {
            if (taskTypeDropdown == null)
            {
                Debug.LogError("[TaskSpawnerUI] Cannot populate dropdown: dropdown reference is null");
                return;
            }

            // Clear existing options
            taskTypeDropdown.ClearOptions();

            // Create options list
            List<string> options = new List<string> { "Select Task Type" };

            // Add valid task types
            foreach (var taskType in availableTaskTypes)
            {
                if (taskType != null && !string.IsNullOrEmpty(taskType.DisplayName))
                {
                    options.Add(taskType.DisplayName);
                }
                else if (enableDebugLogging)
                {
                    Debug.LogWarning("[TaskSpawnerUI] Found invalid task type in available list");
                }
            }

            // Add options to dropdown
            taskTypeDropdown.AddOptions(options);

            // Set up event listener
            taskTypeDropdown.onValueChanged.RemoveAllListeners(); // Clean up any existing listeners
            taskTypeDropdown.onValueChanged.AddListener(OnTaskTypeSelected);

            // Initialize selection
            OnTaskTypeSelected(0);

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskSpawnerUI] Populated dropdown with {options.Count - 1} task types");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles task type selection from dropdown.
        /// Updates selected task type and validates selection.
        /// </summary>
        /// <param name="index">Selected dropdown index</param>
        private void OnTaskTypeSelected(int index)
        {
            // Reset selection for default option
            if (index <= 0)
            {
                selectedTaskType = null;
                return;
            }

            // Calculate actual task type index (accounting for "Select Task" option)
            int taskIndex = index - 1;

            // Validate index bounds
            if (taskIndex < 0 || taskIndex >= availableTaskTypes.Count)
            {
                Debug.LogWarning($"[TaskSpawnerUI] Invalid task type index: {taskIndex}");
                selectedTaskType = null;
                return;
            }

            // Set selected task type
            selectedTaskType = availableTaskTypes[taskIndex];

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskSpawnerUI] Selected task type: {selectedTaskType?.DisplayName ?? "null"}");
            }
        }

        /// <summary>
        /// Handles mouse input for task creation.
        /// Validates input location and creates tasks at clicked positions.
        /// </summary>
        private void HandleTaskCreationInput()
        {
            // No task selected, skip input handling
            if (selectedTaskType == null) return;

            // Check for left mouse click
            if (!Input.GetMouseButtonDown(0)) return;

            Vector3 mousePosition = Input.mousePosition;

            // Check if clicking over UI (but not necessarily our container)
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // Only allow clicks within our tasks container
                if (!RectTransformUtility.RectangleContainsScreenPoint(tasksContainerRect, mousePosition))
                {
                    return;
                }
            }

            // Convert mouse position to local container coordinates
            bool isValidPosition = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tasksContainerRect,
                mousePosition,
                null,
                out Vector2 localPoint
            );

            if (!isValidPosition)
            {
                Debug.LogWarning("[TaskSpawnerUI] Failed to convert mouse position to local coordinates");
                return;
            }

            // Create task at clicked position
            Vector3 spawnPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            CreateTask(spawnPosition);

            // Reset dropdown selection
            ResetDropdownSelection();
        }

        #endregion

        #region Task Creation

        /// <summary>
        /// Creates a task instance at the specified position using the Factory pattern.
        /// Implements proper error handling and validation.
        /// </summary>
        /// <param name="spawnPosition">Local position within the tasks container</param>
        private void CreateTask(Vector3 spawnPosition)
        {
            // Validate prerequisites
            if (selectedTaskType == null)
            {
                Debug.LogWarning("[TaskSpawnerUI] Cannot create task: no task type selected");
                return;
            }

            if (entityFactory == null)
            {
                Debug.LogError("[TaskSpawnerUI] Cannot create task: entity factory is null");
                return;
            }

            if (tasksContainer == null)
            {
                Debug.LogError("[TaskSpawnerUI] Cannot create task: tasks container is null");
                return;
            }

            // Use factory to create task instance
            TaskInstance taskInstance = entityFactory.CreateTask(selectedTaskType, spawnPosition, tasksContainer);

            if (taskInstance == null)
            {
                Debug.LogError($"[TaskSpawnerUI] Failed to create task: {selectedTaskType.DisplayName}");
                return;
            }

            // Register task with task manager (factory might have already done this)
            if (taskManager != null && !IsTaskRegistered(taskInstance))
            {
                taskManager.RegisterTask(taskInstance);
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskSpawnerUI] Created task: {selectedTaskType.DisplayName} at position {spawnPosition}");
            }
        }

        /// <summary>
        /// Checks if a task is already registered with the task manager.
        /// Prevents duplicate registrations.
        /// </summary>
        /// <param name="task">Task to check</param>
        /// <returns>True if task is already registered</returns>
        private bool IsTaskRegistered(TaskInstance task)
        {
            if (taskManager == null || task == null) return false;

            // Check if task is in active tasks (assuming TaskBoard has ActiveTasks property)
            var taskBoard = taskManager as TaskBoard;
            return taskBoard?.ActiveTasks.Contains(task) ?? false;
        }

        /// <summary>
        /// Resets dropdown selection to default state.
        /// </summary>
        private void ResetDropdownSelection()
        {
            if (taskTypeDropdown != null)
            {
                taskTypeDropdown.value = 0;
                OnTaskTypeSelected(0);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new task type to the available options.
        /// Updates the dropdown and validates the input.
        /// </summary>
        /// <param name="taskType">Task type to add</param>
        public void AddAvailableTaskType(TaskTypeSO taskType)
        {
            if (taskType == null)
            {
                Debug.LogWarning("[TaskSpawnerUI] Cannot add null task type");
                return;
            }

            if (availableTaskTypes.Contains(taskType))
            {
                Debug.LogWarning($"[TaskSpawnerUI] Task type {taskType.DisplayName} is already available");
                return;
            }

            availableTaskTypes.Add(taskType);
            PopulateDropdown(); // Refresh dropdown

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskSpawnerUI] Added task type: {taskType.DisplayName}");
            }
        }

        /// <summary>
        /// Removes a task type from available options.
        /// Updates the dropdown and handles current selection.
        /// </summary>
        /// <param name="taskType">Task type to remove</param>
        public void RemoveAvailableTaskType(TaskTypeSO taskType)
        {
            if (taskType == null) return;

            bool removed = availableTaskTypes.Remove(taskType);

            if (removed)
            {
                // Clear selection if removing currently selected type
                if (selectedTaskType == taskType)
                {
                    ResetDropdownSelection();
                }

                PopulateDropdown(); // Refresh dropdown

                if (enableDebugLogging)
                {
                    Debug.Log($"[TaskSpawnerUI] Removed task type: {taskType.DisplayName}");
                }
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Clean up event listeners
            if (taskTypeDropdown != null)
            {
                taskTypeDropdown.onValueChanged.RemoveAllListeners();
            }

            if (enableDebugLogging)
            {
                Debug.Log("[TaskSpawnerUI] TaskSpawnerUI destroyed and cleaned up");
            }
        }

        #endregion
    }
}
