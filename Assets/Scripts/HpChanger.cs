
using UnityEngine;
using UnityEngine.UI;


public class HpChanger : MonoBehaviour
{
    [SerializeField] Image hpBar;
    [SerializeField] private Health enemyHealth;
    [SerializeField] private int maxHpAmount = 100;

    void Awake()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<Health>();
        }
    }

    void Start()
    {
        if (enemyHealth != null)
        {
            maxHpAmount = enemyHealth.maxHealth;
            SetFill(enemyHealth.currentHealth);
            enemyHealth.OnDamageTaken.AddListener(SetFill);
        }
        else
        {
            SetFill(maxHpAmount);
        }
    }

    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamageTaken.RemoveListener(SetFill);
        }
    }

    // Совместимость: трактуем amount как текущее значение ХП (а не дельту)
    public void ChangeHp(int amount)
    {
        SetFill(amount);
    }

    void SetFill(int current)
    {
        int max = enemyHealth != null ? enemyHealth.maxHealth : maxHpAmount;
        if (hpBar != null && max > 0)
        {
            hpBar.fillAmount = Mathf.Clamp01((float)current / max);
        }
    }
}
