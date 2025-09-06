using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using Project.Runtime.Interfaces;
using Project.Runtime.Events;

namespace Project.Runtime.Task
{
    /// <summary>
    /// Improved TaskInstance that follows SOLID principles and uses proper event management.
    /// Represents an instance of a task in the environment with better separation of concerns.
    /// Uses Event System for decoupled communication and implements proper state management.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TaskInstance : MonoBehaviour
    {
        [Header("Task Configuration")]
        [SerializeField] private TaskTypeSO taskType;

        [Header("UI References")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI taskNameText;
        [SerializeField] private Image taskIcon;
        [SerializeField] private Image backgroundImage;

        [Header("Visual Settings")]
        [SerializeField] private bool enableProgressAnimation = true;
        [SerializeField] private float progressAnimationSpeed = 2f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;

        #region Private Fields

        /// <summary>Current assignment state of the task</summary>
        private bool isAssigned = false;

        /// <summary>Current completion state of the task</summary>
        private bool isCompleted = false;

        /// <summary>Current progress value (0-1)</summary>
        private float currentProgress = 0f;

        /// <summary>Target progress for smooth animations</summary>
        private float targetProgress = 0f;

        /// <summary>Task execution coroutine reference</summary>
        private Coroutine executionCoroutine;

        /// <summary>Progress animation coroutine reference</summary>
        private Coroutine progressAnimationCoroutine;

        /// <summary>Event manager reference for decoupled communication</summary>
        private IEventManager eventManager;

        /// <summary>Task manager reference for registration</summary>
        private ITaskSelector taskManager;

        /// <summary>Cached transform reference for performance</summary>
        private Transform cachedTransform;

        #endregion

        #region Events

        /// <summary>Event fired when task execution starts</summary>
        public event Action<TaskInstance> OnStarted;

        /// <summary>Event fired when task progress changes</summary>
        public event Action<TaskInstance, float> OnProgress;

        /// <summary>Event fired when task is completed</summary>
        public event Action<TaskInstance> OnCompleted;

        #endregion

        #region Properties

        /// <summary>Gets the task type configuration</summary>
        public TaskTypeSO TaskType => taskType;

        /// <summary>Gets whether the task is currently assigned to a robot</summary>
        public bool IsAssigned => isAssigned;

        /// <summary>Gets whether the task is completed</summary>
        public bool IsCompleted => isCompleted;

        /// <summary>Gets current progress (0-1)</summary>
        public float Progress => currentProgress;

        /// <summary>Gets cached transform reference</summary>
        public new Transform transform => cachedTransform ?? (cachedTransform = base.transform);

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
            SetupDependencies();
            SetupUI();
            RegisterWithTaskManager();
        }

        private void Update()
        {
            UpdateProgressAnimation();
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes core components and validates configuration.
        /// </summary>
        private void InitializeComponents()
        {
            // Cache transform reference
            cachedTransform = base.transform;

            // Validate task type
            if (taskType == null)
            {
                Debug.LogWarning("[TaskInstance] TaskInstance created without TaskTypeSO");
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskInstance] Initializing task: {taskType?.DisplayName ?? "Unknown"}");
            }
        }

        /// <summary>
        /// Sets up dependency references.
        /// </summary>
        private void SetupDependencies()
        {
            // Get event manager
            eventManager = EventManager.Instance;
            if (eventManager == null && enableDebugLogging)
            {
                Debug.LogWarning("[TaskInstance] EventManager not found - some events may not be published");
            }

            // Get task manager
            taskManager = TaskBoard.Instance;
            if (taskManager == null && enableDebugLogging)
            {
                Debug.LogWarning("[TaskInstance] TaskManager not found - manual registration required");
            }
        }

        /// <summary>
        /// Sets up UI components based on task configuration.
        /// </summary>
        private void SetupUI()
        {
            if (taskType == null) return;

            SetupTaskNameDisplay();
            SetupProgressSlider();
            SetupTaskIcon();
            SetupBackgroundImage();

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskInstance] UI setup complete for task: {taskType.DisplayName}");
            }
        }

        /// <summary>
        /// Sets up task name display.
        /// </summary>
        private void SetupTaskNameDisplay()
        {
            if (taskNameText != null)
            {
                taskNameText.text = taskType.DisplayName;
            }
        }

        /// <summary>
        /// Sets up progress slider appearance and behavior.
        /// </summary>
        private void SetupProgressSlider()
        {
            if (progressSlider == null) return;

            // Initialize progress
            progressSlider.value = 0f;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;

            // Set progress bar color
            var fillImage = progressSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = taskType.HudColor;
            }
        }

        /// <summary>
        /// Sets up task icon display.
        /// </summary>
        private void SetupTaskIcon()
        {
            if (taskIcon != null && taskType.Icon != null)
            {
                taskIcon.sprite = taskType.Icon;
            }
        }

        /// <summary>
        /// Sets up background image with appropriate color.
        /// </summary>
        private void SetupBackgroundImage()
        {
            if (backgroundImage != null)
            {
                // Set background to a lighter version of the HUD color
                Color backgroundColor = taskType.HudColor;
                backgroundColor.a = 0.3f;
                backgroundImage.color = backgroundColor;
            }
        }

        /// <summary>
        /// Registers this task with the task manager.
        /// </summary>
        private void RegisterWithTaskManager()
        {
            if (taskManager != null)
            {
                taskManager.RegisterTask(this);

                if (enableDebugLogging)
                {
                    Debug.Log($"[TaskInstance] Registered task {taskType?.DisplayName} with TaskManager");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the task with specified configuration and position.
        /// Can be called to reconfigure an existing task instance.
        /// </summary>
        /// <param name="type">Task type configuration</param>
        /// <param name="position">Local position for the task</param>
        public void Initialize(TaskTypeSO type, Vector3 position)
        {
            // Validate input
            if (type == null)
            {
                Debug.LogError("[TaskInstance] Cannot initialize with null TaskTypeSO");
                return;
            }

            // Set configuration
            taskType = type;
            transform.localPosition = position;

            // Re-setup UI with new configuration
            SetupUI();

            // Re-register with task manager
            if (taskManager != null)
            {
                taskManager.RegisterTask(this);

                if (enableDebugLogging)
                {
                    Debug.Log($"[TaskInstance] Re-initialized and registered task: {taskType.DisplayName}");
                }
            }
        }

        /// <summary>
        /// Assigns this task to a robot.
        /// Changes visual state and publishes assignment event.
        /// </summary>
        public void AssignToRobot()
        {
            if (isAssigned)
            {
                Debug.LogWarning($"[TaskInstance] Task {taskType?.DisplayName} is already assigned");
                return;
            }

            if (isCompleted)
            {
                Debug.LogWarning($"[TaskInstance] Cannot assign completed task: {taskType?.DisplayName}");
                return;
            }

            // Update state
            isAssigned = true;

            // Update visual feedback
            UpdateVisualState(TaskVisualState.Assigned);

            // Fire events
            OnStarted?.Invoke(this);
            eventManager?.Publish(new TaskAssignedEvent(this, null)); // Robot reference handled elsewhere

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskInstance] Task assigned: {taskType?.DisplayName}");
            }
        }

        /// <summary>
        /// Starts execution of this task.
        /// Begins progress tracking and visual updates.
        /// </summary>
        public void StartExecution()
        {
            if (!isAssigned)
            {
                Debug.LogWarning($"[TaskInstance] Cannot execute unassigned task: {taskType?.DisplayName}");
                return;
            }

            if (isCompleted)
            {
                Debug.LogWarning($"[TaskInstance] Cannot execute completed task: {taskType?.DisplayName}");
                return;
            }

            // Update visual state
            UpdateVisualState(TaskVisualState.Executing);

            // Start execution coroutine
            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
            }

            executionCoroutine = StartCoroutine(ExecuteTaskCoroutine());

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskInstance] Started execution: {taskType?.DisplayName}");
            }
        }

        #endregion

        #region Task Execution

        /// <summary>
        /// Coroutine that handles task execution over time.
        /// Updates progress and handles completion.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator ExecuteTaskCoroutine()
        {
            if (taskType == null)
            {
                Debug.LogError("[TaskInstance] Cannot execute task: TaskTypeSO is null");
                yield break;
            }

            float elapsed = 0f;
            float duration = taskType.BaseDuration;

            // Execute task over time
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // Calculate progress (0-1)
                float newProgress = Mathf.Clamp01(elapsed / duration);
                UpdateProgress(newProgress);

                yield return null;
            }

            // Ensure final progress is exactly 1.0
            UpdateProgress(1f);

            // Complete the task
            CompleteTask();
        }

        /// <summary>
        /// Updates task progress and notifies subscribers.
        /// </summary>
        /// <param name="newProgress">New progress value (0-1)</param>
        private void UpdateProgress(float newProgress)
        {
            newProgress = Mathf.Clamp01(newProgress);

            if (Math.Abs(currentProgress - newProgress) < 0.001f) return; // Avoid redundant updates

            currentProgress = newProgress;
            targetProgress = newProgress;

            // Fire progress event
            OnProgress?.Invoke(this, currentProgress);

            // Update UI immediately if animation is disabled
            if (!enableProgressAnimation && progressSlider != null)
            {
                progressSlider.value = currentProgress;
            }
        }

        /// <summary>
        /// Completes the task and handles cleanup.
        /// </summary>
        private void CompleteTask()
        {
            if (isCompleted)
            {
                Debug.LogWarning($"[TaskInstance] Task already completed: {taskType?.DisplayName}");
                return;
            }

            // Update state
            isCompleted = true;
            currentProgress = 1f;
            targetProgress = 1f;

            // Update visual state
            UpdateVisualState(TaskVisualState.Completed);

            // Fire completion events
            OnCompleted?.Invoke(this);
            eventManager?.Publish(new TaskCompletedEvent(this, null)); // Robot reference handled elsewhere

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskInstance] Task completed: {taskType?.DisplayName}");
            }
        }

        #endregion

        #region Visual Management

        /// <summary>
        /// Enumeration for different visual states of the task.
        /// </summary>
        private enum TaskVisualState
        {
            Available,
            Assigned,
            Executing,
            Completed
        }

        /// <summary>
        /// Updates visual appearance based on task state.
        /// </summary>
        /// <param name="visualState">Target visual state</param>
        private void UpdateVisualState(TaskVisualState visualState)
        {
            if (backgroundImage == null) return;

            Color targetColor = visualState switch
            {
                TaskVisualState.Available => taskType?.HudColor * 0.3f ?? Color.white * 0.3f,
                TaskVisualState.Assigned => Color.yellow * 0.5f,
                TaskVisualState.Executing => Color.green * 0.3f,
                TaskVisualState.Completed => Color.gray * 0.3f,
                _ => Color.white * 0.3f
            };

            // Ensure alpha is set appropriately
            targetColor.a = 0.3f;
            backgroundImage.color = targetColor;
        }

        /// <summary>
        /// Updates progress slider animation.
        /// Called in Update() to provide smooth progress transitions.
        /// </summary>
        private void UpdateProgressAnimation()
        {
            if (!enableProgressAnimation || progressSlider == null) return;

            // Smoothly animate progress slider to target value
            if (Math.Abs(progressSlider.value - targetProgress) > 0.001f)
            {
                progressSlider.value = Mathf.MoveTowards(
                    progressSlider.value,
                    targetProgress,
                    progressAnimationSpeed * Time.deltaTime
                );
            }
        }

        #endregion

        #region State Validation

        /// <summary>
        /// Validates current task state for debugging.
        /// </summary>
        /// <returns>True if task state is valid</returns>
        public bool ValidateState()
        {
            bool isValid = true;

            // Check for null task type
            if (taskType == null)
            {
                Debug.LogError($"[TaskInstance] Task has null TaskTypeSO - GameObject: {gameObject.name}");
                isValid = false;
            }

            // Check progress bounds
            if (currentProgress < 0f || currentProgress > 1f)
            {
                Debug.LogError($"[TaskInstance] Task progress out of bounds: {currentProgress}");
                isValid = false;
            }

            // Check state consistency
            if (isCompleted && currentProgress < 1f)
            {
                Debug.LogError($"[TaskInstance] Task marked completed but progress < 1.0: {currentProgress}");
                isValid = false;
            }

            if (isCompleted && !isAssigned)
            {
                Debug.LogError($"[TaskInstance] Task completed but not assigned");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Gets current state summary for debugging.
        /// </summary>
        /// <returns>State summary string</returns>
        public string GetStateSummary()
        {
            return $"Task: {taskType?.DisplayName ?? "Unknown"} | " +
                   $"Assigned: {isAssigned} | " +
                   $"Completed: {isCompleted} | " +
                   $"Progress: {currentProgress:F2}";
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up resources and stops coroutines.
        /// Called during destruction to prevent memory leaks.
        /// </summary>
        private void CleanupResources()
        {
            // Stop execution coroutine
            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            // Stop animation coroutine
            if (progressAnimationCoroutine != null)
            {
                StopCoroutine(progressAnimationCoroutine);
                progressAnimationCoroutine = null;
            }

            // Clear event subscribers (they should clean up themselves, but just in case)
            OnStarted = null;
            OnProgress = null;
            OnCompleted = null;

            if (enableDebugLogging)
            {
                Debug.Log($"[TaskInstance] Cleaned up resources for task: {taskType?.DisplayName ?? "Unknown"}");
            }
        }

        #endregion

        #region Debug and Testing

        /// <summary>
        /// Forces task completion for testing purposes.
        /// </summary>
        [ContextMenu("Force Complete Task")]
        public void ForceComplete()
        {
            if (isCompleted)
            {
                Debug.LogWarning($"[TaskInstance] Task already completed: {taskType?.DisplayName}");
                return;
            }

            // Stop execution coroutine
            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            // Force completion
            UpdateProgress(1f);
            CompleteTask();

            Debug.Log($"[TaskInstance] Force completed task: {taskType?.DisplayName}");
        }

        /// <summary>
        /// Resets task to initial state for testing.
        /// </summary>
        [ContextMenu("Reset Task")]
        public void ResetTask()
        {
            // Stop any running coroutines
            if (executionCoroutine != null)
            {
                StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            // Reset state
            isAssigned = false;
            isCompleted = false;
            currentProgress = 0f;
            targetProgress = 0f;

            // Update UI
            UpdateVisualState(TaskVisualState.Available);
            if (progressSlider != null)
            {
                progressSlider.value = 0f;
            }

            Debug.Log($"[TaskInstance] Reset task: {taskType?.DisplayName}");
        }

        /// <summary>
        /// Logs detailed task information for debugging.
        /// </summary>
        [ContextMenu("Log Task Info")]
        public void LogTaskInfo()
        {
            Debug.Log($"[TaskInstance] === Task Information ===");
            Debug.Log($"  Name: {taskType?.DisplayName ?? "Unknown"}");
            Debug.Log($"  Category: {taskType?.CategoryTag ?? "Unknown"}");
            Debug.Log($"  Duration: {taskType?.BaseDuration ?? 0f}s");
            Debug.Log($"  State: {GetStateSummary()}");
            Debug.Log($"  Valid: {ValidateState()}");
            Debug.Log($"  GameObject: {gameObject.name}");
            Debug.Log($"  Position: {transform.localPosition}");
        }

        #endregion
    }
}