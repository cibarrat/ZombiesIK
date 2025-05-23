using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieHitbox : MonoBehaviour
{
    private ZombieController zController;

    private void Awake()
    {
        zController = GetComponentInParent<ZombieController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger enter: {other.gameObject.tag}");
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("hit player");
            other.gameObject.GetComponent<PlayerStats>().Damage(zController.AttackDamage);
        }
    }
}
