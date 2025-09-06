using Project.Runtime;
using Project.Runtime.Robot;
using Project.Runtime.Task;
using UnityEngine;

/// <summary>
/// Interface for factory pattern implementation.
/// Creates robot and task instances with proper configuration.
/// </summary>
namespace Project.Runtime.Interfaces
{
    public interface IEntityFactory
    {
        /// <summary>
        /// Creates a robot agent instance
        /// </summary>
        /// <param name="robotType">Type of robot to create</param>
        /// <param name="position">Spawn position</param>
        /// <param name="parent">Parent transform</param>
        /// <returns>Created robot agent</returns>
        RobotAgent CreateRobot(RobotTypeSO robotType, Vector3 position, Transform parent);

        /// <summary>
        /// Creates a task instance
        /// </summary>
        /// <param name="taskType">Type of task to create</param>
        /// <param name="position">Spawn position</param>
        /// <param name="parent">Parent transform</param>
        /// <returns>Created task instance</returns>
        TaskInstance CreateTask(TaskTypeSO taskType, Vector3 position, Transform parent);
    }
}
