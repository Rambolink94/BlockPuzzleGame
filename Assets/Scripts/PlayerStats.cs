using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 5;

    private int _health;

    public int Health => _health;
    
    void Awake()
    {
        _health = maxHealth;
    }

    public void DealDamage(int damage)
    {
        _health -= damage;
        
        if (_health <= 0)
            HandleDeath();
    }

    public void HandleDeath()
    {
        GameManager.Instance.RestartLevel();
    }
}
