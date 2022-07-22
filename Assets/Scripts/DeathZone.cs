using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageCooldown = 0.1f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IDamageable damageable))
        {
            StartCoroutine(DealDamage(damageable));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // TODO: This might interrupt the wrong coroutine
        if (other.TryGetComponent(out IDamageable damageable))
        {
            StopCoroutine(DealDamage(damageable));
        }
    }

    private IEnumerator DealDamage(IDamageable damageable)
    {
        while (damageable.Health > 0)
        {
            yield return new WaitForSeconds(damageCooldown);
            damageable.DealDamage(damagePerTick);
        }

        yield return null;
    }
}
