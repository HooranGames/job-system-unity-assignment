using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using Project.Runtime.Interfaces;
using Project.Runtime.Events;
using Project.Runtime.Task;

namespace Project.Runtime.Robot
{
    /// <summary>
    /// Improved RobotAgent that follows SOLID principles and uses dependency injection.
    /// Implements IRobotAgent interface for better testability and loose coupling.
    /// Uses Event System for communication instead of direct references.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RobotAgent : MonoBehaviour, IRobotAgent
    {
        [Header("Robot Configuration")]
        [SerializeField] private RobotTypeSO robotType;

        [Header("UI References")]
        [SerializeField] private Image robotImage;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI robotNameText;

        [Header("Behavior Settings")]
        [SerializeField] private bool enableDebugLogging = true;

        #region Private Fields

        /// <summary>Current state of the robot</summary>
        private RobotState currentState = RobotState.Idle;

        /// <summary>Currently assigned task instance</summary>
        private TaskInstance assignedTask;

        /// <summary>Task selection strategy</summary>
        private ITaskSelector taskSelector;

        /// <summary>Task management system</summary>
        private ITaskSelector taskManager;

        /// <summary>Event management system</summary>
        private IEventManager eventManager;

        /// <summary>Movement coroutine reference</summary>
        private Coroutine movementCoroutine;

        /// <summary>Idle animation coroutine reference</summary>
        private Coroutine idleAnimationCoroutine;

        /// <summary>Original position for animations</summary>
        private Vector3 originalPosition;

        /// <summary>Cached transform reference for performance</summary>
        private Transform cachedTransform;

        #endregion

        #region IRobotAgent Implementation

        /// <summary>
        /// Gets the current state of the robot
        /// </summary>
        public RobotState CurrentState => currentState;

        /// <summary>
        /// Gets the robot type configuration
        /// </summary>
        public RobotTypeSO RobotType => robotType;

        /// <summary>
        /// Gets the transform component of the robot
        /// </summary>
        public Transform Transform => cachedTransform ?? (cachedTransform = transform);

        /// <summary>
        /// Checks if robot can perform a specific task type.
        /// Delegates to RobotTypeSO for consistency.
        /// </summary>
        /// <param name="taskType">Task type to check</param>
        /// <returns>True if robot can perform the task</returns>
        public bool CanPerformTask(TaskTypeSO taskType)
        {
            bool canPerform = robotType != null && robotType.CanPerformTask(taskType);

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} can perform {taskType?.DisplayName}: {canPerform}");
            }

            return canPerform;
        }

        /// <summary>
        /// Assigns a task to the robot.
        /// Validates task availability and starts execution process.
        /// </summary>
        /// <param name="task">Task instance to assign</param>
        public void AssignTask(TaskInstance task)
        {
            // Input validation
            if (task == null)
            {
                Debug.LogWarning($"[RobotAgent] {robotType?.RobotName} received null task assignment");
                return;
            }

            if (task.IsAssigned)
            {
                Debug.LogWarning($"[RobotAgent] {robotType?.RobotName} tried to assign already assigned task: {task.TaskType?.DisplayName}");
                return;
            }

            if (!CanPerformTask(task.TaskType))
            {
                Debug.LogWarning($"[RobotAgent] {robotType?.RobotName} cannot perform task: {task.TaskType?.DisplayName}");
                return;
            }

            // Assign task
            assignedTask = task;
            task.AssignToRobot();
            ChangeState(RobotState.Moving);

            // Publish task assignment event
            eventManager?.Publish(new TaskAssignedEvent(task, this));

            // Start movement to task
            StartMovementToTask(task.transform);

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} assigned task: {task.TaskType?.DisplayName}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the robot with specified configuration.
        /// Can be called multiple times for reconfiguration.
        /// </summary>
        /// <param name="type">Robot type configuration</param>
        /// <param name="position">Initial position</param>
        public void Initialize(RobotTypeSO type, Vector3 position)
        {
            robotType = type;
            Transform.localPosition = position;
            originalPosition = position;

            // Re-initialize with new configuration
            PerformInitialization();
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            PerformInitialization();
            SetupDependencies();
            SubscribeToEvents();

            // Check for existing tasks after initialization
            StartCoroutine(CheckForExistingTasksDelayed());
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Performs core initialization logic.
        /// Separated for reusability and testability.
        /// </summary>
        private void PerformInitialization()
        {
            // Cache transform reference
            cachedTransform = transform;

            // Store original position for animations
            if (originalPosition == Vector3.zero)
            {
                originalPosition = Transform.localPosition;
            }

            // Initialize task selector (can be injected later for testing)
            if (taskSelector == null)
            {
                taskSelector = new NearestTaskSelector();
            }

            // Setup UI components
            SetupUI();

            // Set initial state
            ChangeState(RobotState.Idle);

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] Initialized robot: {robotType?.RobotName}");
            }
        }

        /// <summary>
        /// Sets up dependency references.
        /// Uses service locator pattern for now, can be improved with proper DI.
        /// </summary>
        private void SetupDependencies()
        {
            // Get task manager reference
            if (taskManager == null)
            {
                taskManager = TaskBoard.Instance;
                if (taskManager == null)
                {
                    Debug.LogError($"[RobotAgent] {robotType?.RobotName} cannot find TaskManager instance");
                }
            }

            // Get event manager reference
            if (eventManager == null)
            {
                eventManager = EventManager.Instance;
                if (eventManager == null)
                {
                    Debug.LogError($"[RobotAgent] {robotType?.RobotName} cannot find EventManager instance");
                }
            }
        }

        /// <summary>
        /// Subscribes to relevant system events.
        /// Uses event system for loose coupling.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (taskManager != null)
            {
                taskManager.OnTaskAdded += HandleTaskAvailable;
            }

            // Subscribe to additional events if needed
            // eventManager?.Subscribe<SomeOtherEvent>(HandleSomeOtherEvent);
        }

        /// <summary>
        /// Sets up UI components based on robot configuration.
        /// </summary>
        private void SetupUI()
        {
            if (robotType == null) return;

            // Set robot image
            if (robotImage != null && robotType.Sprite != null)
            {
                robotImage.sprite = robotType.Sprite;
            }

            // Set robot name
            if (robotNameText != null)
            {
                robotNameText.text = robotType.RobotName;
            }

            // Initialize status display
            UpdateStatusDisplay();
        }

        #endregion

        #region Task Management

        /// <summary>
        /// Handles new task availability events.
        /// Triggered when new tasks are added to the system.
        /// </summary>
        /// <param name="newTask">Newly available task</param>
        private void HandleTaskAvailable(TaskInstance newTask)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} notified of new task: {newTask?.TaskType?.DisplayName}");
            }

            // Only look for tasks if currently idle or sleeping
            if (currentState == RobotState.Idle || currentState == RobotState.Sleeping)
            {
                TryFindAndAssignTask();
            }
        }

        /// <summary>
        /// Attempts to find and assign a suitable task.
        /// Uses task selector strategy for decision making.
        /// </summary>
        private void TryFindAndAssignTask()
        {
            if (taskManager == null)
            {
                Debug.LogWarning($"[RobotAgent] {robotType?.RobotName} cannot find tasks: TaskManager is null");
                return;
            }

            // Get available tasks from task manager
            var availableTasks = taskManager.GetAvailableTasksForAgent(this);

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} found {availableTasks?.Count()} available tasks");
            }

            // Use task selector to choose best task
            var selectedTask = taskSelector.SelectTask(this, availableTasks);

            if (selectedTask != null)
            {
                AssignTask(selectedTask);
            }
            else
            {
                // No suitable tasks found, go to sleep
                ChangeState(RobotState.Sleeping);

                if (enableDebugLogging)
                {
                    Debug.Log($"[RobotAgent] {robotType?.RobotName} found no suitable tasks, going to sleep");
                }
            }
        }

        /// <summary>
        /// Coroutine to check for existing tasks after initialization.
        /// Delayed to ensure all systems are initialized.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator CheckForExistingTasksDelayed()
        {
            yield return new WaitForEndOfFrame();

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} checking for existing tasks...");
            }

            if (currentState == RobotState.Idle || currentState == RobotState.Sleeping)
            {
                TryFindAndAssignTask();
            }
        }

        #endregion

        #region Task Execution

        /// <summary>
        /// Starts movement towards assigned task.
        /// Stops any existing movement before starting new one.
        /// </summary>
        /// <param name="targetTransform">Target task transform</param>
        private void StartMovementToTask(Transform targetTransform)
        {
            // Stop existing movement
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
            }

            // Start new movement
            movementCoroutine = StartCoroutine(MoveToTask(targetTransform));
        }

        /// <summary>
        /// Coroutine for moving robot to task location.
        /// Simulates movement for UI-based robots.
        /// </summary>
        /// <param name="targetTransform">Target transform to move to</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator MoveToTask(Transform targetTransform)
        {
            if (targetTransform == null || robotType == null)
            {
                Debug.LogError($"[RobotAgent] {robotType?.RobotName} movement failed: invalid target or robot type");
                yield break;
            }

            // Calculate target position with offset
            Vector3 targetPosition = targetTransform.localPosition;
            targetPosition.y += 50f; // UI offset

            // Move towards target
            while (Vector3.Distance(Transform.localPosition, targetPosition) > 5f)
            {
                Transform.localPosition = Vector3.MoveTowards(
                    Transform.localPosition,
                    targetPosition,
                    robotType.MoveSpeed * 50f * Time.deltaTime); // UI speed adjustment

                yield return null;
            }

            // Snap to final position
            Transform.localPosition = targetPosition;

            // Start task execution
            StartTaskExecution();
        }

        /// <summary>
        /// Starts execution of the assigned task.
        /// Changes state and sets up completion handling.
        /// </summary>
        private void StartTaskExecution()
        {
            if (assignedTask == null)
            {
                Debug.LogError($"[RobotAgent] {robotType?.RobotName} tried to execute null task");
                return;
            }

            ChangeState(RobotState.Executing);

            // Subscribe to task completion
            assignedTask.OnCompleted += HandleTaskCompleted;

            // Start task execution
            assignedTask.StartExecution();

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} started executing task: {assignedTask.TaskType?.DisplayName}");
            }
        }

        /// <summary>
        /// Handles task completion.
        /// Cleans up references and looks for next task.
        /// </summary>
        /// <param name="completedTask">The completed task instance</param>
        private void HandleTaskCompleted(TaskInstance completedTask)
        {
            if (completedTask == null) return;

            // Unsubscribe from task events
            completedTask.OnCompleted -= HandleTaskCompleted;

            // Publish completion event
            eventManager?.Publish(new TaskCompletedEvent(completedTask, this));

            // Clear assignment
            assignedTask = null;

            // Return to idle state
            ChangeState(RobotState.Idle);

            // Immediately look for next task
            TryFindAndAssignTask();

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} completed task: {completedTask.TaskType?.DisplayName}");
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Changes robot state and handles state transitions.
        /// Publishes state change events for monitoring.
        /// </summary>
        /// <param name="newState">New state to transition to</param>
        private void ChangeState(RobotState newState)
        {
            RobotState previousState = currentState;
            currentState = newState;

            // Update UI
            UpdateStatusDisplay();

            // Handle state-specific logic
            HandleStateTransition(newState);

            // Publish state change event
            eventManager?.Publish(new RobotStateChangedEvent(this, previousState, newState));

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} state changed: {previousState} → {newState}");
            }
        }

        /// <summary>
        /// Handles state-specific transition logic.
        /// Manages animations and state-based behaviors.
        /// </summary>
        /// <param name="state">New state to handle</param>
        private void HandleStateTransition(RobotState state)
        {
            // Stop any running idle animation
            StopIdleAnimation();

            // Handle state-specific logic
            switch (state)
            {
                case RobotState.Sleeping:
                    StartIdleAnimation();
                    break;

                case RobotState.Idle:
                    // Could add idle-specific behavior here
                    break;

                case RobotState.Moving:
                case RobotState.Executing:
                    // Movement and execution handled elsewhere
                    break;
            }
        }

        #endregion

        #region Animations

        /// <summary>
        /// Starts idle hover animation for sleeping robots.
        /// </summary>
        private void StartIdleAnimation()
        {
            if (idleAnimationCoroutine == null)
            {
                idleAnimationCoroutine = StartCoroutine(IdleHoverAnimation());
            }
        }

        /// <summary>
        /// Stops idle animation if running.
        /// </summary>
        private void StopIdleAnimation()
        {
            if (idleAnimationCoroutine != null)
            {
                StopCoroutine(idleAnimationCoroutine);
                idleAnimationCoroutine = null;

                // Return to original position
                Transform.localPosition = originalPosition;
            }
        }

        /// <summary>
        /// Coroutine for idle hover animation.
        /// Creates subtle floating effect for sleeping robots.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator IdleHoverAnimation()
        {
            if (robotType == null) yield break;

            Vector3 basePosition = originalPosition;

            while (currentState == RobotState.Sleeping)
            {
                float yOffset = Mathf.Sin(Time.time * robotType.HoverFrequency) * robotType.HoverAmplitude * 10f;
                Transform.localPosition = basePosition + Vector3.up * yOffset;
                yield return null;
            }

            // Return to base position when animation ends
            Transform.localPosition = basePosition;
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Updates status display based on current state.
        /// Shows appropriate text and colors for each state.
        /// </summary>
        private void UpdateStatusDisplay()
        {
            if (statusText == null) return;

            // Set status message based on state
            string statusMessage = currentState switch
            {
                RobotState.Idle => "Searching for task...",
                RobotState.Moving => "Moving to task",
                RobotState.Executing => "Working on task",
                RobotState.Sleeping => "Sleeping...",
                _ => "Unknown state"
            };

            statusText.text = statusMessage;

            // Set color based on state
            Color statusColor = currentState switch
            {
                RobotState.Idle => Color.yellow,
                RobotState.Moving => Color.blue,
                RobotState.Executing => Color.green,
                RobotState.Sleeping => Color.white,
                _ => Color.white
            };

            statusText.color = statusColor;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleans up resources and unsubscribes from events.
        /// Called during destruction to prevent memory leaks.
        /// </summary>
        private void CleanupResources()
        {
            // Stop all coroutines
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
            }

            StopIdleAnimation();

            // Unsubscribe from events
            if (taskManager != null)
            {
                taskManager.OnTaskAdded -= HandleTaskAvailable;
            }

            // Clean up task assignment
            if (assignedTask != null)
            {
                assignedTask.OnCompleted -= HandleTaskCompleted;
                assignedTask = null;
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[RobotAgent] {robotType?.RobotName} cleaned up resources");
            }
        }

        #endregion

        #region Dependency Injection (for testing)

        /// <summary>
        /// Injects task selector dependency.
        /// Useful for testing with different selection strategies.
        /// </summary>
        /// <param name="selector">Task selector to inject</param>
        public void InjectTaskSelector(ITaskSelector selector)
        {
            taskSelector = selector;
        }

        /// <summary>
        /// Injects task manager dependency.
        /// Useful for testing with mock task managers.
        /// </summary>
        /// <param name="manager">Task manager to inject</param>
        public void InjectTaskManager(ITaskSelector manager)
        {
            taskManager = manager;
        }

        /// <summary>
        /// Injects event manager dependency.
        /// Useful for testing with mock event managers.
        /// </summary>
        /// <param name="eventMgr">Event manager to inject</param>
        public void InjectEventManager(IEventManager eventMgr)
        {
            eventManager = eventMgr;
        }

        #endregion
    }
}