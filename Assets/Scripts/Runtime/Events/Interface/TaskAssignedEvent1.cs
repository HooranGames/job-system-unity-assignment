using System.Collections;
using System.Collections.Generic;
using Project.Runtime;
using Project.Runtime.Events;
using Project.Runtime.Interfaces;
using Project.Runtime.Task;
using UnityEngine;

/// <summary>
/// Event fired when a task is assigned to a robot
/// </summary>
public class TaskAssignedEvent : BaseEvent
{
    public TaskInstance Task { get; private set; }
    public IRobotAgent Robot { get; private set; }

    public TaskAssignedEvent(TaskInstance task, IRobotAgent robot)
    {
        Task = task;
        Robot = robot;
    }
}

