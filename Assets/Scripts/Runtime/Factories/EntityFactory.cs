using UnityEngine;
using Project.Runtime.Interfaces;
using Project.Utility;
using Project.Runtime.Task;
using Project.Runtime.Robot;

namespace Project.Runtime.Factories
{
    /// <summary>
    /// Factory class for creating robot and task entities.
    /// Implements Factory Pattern to centralize entity creation logic.
    /// Follows Single Responsibility Principle by focusing only on entity creation.
    /// </summary>
    public class EntityFactory : Singleton<EntityFactory>, IEntityFactory
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject robotPrefab;
        [SerializeField] private GameObject taskPrefab;

        [Header("Default Configuration")]
        [SerializeField] private bool enableDebugLogging = true;

        #region IEntityFactory Implementation

        /// <summary>
        /// Creates a robot agent instance with proper initialization.
        /// Encapsulates robot creation logic and ensures consistent setup.
        /// </summary>
        /// <param name="robotType">Type configuration for the robot</param>
        /// <param name="position">World/local position for the robot</param>
        /// <param name="parent">Parent transform (can be null)</param>
        /// <returns>Fully initialized robot agent instance</returns>
        public RobotAgent CreateRobot(RobotTypeSO robotType, Vector3 position, Transform parent)
        {
            // Input validation
            if (robotType == null)
            {
                Debug.LogError("[EntityFactory] Cannot create robot: RobotTypeSO is null");
                return null;
            }

            if (robotPrefab == null)
            {
                Debug.LogError("[EntityFactory] Cannot create robot: Robot prefab is not assigned");
                return null;
            }

            // Create robot instance
            GameObject robotObject = CreateGameObjectFromPrefab(robotPrefab, position, parent, $"Robot_{robotType.RobotName}");

            if (robotObject == null)
            {
                Debug.LogError("[EntityFactory] Failed to instantiate robot GameObject");
                return null;
            }

            // Get or add RobotAgent component
            RobotAgent robotAgent = robotObject.GetComponent<RobotAgent>();
            if (robotAgent == null)
            {
                Debug.LogError($"[EntityFactory] Robot prefab missing RobotAgent component: {robotPrefab.name}");
                Destroy(robotObject);
                return null;
            }

            // Initialize robot with configuration
            try
            {
                robotAgent.Initialize(robotType, position);

                if (enableDebugLogging)
                {
                    Debug.Log($"[EntityFactory] Successfully created robot: {robotType.RobotName} at position {position}");
                }

                return robotAgent;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EntityFactory] Error initializing robot {robotType.RobotName}: {ex.Message}");
                Destroy(robotObject);
                return null;
            }
        }

        /// <summary>
        /// Creates a task instance with proper initialization.
        /// Encapsulates task creation logic and ensures consistent setup.
        /// </summary>
        /// <param name="taskType">Type configuration for the task</param>
        /// <param name="position">World/local position for the task</param>
        /// <param name="parent">Parent transform (can be null)</param>
        /// <returns>Fully initialized task instance</returns>
        public TaskInstance CreateTask(TaskTypeSO taskType, Vector3 position, Transform parent)
        {
            // Input validation
            if (taskType == null)
            {
                Debug.LogError("[EntityFactory] Cannot create task: TaskTypeSO is null");
                return null;
            }

            if (taskPrefab == null)
            {
                Debug.LogError("[EntityFactory] Cannot create task: Task prefab is not assigned");
                return null;
            }

            // Create task instance
            GameObject taskObject = CreateGameObjectFromPrefab(taskPrefab, position, parent, $"Task_{taskType.DisplayName}");

            if (taskObject == null)
            {
                Debug.LogError("[EntityFactory] Failed to instantiate task GameObject");
                return null;
            }

            // Get or add TaskInstance component
            TaskInstance taskInstance = taskObject.GetComponent<TaskInstance>();
            if (taskInstance == null)
            {
                Debug.LogError($"[EntityFactory] Task prefab missing TaskInstance component: {taskPrefab.name}");
                Destroy(taskObject);
                return null;
            }

            // Initialize task with configuration
            try
            {
                taskInstance.Initialize(taskType, position);

                if (enableDebugLogging)
                {
                    Debug.Log($"[EntityFactory] Successfully created task: {taskType.DisplayName} at position {position}");
                }

                return taskInstance;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EntityFactory] Error initializing task {taskType.DisplayName}: {ex.Message}");
                Destroy(taskObject);
                return null;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Helper method to create GameObject from prefab with consistent setup.
        /// Centralizes common instantiation logic and error handling.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="position">Position for the new object</param>
        /// <param name="parent">Parent transform</param>
        /// <param name="name">Name for the new object</param>
        /// <returns>Created GameObject or null if failed</returns>
        private GameObject CreateGameObjectFromPrefab(GameObject prefab, Vector3 position, Transform parent, string name)
        {
            try
            {
                GameObject newObject = Instantiate(prefab, parent);

                // Set position (local if has parent, world if no parent)
                if (parent != null)
                {
                    newObject.transform.localPosition = position;
                }
                else
                {
                    newObject.transform.position = position;
                }

                // Set descriptive name
                newObject.name = name;

                return newObject;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EntityFactory] Error creating GameObject from prefab {prefab.name}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Configuration and Validation

        /// <summary>
        /// Validates that all required prefabs are assigned.
        /// Called during Awake to ensure factory is properly configured.
        /// </summary>
        /// <returns>True if factory is properly configured</returns>
        private bool ValidateConfiguration()
        {
            bool isValid = true;

            if (robotPrefab == null)
            {
                Debug.LogError("[EntityFactory] Robot prefab is not assigned in inspector");
                isValid = false;
            }

            if (taskPrefab == null)
            {
                Debug.LogError("[EntityFactory] Task prefab is not assigned in inspector");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Sets the robot prefab reference.
        /// Useful for runtime configuration or testing.
        /// </summary>
        /// <param name="prefab">Robot prefab to use</param>
        public void SetRobotPrefab(GameObject prefab)
        {
            robotPrefab = prefab;

            if (enableDebugLogging)
            {
                Debug.Log($"[EntityFactory] Robot prefab set to: {prefab?.name ?? "null"}");
            }
        }

        /// <summary>
        /// Sets the task prefab reference.
        /// Useful for runtime configuration or testing.
        /// </summary>
        /// <param name="prefab">Task prefab to use</param>
        public void SetTaskPrefab(GameObject prefab)
        {
            taskPrefab = prefab;

            if (enableDebugLogging)
            {
                Debug.Log($"[EntityFactory] Task prefab set to: {prefab?.name ?? "null"}");
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Validate configuration on startup
            if (!ValidateConfiguration())
            {
                Debug.LogWarning("[EntityFactory] Factory is not properly configured. Some features may not work correctly.");
            }
            else if (enableDebugLogging)
            {
                Debug.Log("[EntityFactory] Entity Factory initialized successfully");
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to validate configuration in inspector.
        /// Helps developers catch configuration issues early.
        /// </summary>
        [ContextMenu("Validate Configuration")]
        public void EditorValidateConfiguration()
        {
            bool isValid = ValidateConfiguration();

            if (isValid)
            {
                Debug.Log("[EntityFactory] Configuration is valid ✓");
            }
            else
            {
                Debug.LogWarning("[EntityFactory] Configuration has issues ⚠️");
            }
        }
#endif

        #endregion
    }
}