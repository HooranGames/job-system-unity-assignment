using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
namespace Project.Runtime
{
    /// <summary>
    /// Represents an instance of a task in the environment.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TaskInstance : MonoBehaviour
    {
        [SerializeField] private TaskTypeSO taskType;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI taskNameText;
        [SerializeField] private Image taskIcon;
        [SerializeField] private Image backgroundImage;

        private bool isAssigned = false;
        private bool isCompleted = false;
        private float currentProgress = 0f;
        private Coroutine executionCoroutine;

        public event Action<TaskInstance> OnStarted;
        public event Action<TaskInstance, float> OnProgress;
        public event Action<TaskInstance> OnCompleted;

        public TaskTypeSO TaskType => taskType;
        public bool IsAssigned => isAssigned;
        public bool IsCompleted => isCompleted;
        public float Progress => currentProgress;

        private void Start()
        {
            if (taskType != null)
            {
                SetupUI();
            }

            if (TaskBoard.Instance != null)
            {
                TaskBoard.Instance.RegisterTask(this);
                Debug.Log($"TaskInstance: Registered task {taskType?.DisplayName} to TaskBoard");
            }
            else
            {
                Debug.LogWarning($"TaskInstance: TaskBoard.Instance is null when trying to register {taskType?.DisplayName}");
            }
        }

        private void SetupUI()
        {
            if (taskNameText != null)
                taskNameText.text = taskType.DisplayName;

            if (progressSlider != null)
            {
                progressSlider.value = 0f;
                var fillImage = progressSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = taskType.HudColor;
            }

            if (taskIcon != null && taskType.Icon != null)
                taskIcon.sprite = taskType.Icon;

            if (backgroundImage != null)
                backgroundImage.color = taskType.HudColor * 0.3f; // Lighter background
        }

        public void Initialize(TaskTypeSO type, Vector3 position)
        {
            taskType = type;
            transform.localPosition = position;
            SetupUI();

            if (TaskBoard.Instance != null)
            {
                TaskBoard.Instance.RegisterTask(this);
                Debug.Log($"TaskInstance: Registered task {taskType?.DisplayName} to TaskBoard (via Initialize)");
            }
        }

        public void AssignToRobot()
        {
            Debug.Log($"TaskInstance: Assigning task {taskType?.DisplayName} to robot");
            isAssigned = true;
            OnStarted?.Invoke(this);

            // Visual feedback for assignment
            if (backgroundImage != null)
                backgroundImage.color = Color.yellow * 0.5f;
        }

        public void StartExecution()
        {
            Debug.Log($"TaskInstance: Starting execution of task {taskType?.DisplayName}");
            if (executionCoroutine != null)
                StopCoroutine(executionCoroutine);

            executionCoroutine = StartCoroutine(ExecuteTask());

            // Visual feedback for execution
            if (backgroundImage != null)
                backgroundImage.color = Color.green * 0.3f;
        }

        private IEnumerator ExecuteTask()
        {
            float elapsed = 0f;
            float duration = taskType.BaseDuration;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                currentProgress = Mathf.Clamp01(elapsed / duration);

                if (progressSlider != null)
                    progressSlider.value = currentProgress;

                OnProgress?.Invoke(this, currentProgress);
                yield return null;
            }

            CompleteTask();
        }

        private void CompleteTask()
        {
            Debug.Log($"TaskInstance: Completing task {taskType?.DisplayName}");
            isCompleted = true;
            currentProgress = 1f;

            if (progressSlider != null)
                progressSlider.value = 1f;

            OnCompleted?.Invoke(this);

            // Visual feedback for completion
            if (backgroundImage != null)
                backgroundImage.color = Color.gray * 0.3f;
        }


    }
}