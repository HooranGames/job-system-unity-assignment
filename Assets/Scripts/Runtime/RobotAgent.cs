using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
namespace Project.Runtime
{


    /// <summary>
    /// Represents a robot agent that can perform tasks in the environment.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RobotAgent : MonoBehaviour
    {
        [SerializeField] private RobotTypeSO robotType;
        [SerializeField] private Image robotImage;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI robotNameText;

        private RobotState currentState = RobotState.Idle;
        private TaskInstance assignedTask;
        private ITaskSelector taskSelector;
        private Coroutine movementCoroutine;
        private Coroutine idleAnimationCoroutine;
        private Vector3 originalPosition;

        public RobotState CurrentState => currentState;
        public RobotTypeSO RobotType => robotType;

        private void Start()
        {
            Initialize();
            SubscribeToTaskBoard();
            StartCoroutine(CheckForExistingTasksDelayed());
        }

        private IEnumerator CheckForExistingTasksDelayed()
        {
            yield return new WaitForEndOfFrame();
            Debug.Log($"Robot {robotType?.RobotName} checking for existing tasks...");
            if (currentState == RobotState.Idle || currentState == RobotState.Sleeping)
            {
                TryFindAndAssignTask();
            }
        }

        private void Initialize()
        {
            originalPosition = transform.localPosition;
            taskSelector = new NearestTaskSelector();

            if (robotType != null)
            {
                Debug.Log($"Initializing robot: {robotType.RobotName}");
                Debug.Log($"Robot preferred tasks count: {robotType.PreferredTasks.Count}");

                if (robotImage != null)
                    robotImage.sprite = robotType.Sprite;

                if (robotNameText != null)
                    robotNameText.text = robotType.RobotName;
            }

            ChangeState(RobotState.Idle);
        }

        private void SubscribeToTaskBoard()
        {
            if (TaskBoard.Instance != null)
            {
                TaskBoard.Instance.OnTaskAdded += HandleTaskAvailable;
                Debug.Log($"Robot {robotType?.RobotName} subscribed to TaskBoard");
            }
            else
            {
                Debug.LogWarning("TaskBoard.Instance is null!");
            }
        }

        private void HandleTaskAvailable(TaskInstance newTask)
        {
            Debug.Log($"Robot {robotType?.RobotName} received task available event for: {newTask?.TaskType?.DisplayName}");
            if (currentState == RobotState.Idle || currentState == RobotState.Sleeping)
            {
                TryFindAndAssignTask();
            }
        }

        private void TryFindAndAssignTask()
        {
            if (TaskBoard.Instance == null)
            {
                Debug.LogWarning("TaskBoard.Instance is null in TryFindAndAssignTask!");
                return;
            }

            var availableTasks = TaskBoard.Instance.GetAvailableTasksForRobot(this);
            Debug.Log($"Robot {robotType?.RobotName} found {availableTasks?.Count()} available tasks");

            foreach (var task in availableTasks ?? Enumerable.Empty<TaskInstance>())
            {
                bool canPerform = CanPerformTask(task.TaskType);
                Debug.Log($"Task: {task.TaskType?.DisplayName}, Can perform: {canPerform}, IsAssigned: {task.IsAssigned}");
            }

            var selectedTask = taskSelector.SelectTask(this, availableTasks);

            if (selectedTask != null)
            {
                Debug.Log($"Robot {robotType?.RobotName} selected task: {selectedTask.TaskType?.DisplayName}");
                AssignTask(selectedTask);
            }
            else
            {
                Debug.Log($"Robot {robotType?.RobotName} found no suitable tasks, going to sleep");
                ChangeState(RobotState.Sleeping);
            }
        }

        public void AssignTask(TaskInstance task)
        {
            if (task == null || task.IsAssigned) return;

            Debug.Log($"Robot {robotType?.RobotName} assigned task: {task.TaskType?.DisplayName}");
            assignedTask = task;
            task.AssignToRobot();
            ChangeState(RobotState.Moving);

            if (movementCoroutine != null)
                StopCoroutine(movementCoroutine);

            // For UI robots, we simulate movement to task location
            movementCoroutine = StartCoroutine(MoveToTask(task.transform));
        }

        public bool CanPerformTask(TaskTypeSO taskType)
        {
            bool canPerform = robotType != null && robotType.CanPerformTask(taskType);
            Debug.Log($"Robot {robotType?.RobotName} can perform task {taskType?.DisplayName}: {canPerform}");
            Debug.Log($"Robot preferred tasks count: {robotType?.PreferredTasks.Count}");
            if (robotType?.PreferredTasks.Count > 0)
            {
                Debug.Log($"Preferred tasks: {string.Join(", ", robotType.PreferredTasks.ConvertAll(t => t?.DisplayName ?? "null"))}");
            }
            return canPerform;
        }

        private IEnumerator MoveToTask(Transform targetTransform)
        {
            Vector3 targetPosition = targetTransform.localPosition;
            targetPosition.y += 50f;

            while (Vector3.Distance(transform.localPosition, targetPosition) > 5f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    targetPosition,
                    robotType.MoveSpeed * 50f * Time.deltaTime); // UI speed adjustment
                yield return null;
            }

            transform.localPosition = targetPosition;
            StartTaskExecution();
        }

        private void StartTaskExecution()
        {
            if (assignedTask == null) return;

            ChangeState(RobotState.Executing);
            assignedTask.OnCompleted += HandleTaskCompleted;
            assignedTask.StartExecution();
        }

        private void HandleTaskCompleted(TaskInstance task)
        {
            if (assignedTask != null)
                assignedTask.OnCompleted -= HandleTaskCompleted;

            assignedTask = null;
            ChangeState(RobotState.Idle);

            // Immediately try to find next task
            TryFindAndAssignTask();
        }

        private void ChangeState(RobotState newState)
        {
            currentState = newState;
            UpdateStatusDisplay();
            HandleStateChange(newState);
        }

        private void HandleStateChange(RobotState state)
        {
            // Stop any running animations
            if (idleAnimationCoroutine != null)
            {
                StopCoroutine(idleAnimationCoroutine);
                idleAnimationCoroutine = null;
            }

            if (state == RobotState.Sleeping)
            {
                idleAnimationCoroutine = StartCoroutine(IdleHoverAnimation());
            }
        }

        private IEnumerator IdleHoverAnimation()
        {
            Vector3 basePosition = originalPosition;

            while (currentState == RobotState.Sleeping)
            {
                float yOffset = Mathf.Sin(Time.time * robotType.HoverFrequency) * robotType.HoverAmplitude * 10f; // UI scale
                transform.localPosition = basePosition + Vector3.up * yOffset;
                yield return null;
            }

            transform.localPosition = basePosition;
        }

        private void UpdateStatusDisplay()
        {
            if (statusText == null) return;

            string statusMessage = currentState switch
            {
                RobotState.Idle => "Searching for task...",
                RobotState.Moving => "Moving",
                RobotState.Executing => "Working",
                RobotState.Sleeping => "Sleeping...",
                _ => "Unknown"
            };

            statusText.text = statusMessage;

            // Change color based on state
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

        public void Initialize(RobotTypeSO type, Vector3 position)
        {
            robotType = type;
            transform.localPosition = position;
            originalPosition = position;
            Initialize();
        }

        private void OnDestroy()
        {


            if (assignedTask != null)
                assignedTask.OnCompleted -= HandleTaskCompleted;
        }
    }
}