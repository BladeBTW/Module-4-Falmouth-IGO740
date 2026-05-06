using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageTrigger : MonoBehaviour
{
    [Header("Damage")]
    public int damageAmount = 5;
    public bool damageOnlyOnce = false;

    [Header("Target")]
    public string playerTag = "Player";

    [Header("Debug")]
    public bool logDamage = true;

    private bool _hasDamaged;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasDamaged && damageOnlyOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (logDamage)
        {
            Debug.Log(
                $"[DamageTrigger] {gameObject.name} damaged {other.name} for {damageAmount}. Trigger position: {transform.position}",
                this
            );
        }

        playerHealth.TakeDamage(damageAmount, gameObject);

        _hasDamaged = true;
    }
}