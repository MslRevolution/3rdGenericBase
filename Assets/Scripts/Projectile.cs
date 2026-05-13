using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{

    Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        StartCoroutine(ProjectileLaunched());
    }

    [SerializeField] public float secondsUntilDecay = 2;
    [SerializeField] public float secondsUntilDestroy = 5;
    IEnumerator ProjectileLaunched()
    {
        yield return new WaitForSeconds(secondsUntilDecay);
        rb.useGravity = true;
        yield return new WaitForSeconds(secondsUntilDestroy);
        Expire();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Expire();
    }

    private void Expire()
    {
        //Do whatever like exploding before destroying the gameobject.
        Destroy(gameObject);
    }
}
