
using UnityEngine;

public class SnowballController : MonoBehaviour
{
    public float lifetime = 5f;
    public int damage = 10;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Snowball hit: " + collision.gameObject.name);

        Health health = collision.gameObject.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
