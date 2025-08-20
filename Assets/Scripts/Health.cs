
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    public UnityEvent OnDeath;
    public UnityEvent<int> OnDamageTaken;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        OnDamageTaken?.Invoke(currentHealth);
        Debug.Log(gameObject.name + " took " + damage + " damage. Current health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamageFrom(GameObject source, int damage)
    {
        if (source == null) return;
        var projectile = source.GetComponent<SnowballProjectile>();
        if (projectile == null)
        {
            return;
        }
        TakeDamage(damage);
    }

    void Die()
    {
        Debug.Log(gameObject.name + " has died.");
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
