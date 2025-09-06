using Project.Runtime;
using Project.Runtime.Robot;
using Project.Runtime.Task;
using UnityEngine;

/// <summary>
/// Interface for robot agent core functionality.
/// Defines the contract for robot behavior and capabilities.
/// </summary>
namespace Project.Runtime.Interfaces
{
    public interface IRobotAgent
    {
        /// <summary>
        /// Gets the current state of the robot
        /// </summary>
        RobotState CurrentState { get; }

        /// <summary>
        /// Gets the robot type configuration
        /// </summary>
        RobotTypeSO RobotType { get; }

        /// <summary>
        /// Gets the transform component of the robot
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Checks if robot can perform a specific task type
        /// </summary>
        /// <param name="taskType">Task type to check</param>
        /// <returns>True if robot can perform the task</returns>
        bool CanPerformTask(TaskTypeSO taskType);

        /// <summary>
        /// Assigns a task to the robot
        /// </summary>
        /// <param name="task">Task instance to assign</param>
        void AssignTask(TaskInstance task);
    }
}