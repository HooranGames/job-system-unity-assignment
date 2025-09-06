using System.Collections;
using System.Collections.Generic;
using Project.Runtime.Interfaces;
using Project.Runtime.Task;
using UnityEngine;
namespace Project.Runtime.Events
{
    // <summary>
    /// Event fired when a task is completed
    /// </summary>
    public class TaskCompletedEvent : BaseEvent
    {
        public TaskInstance Task { get; private set; }
        public IRobotAgent Robot { get; private set; }

        public TaskCompletedEvent(TaskInstance task, IRobotAgent robot)
        {
            Task = task;
            Robot = robot;
        }
    }
}
