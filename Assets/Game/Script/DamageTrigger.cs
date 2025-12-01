using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 10;      // how much damage this trigger applies
    public bool destroyAfterHit = false; // should this object disappear after hurting the player?

    [Header("Optional")]
    public bool oneUseOnly = false;   // if true: trigger can damage once per player per run
    private bool used = false;

    private void OnTriggerEnter(Collider other)
    {
        if (used && oneUseOnly)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        bool damaged = playerHealth.TryTakeDamage(damageAmount);

        if (damaged)
        {
            used = true;

            if (destroyAfterHit)
                Destroy(gameObject);
        }
    }
}
