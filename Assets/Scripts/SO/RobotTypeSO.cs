using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New RobotType", menuName = "Robot System/Robot Type")]
public class RobotTypeSO : ScriptableObject
{
    [SerializeField] private string robotName;
    [SerializeField] private Sprite sprite;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private List<TaskTypeSO> preferredTasks = new List<TaskTypeSO>();
    [SerializeField] private float hoverAmplitude = 0.1f;
    [SerializeField] private float hoverFrequency = 1f;

    public string RobotName => robotName;
    public Sprite Sprite => sprite;
    public float MoveSpeed => moveSpeed;
    public List<TaskTypeSO> PreferredTasks => preferredTasks;
    public float HoverAmplitude => hoverAmplitude;
    public float HoverFrequency => hoverFrequency;

    public bool CanPerformTask(TaskTypeSO taskType) =>
      taskType != null && (preferredTasks.Count == 0 || preferredTasks.Contains(taskType));

}