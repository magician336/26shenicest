using UnityEngine;

[CreateAssetMenu(menuName = "Data/MiniGame/Settings", fileName = "MiniGameSettings")]
public class MiniGameSettings : ScriptableObject
{
    [Header("通用设置")]
    [SerializeField] private string gameName = "非对称合作小游戏";

    public string GameName => gameName;
}
