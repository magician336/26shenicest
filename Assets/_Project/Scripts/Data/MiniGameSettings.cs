using UnityEngine;

[CreateAssetMenu(menuName = "Data/MiniGame/Settings", fileName = "MiniGameSettings")]
public class MiniGameSettings : ScriptableObject
{
    [Header("通用设置")]
    [SerializeField] private string gameName = "点击与拖拽";
    [SerializeField] private float timeLimit = 10f;
    [SerializeField] private int targetScore = 5;

    public string GameName => gameName;
    public float TimeLimit => timeLimit;
    public int TargetScore => targetScore;
}
