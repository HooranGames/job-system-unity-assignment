using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Project.Runtime;
using System.Linq;
using Project.Runtime.Factories;
using Project.Runtime.Interfaces;
using Project.Runtime.Robot;

/// <summary>
/// Improved UI_RobotManager that uses Factory pattern and proper dependency management.
/// Follows Single Responsibility Principle and uses proper separation of concerns.
/// Implements better error handling and validation.
/// </summary>
public class UI_RobotManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button spawnRobotButton;
    [SerializeField] private TMP_Dropdown robotTypeDropdown;
    [SerializeField] private Transform robotsContainer;

    [Header("Configuration")]
    [SerializeField] private List<RobotTypeSO> availableRobotTypes = new List<RobotTypeSO>();
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private int robotsPerRow = 4;
    [SerializeField] private float robotSpacing = 120f;
    [SerializeField] private float rowSpacing = 80f;
    [SerializeField] private Vector2 spawnOffset = new Vector2(20f, -20f);

    #region Private Fields

    /// <summary>List of spawned robot agents for tracking</summary>
    private readonly List<RobotAgent> spawnedRobots = new List<RobotAgent>();

    /// <summary>Currently selected robot type from dropdown</summary>
    private RobotTypeSO selectedRobotType;

    /// <summary>Entity factory for creating robot instances</summary>
    private IEntityFactory entityFactory;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        SetupDependencies();
        SetupUI();
        SetupEventListeners();
    }

    private void OnDestroy()
    {
        CleanupEventListeners();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes components and validates configuration.
    /// </summary>
    private void InitializeComponents()
    {
        // Validate required components
        if (spawnRobotButton == null)
        {
            Debug.LogError("[UI_RobotManager] Spawn robot button is not assigned");
            enabled = false;
            return;
        }

        if (robotTypeDropdown == null)
        {
            Debug.LogError("[UI_RobotManager] Robot type dropdown is not assigned");
            enabled = false;
            return;
        }

        if (robotsContainer == null)
        {
            Debug.LogError("[UI_RobotManager] Robots container is not assigned");
            enabled = false;
            return;
        }

        // Validate configuration
        if (availableRobotTypes == null || availableRobotTypes.Count == 0)
        {
            Debug.LogWarning("[UI_RobotManager] No available robot types configured");
        }

        if (enableDebugLogging)
        {
            Debug.Log("[UI_RobotManager] UI_RobotManager initialized successfully");
        }
    }

    /// <summary>
    /// Sets up dependency references.
    /// </summary>
    private void SetupDependencies()
    {
        // Get entity factory
        entityFactory = EntityFactory.Instance;
        if (entityFactory == null)
        {
            Debug.LogError("[UI_RobotManager] Cannot find EntityFactory instance");
        }
    }

    /// <summary>
    /// Sets up UI components.
    /// </summary>
    private void SetupUI()
    {
        PopulateDropdown();
    }

    /// <summary>
    /// Sets up event listeners for UI components.
    /// </summary>
    private void SetupEventListeners()
    {
        if (spawnRobotButton != null)
        {
            spawnRobotButton.onClick.AddListener(SpawnRobot);
        }
    }

    #endregion

    #region UI Setup

    /// <summary>
    /// Populates the robot type dropdown with available options.
    /// </summary>
    private void PopulateDropdown()
    {
        if (robotTypeDropdown == null)
        {
            Debug.LogError("[UI_RobotManager] Cannot populate dropdown: dropdown reference is null");
            return;
        }

        // Clear existing options
        robotTypeDropdown.ClearOptions();

        // Create options list
        List<string> options = new List<string>();

        // Add valid robot types
        foreach (var robotType in availableRobotTypes)
        {
            if (robotType != null && !string.IsNullOrEmpty(robotType.RobotName))
            {
                options.Add(robotType.RobotName);
            }
            else if (enableDebugLogging)
            {
                Debug.LogWarning("[UI_RobotManager] Found invalid robot type in available list");
            }
        }

        if (options.Count == 0)
        {
            Debug.LogWarning("[UI_RobotManager] No valid robot types found");
            options.Add("No robots available");
        }

        // Add options to dropdown
        robotTypeDropdown.AddOptions(options);

        // Set up event listener
        robotTypeDropdown.onValueChanged.RemoveAllListeners();
        robotTypeDropdown.onValueChanged.AddListener(OnRobotTypeSelected);

        // Initialize selection
        if (availableRobotTypes.Count > 0)
        {
            OnRobotTypeSelected(0);
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[UI_RobotManager] Populated dropdown with {options.Count} robot types");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles robot type selection from dropdown.
    /// </summary>
    /// <param name="index">Selected dropdown index</param>
    private void OnRobotTypeSelected(int index)
    {
        // Validate index bounds
        if (index < 0 || index >= availableRobotTypes.Count)
        {
            Debug.LogWarning($"[UI_RobotManager] Invalid robot type index: {index}");
            selectedRobotType = null;
            return;
        }

        // Set selected robot type
        selectedRobotType = availableRobotTypes[index];

        if (enableDebugLogging)
        {
            Debug.Log($"[UI_RobotManager] Selected robot type: {selectedRobotType?.RobotName ?? "null"}");
        }
    }

    #endregion

    #region Robot Management

    /// <summary>
    /// Spawns a new robot using the Factory pattern.
    /// Implements proper validation and positioning.
    /// </summary>
    private void SpawnRobot()
    {
        // Validate prerequisites
        if (selectedRobotType == null)
        {
            Debug.LogWarning("[UI_RobotManager] Cannot spawn robot: no robot type selected");
            return;
        }

        if (entityFactory == null)
        {
            Debug.LogError("[UI_RobotManager] Cannot spawn robot: entity factory is null");
            return;
        }

        if (robotsContainer == null)
        {
            Debug.LogError("[UI_RobotManager] Cannot spawn robot: robots container is null");
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition = CalculateNextSpawnPosition();

        // Use factory to create robot
        RobotAgent robotAgent = entityFactory.CreateRobot(selectedRobotType, spawnPosition, robotsContainer);

        if (robotAgent == null)
        {
            Debug.LogError($"[UI_RobotManager] Failed to create robot: {selectedRobotType.RobotName}");
            return;
        }

        // Track spawned robot
        spawnedRobots.Add(robotAgent);

        if (enableDebugLogging)
        {
            Debug.Log($"[UI_RobotManager] Spawned robot: {selectedRobotType.RobotName} at position {spawnPosition}. Total robots: {spawnedRobots.Count}");
        }
    }

    /// <summary>
    /// Calculates the next spawn position for a new robot.
    /// Arranges robots in a grid layout.
    /// </summary>
    /// <returns>Local position for the next robot</returns>
    private Vector3 CalculateNextSpawnPosition()
    {
        // Count active robots (excluding destroyed ones)
        int activeRobotCount = spawnedRobots.Count(r => r != null);

        // Calculate grid position
        int column = activeRobotCount % robotsPerRow;
        int row = activeRobotCount / robotsPerRow;

        // Calculate world position
        float x = spawnOffset.x + (column * robotSpacing);
        float y = spawnOffset.y - (row * rowSpacing);

        return new Vector3(x, y, 0f);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the count of currently active (non-destroyed) robots.
    /// </summary>
    /// <returns>Number of active robots</returns>
    public int GetActiveRobotCount()
    {
        // Clean up null references
        spawnedRobots.RemoveAll(r => r == null);
        return spawnedRobots.Count;
    }

    /// <summary>
    /// Gets all currently active robot agents.
    /// </summary>
    /// <returns>Enumerable of active robot agents</returns>
    public IEnumerable<RobotAgent> GetActiveRobots()
    {
        // Clean up null references and return active robots
        spawnedRobots.RemoveAll(r => r == null);
        return spawnedRobots.AsReadOnly();
    }

    /// <summary>
    /// Adds a new robot type to available options.
    /// </summary>
    /// <param name="robotType">Robot type to add</param>
    public void AddAvailableRobotType(RobotTypeSO robotType)
    {
        if (robotType == null)
        {
            Debug.LogWarning("[UI_RobotManager] Cannot add null robot type");
            return;
        }

        if (availableRobotTypes.Contains(robotType))
        {
            Debug.LogWarning($"[UI_RobotManager] Robot type {robotType.RobotName} is already available");
            return;
        }

        availableRobotTypes.Add(robotType);
        PopulateDropdown(); // Refresh dropdown

        if (enableDebugLogging)
        {
            Debug.Log($"[UI_RobotManager] Added robot type: {robotType.RobotName}");
        }
    }

    /// <summary>
    /// Removes a robot type from available options.
    /// </summary>
    /// <param name="robotType">Robot type to remove</param>
    public void RemoveAvailableRobotType(RobotTypeSO robotType)
    {
        if (robotType == null) return;

        bool removed = availableRobotTypes.Remove(robotType);

        if (removed)
        {
            // Update selection if removing currently selected type
            if (selectedRobotType == robotType)
            {
                selectedRobotType = availableRobotTypes.Count > 0 ? availableRobotTypes[0] : null;
                robotTypeDropdown.value = 0;
            }

            PopulateDropdown(); // Refresh dropdown

            if (enableDebugLogging)
            {
                Debug.Log($"[UI_RobotManager] Removed robot type: {robotType.RobotName}");
            }
        }
    }

    /// <summary>
    /// Destroys all spawned robots.
    /// Useful for cleanup or reset functionality.
    /// </summary>
    [ContextMenu("Destroy All Robots")]
    public void DestroyAllRobots()
    {
        int destroyedCount = 0;

        for (int i = spawnedRobots.Count - 1; i >= 0; i--)
        {
            if (spawnedRobots[i] != null)
            {
                Destroy(spawnedRobots[i].gameObject);
                destroyedCount++;
            }
        }

        spawnedRobots.Clear();

        if (enableDebugLogging)
        {
            Debug.Log($"[UI_RobotManager] Destroyed {destroyedCount} robots");
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleans up event listeners to prevent memory leaks.
    /// </summary>
    private void CleanupEventListeners()
    {
        if (spawnRobotButton != null)
        {
            spawnRobotButton.onClick.RemoveAllListeners();
        }

        if (robotTypeDropdown != null)
        {
            robotTypeDropdown.onValueChanged.RemoveAllListeners();
        }

        if (enableDebugLogging)
        {
            Debug.Log("[UI_RobotManager] UI_RobotManager destroyed and cleaned up");
        }
    }

    #endregion
}
