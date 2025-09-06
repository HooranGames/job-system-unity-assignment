using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using Project.Runtime;
namespace Project.UI
{
    /// <
    public class TaskSpawnerUI : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown taskTypeDropdown;
        [SerializeField] private GameObject taskInstancePrefab;
        [SerializeField] private List<TaskTypeSO> availableTaskTypes;
        [SerializeField] private Transform tasksContainer;

        private TaskTypeSO selectedTaskType;
        private RectTransform tasksContainerRect;

        private void Start()
        {
            tasksContainerRect = tasksContainer as RectTransform;
            SetupUI();
        }

        private void SetupUI()
        {
            PopulateDropdown();
        }

        private void PopulateDropdown()
        {
            taskTypeDropdown.ClearOptions();
            List<string> options = new List<string>();

            options.Add("Select Task");

            foreach (var taskType in availableTaskTypes)
            {
                if (taskType != null)
                    options.Add(taskType.DisplayName);
            }

            taskTypeDropdown.AddOptions(options);
            taskTypeDropdown.onValueChanged.AddListener(OnTaskTypeSelected);


            OnTaskTypeSelected(0);
        }

        private void OnTaskTypeSelected(int index)
        {
            if (index <= 0)
            {
                selectedTaskType = null;
                return;
            }

            int taskIndex = index - 1;
            if (taskIndex >= 0 && taskIndex < availableTaskTypes.Count)
            {
                selectedTaskType = availableTaskTypes[taskIndex];
            }
        }

        private void Update()
        {
            if (selectedTaskType == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Input.mousePosition;

                if (EventSystem.current.IsPointerOverGameObject())
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(tasksContainerRect, mousePos))
                        return;
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tasksContainerRect,
                    mousePos,
                    null,
                    out Vector2 localPoint
                );

                Vector3 spawnPosition = new Vector3(localPoint.x, localPoint.y, 0f);
                CreateTask(spawnPosition);

                taskTypeDropdown.value = 0;
                OnTaskTypeSelected(0);
            }
        }

        private void CreateTask(Vector3 spawnPosition)
        {
            if (selectedTaskType == null || taskInstancePrefab == null) return;

            GameObject taskObject = Instantiate(taskInstancePrefab, tasksContainer);
            TaskInstance taskInstance = taskObject.GetComponent<TaskInstance>();

            if (taskInstance != null)
            {
                taskInstance.Initialize(selectedTaskType, spawnPosition);

                if (TaskBoard.Instance != null)
                    TaskBoard.Instance.RegisterTask(taskInstance);
            }
        }
    }
}
