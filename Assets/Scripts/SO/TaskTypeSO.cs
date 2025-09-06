using UnityEngine;

[CreateAssetMenu(fileName = "New TaskType", menuName = "Robot System/Task Type")]
public class TaskTypeSO : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private float baseDuration = 10f;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color hudColor = Color.white;
    [SerializeField] private string categoryTag = "General";

    public string DisplayName => displayName;
    public float BaseDuration => baseDuration;
    public Sprite Icon => icon;
    public Color HudColor => hudColor;
    public string CategoryTag => categoryTag;

    private void OnValidate()
    {
        if (baseDuration <= 0f)
            baseDuration = 1f;
    }
}