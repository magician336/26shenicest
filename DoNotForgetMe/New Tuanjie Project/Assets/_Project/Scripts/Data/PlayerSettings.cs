using UnityEngine;

[CreateAssetMenu(menuName = "Data/Player/Player Settings", fileName = "PlayerSettings")]
public class PlayerSettings : ScriptableObject
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("交互")]
    [SerializeField] private float interactRange = 1.5f;

    [Header("生命")]
    [SerializeField] private int maxHealth = 3;

    public float MoveSpeed => moveSpeed;
    public float InteractRange => interactRange;
    public int MaxHealth => maxHealth;
}
