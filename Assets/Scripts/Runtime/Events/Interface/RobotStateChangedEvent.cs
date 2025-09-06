using System.Collections;
using System.Collections.Generic;
using Project.Runtime;
using Project.Runtime.Events;
using Project.Runtime.Interfaces;
using Project.Runtime.Robot;
using UnityEngine;

// <summary>
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

