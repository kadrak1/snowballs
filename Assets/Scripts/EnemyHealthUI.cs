
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    public Slider healthSlider;
    public Health enemyHealth;
    public Transform target;
    public Vector3 offset;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (enemyHealth != null && healthSlider != null)
        {
            healthSlider.maxValue = enemyHealth.maxHealth;
            healthSlider.value = enemyHealth.currentHealth;
            enemyHealth.OnDamageTaken.AddListener(UpdateHealthUI);
        }
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamageTaken.RemoveListener(UpdateHealthUI);
        }
    }

    void Update()
    {
        if (target != null && mainCamera != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(mainCamera.transform);
        }
    }

    void UpdateHealthUI(int currentHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
}
