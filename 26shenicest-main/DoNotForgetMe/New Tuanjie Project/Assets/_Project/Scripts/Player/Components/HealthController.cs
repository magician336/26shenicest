using System;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    [SerializeField] private PlayerSettings playerSettings;

    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDie;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public void ApplyPlayerSettings(PlayerSettings settings)
    {
        playerSettings = settings;
        if (playerSettings != null)
        {
            maxHealth = playerSettings.MaxHealth;
        }
    }

    private void Start()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDie?.Invoke();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandlePlayerDeath(this);
        }
    }
}
