using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Project.Runtime;

namespace Project.UI
{
    /// <summary>
    /// Manages the UI for spawning and managing robot agents.
    /// </summary>
    public class UI_RobotManager : MonoBehaviour
    {
        [SerializeField] private Button spawnRobotButton;
        [SerializeField] private TMP_Dropdown robotTypeDropdown;
        [SerializeField] private GameObject robotUIPrefab;
        [SerializeField] private List<RobotTypeSO> availableRobotTypes;
        [SerializeField] private Transform robotsContainer;

        private List<RobotAgent> spawnedRobots = new List<RobotAgent>();
        private RobotTypeSO selectedRobotType;

        private void Start()
        {
            SetupUI();
            spawnRobotButton.onClick.AddListener(SpawnRobot);
        }

        private void SetupUI()
        {
            PopulateDropdown();
        }

        private void PopulateDropdown()
        {
            robotTypeDropdown.ClearOptions();
            List<string> options = new List<string>();

            foreach (var robotType in availableRobotTypes)
            {
                if (robotType != null)
                    options.Add(robotType.RobotName);
            }

            robotTypeDropdown.AddOptions(options);
            robotTypeDropdown.onValueChanged.AddListener(OnRobotTypeSelected);

            if (availableRobotTypes.Count > 0)
                OnRobotTypeSelected(0);
        }

        private void OnRobotTypeSelected(int index)
        {
            if (index >= 0 && index < availableRobotTypes.Count)
            {
                selectedRobotType = availableRobotTypes[index];
            }
        }

        private void SpawnRobot()
        {
            if (selectedRobotType == null || robotUIPrefab == null) return;

            Vector3 spawnPosition = FindEmptyRobotPosition();
            GameObject robotObject = Instantiate(robotUIPrefab, robotsContainer);
            robotObject.transform.localPosition = spawnPosition;

            RobotAgent robotAgent = robotObject.GetComponent<RobotAgent>();

            if (robotAgent != null)
            {
                robotAgent.Initialize(selectedRobotType, spawnPosition);
                spawnedRobots.Add(robotAgent);
            }
        }

        private Vector3 FindEmptyRobotPosition()
        {
            // Arrange robots in rows
            int robotCount = robotsContainer.childCount;
            int column = robotCount % 4; // 4 robots per row
            int row = robotCount / 4;

            float x = 20f + (column * 120f);
            float y = -20f - (row * 80f);

            return new Vector3(x, y, 0f);
        }


    }
}