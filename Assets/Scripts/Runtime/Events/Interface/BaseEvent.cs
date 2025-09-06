using Project.Runtime.Interfaces;
using Project.Runtime.Robot;
using UnityEngine;

namespace Project.Runtime.Events
{
    /// <summary>
    /// Base class for all system events.
    /// Provides common event structure and timestamp.
    /// </summary>
    public abstract class BaseEvent
    {
        /// <summary>
        /// Timestamp when event was created
        /// </summary>
        public float Timestamp { get; private set; }

        protected BaseEvent()
        {
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event fired when a robot's state changes
    /// </summary>
    public class RobotStateChangedEvent : BaseEvent
    {
        public IRobotAgent Robot { get; private set; }
        public RobotState PreviousState { get; private set; }
        public RobotState NewState { get; private set; }

        public RobotStateChangedEvent(IRobotAgent robot, RobotState previousState, RobotState newState)
        {
            Robot = robot;
            PreviousState = previousState;
            NewState = newState;
        }
    }
}