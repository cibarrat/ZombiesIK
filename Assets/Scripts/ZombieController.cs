using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;

public class ZombieController : MonoBehaviour
{
    [SerializeField] private float HP = 400;
    [SerializeField] private float speed = 2;
    [SerializeField] private float chaseSpeed = 3;
    [SerializeField] private float rotationSpeed = 10;
    [SerializeField] private GameObject weakSpot;
    [SerializeField] private float stunDamageLimit = 40;
    [field: SerializeField] public float AttackDamage { get; private set; } = 20;
    [SerializeField] private AudioClip hurtAudio;
    [SerializeField] private AudioClip attackAudio;
    [SerializeField] private AudioClip idleAudio;
    [SerializeField] private float timeBetweenMoans = 20;
    [SerializeField] private float sightDistance = 300f;
    [SerializeField] private int sightRays = 10;
    [SerializeField] private float roamDistance = 10;
    [SerializeField] private float hitstun = 3;
    [SerializeField] private float chaseTimeout = 3;
    [SerializeField] private float attackEndlag = 3;
    [SerializeField] private float hitboxStartupTime = 0.5f;
    [SerializeField] private float hitboxDuration = 1f;
    [SerializeField] private GameObject hitbox;
    [SerializeField] private TextMeshProUGUI canMoveText;
    [SerializeField] private TextMeshProUGUI canAttackText;
    [SerializeField] private TextMeshProUGUI canRoamText;
    [SerializeField] private TextMeshProUGUI canMoanText;
    [SerializeField] private TextMeshProUGUI isChasingText;
    [SerializeField] private TextMeshProUGUI isAttackingText;
    [SerializeField] private TextMeshProUGUI isRoamingText;
    [SerializeField] private TextMeshProUGUI seesPlayerText;
    public bool debug = false;

    private bool canMoan = true;
    private bool canMove = true;
    private bool canAttack = true;
    private bool canRoam = true;
    private bool seesPlayer = false;
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool isRoaming = false;

    private Transform playerPosition;
    private Vector3 lastPlayerPosition;
    private NavMeshAgent navMeshAgent;
    private Camera sight;
    private AudioSource audioSource;
    private Coroutine chaseCoroutine = null;
    private Coroutine roamCoroutine = null;
    private CharacterController controller;
    private Animator animator;

    private void Awake()
    {
        sight = GetComponentInChildren<Camera>();
        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        controller = GetComponentInChildren<CharacterController>();
        animator = GetComponent<Animator>();
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            foreach (Collider otherCollider in colliders)
            {
                Physics.IgnoreCollision(collider, otherCollider, true);
            }
            
        }
    }

    private void Update()
    {
        animator.SetFloat("Speed", 0);
        if (debug)
        {
            canMoveText.text = $"canMove: {canMove}";
            canAttackText.text = $"canAttack: {canAttack}";
            canRoamText.text = $"canRoam: {canRoam}";
            canMoanText.text = $"canMoan: {canMoan}";
            isChasingText.text = $"isChasing: {isChasing}";
            isAttackingText.text = $"isAttacking: {isAttacking}";
            isRoamingText.text = $"isRoaming: {isRoaming}";
            seesPlayerText.text = $"seesPlayer: {seesPlayer}";
        }
        SeekPlayer();
        Moan();
        Walk();
    
    }

    private void Walk()
    {
        
        if (canMove && isChasing)
        {
            navMeshAgent.speed = chaseSpeed;
            if (seesPlayer)
            {
                //animator.SetTrigger("IsRunning");
                              
                navMeshAgent.destination = playerPosition.position;
                lastPlayerPosition = playerPosition.position;
                //Rotate(navMeshAgent.destination + new Vector3(0, 1, 0), weakSpot.transform);
            }
            else if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
            { 
                navMeshAgent.destination = lastPlayerPosition;
            } else
            {
                isChasing = false;
                canRoam = true;
            }
            if (Vector3.Distance(navMeshAgent.destination, transform.position) <= 1)
            {
                Rotate(navMeshAgent.destination);
            }
        }
        Roam();
        if (canMove && !navMeshAgent.isStopped)
        { 
            animator.SetFloat("Speed", navMeshAgent.speed);
        }
        else
        {
            navMeshAgent.isStopped = true;
        }
  
    }
    private void Roam()
    {
        if (canMove && canRoam && !isChasing && !isRoaming)
        {
            navMeshAgent.speed = speed;
            Vector3 randomPoint = GetRandomPoint(transform.position, roamDistance);
            navMeshAgent.destination = randomPoint;
            isRoaming = true;
            roamCoroutine = StartCoroutine(RoamTimeout(Random.Range(1, 15)));
        }
    }

    private IEnumerator Attack()
    {
        if (!isAttacking && canAttack)
        {
            navMeshAgent.isStopped = true;
            SphereCollider attackRangeCollider = GetComponent<SphereCollider>();
            animator.SetTrigger("IsAttacking");
            isAttacking = true;
            canMove = false;
            canAttack = false;
            canRoam = false;
            StartCoroutine(ActivateAttackHitbox(hitboxStartupTime));
            yield return new WaitForSeconds(attackEndlag);
            navMeshAgent.isStopped = false; 
            canMove = true;
            canAttack = true;
            isAttacking = false;
            canRoam = true;
        }
    }

    private void SeekPlayer()
    {
        CastRayFromScreenPoint(new Vector2(Screen.width / 2, Screen.height / 2));
        for (int i = 1; i <= sightRays; i++)
        {
            // Calcula puntos en la pantalla a izquierda y derecha del centro
            Vector2 leftScreenPoint = new Vector2((Screen.width / 2) - (i * 50), Screen.height / 2);
            Vector2 rightScreenPoint = new Vector2((Screen.width / 2) + (i * 50), Screen.height / 2);

            // Lanza rayos desde esos puntos
            CastRayFromScreenPoint(leftScreenPoint);
            CastRayFromScreenPoint(rightScreenPoint);
        }
        
    }

    void CastRayFromScreenPoint(Vector2 screenPoint)
    {
        // Usa la c�mara para convertir un punto en la pantalla a un rayo en el mundo
        Ray ray = sight.ScreenPointToRay(screenPoint);

        // Lanza el raycast y comprueba si golpea algo
        if (Physics.Raycast(ray, out RaycastHit hit, sightDistance))
        {
            if (hit.collider.gameObject.CompareTag("Player"))
            {
                isChasing = true;
                playerPosition = hit.collider.transform;
                if (chaseCoroutine != null)
                {
                    StopCoroutine(chaseCoroutine);
                    chaseCoroutine = null;
                }
                if (roamCoroutine != null)
                {
                    StopCoroutine (roamCoroutine);
                    roamCoroutine = null;
                }
                seesPlayer = true;
                isRoaming = false;
                canRoam = false;
                sight.transform.LookAt(playerPosition);
            } else
            {
                chaseCoroutine ??= StartCoroutine(ChaseTimeout());
            }
        }
    }

    public void Damage(float damage, Collider hitSpot)
    {
        
        if (HP > 0)
        {
            float chanceOfGrunt = Random.Range(0f, 1f);
            if (hitSpot.gameObject == weakSpot)
            {   
                damage *= Random.Range(1.5f, 2);
                chanceOfGrunt *= 1.1f;
            }
            if (damage >= stunDamageLimit)
            {
                animator.SetTrigger("IsHitStun");
                chanceOfGrunt = 1;
                canAttack = false;
                canMove = false;
                canMoan = false;
                canRoam = false;
                StartCoroutine(Hitstun());
            }
            HP -= damage;
            if (HP < 0)
            {
                animator.SetTrigger("IsDead");
                StopAllCoroutines();
                chanceOfGrunt = 1;
                navMeshAgent.isStopped = true;
                canMove = false;
                canAttack = false;
                canMoan = false;
                isChasing = false;
            }
            if (chanceOfGrunt > 0.5f)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(hurtAudio);
            }
            if (roamCoroutine != null)
            {
                StopCoroutine(roamCoroutine);
                roamCoroutine = null;
            }
            if (chaseCoroutine != null)
            {
                StopCoroutine (chaseCoroutine);
                chaseCoroutine = null;
            }
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform;
            navMeshAgent.destination = playerPosition.position;
            Rotate(playerPosition.position);
            sight.transform.LookAt(playerPosition.position);
            isChasing = true;
            isRoaming = false;
            canRoam = false;
            
        }
    }

    private void Rotate(Vector3 destination)
    {
        Rotate(destination, transform);
    }

    private void Rotate(Vector3 destination, Transform transform)
    {
        Vector3 direction = destination - transform.position;
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }

    private void Moan()
    {
        if (canMoan)
        {
            float chanceOfMoan = Random.Range(0f, 1f);
            if (chanceOfMoan > 0.5)
            {
                audioSource.PlayOneShot(idleAudio);
                canMoan = false;
                StartCoroutine(WaitForNextMoan(timeBetweenMoans));
            }
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (canAttack && other.gameObject.CompareTag("Player"))
        {
            Rotate(other.transform.position);
            if (roamCoroutine != null)
            {
                StopCoroutine(roamCoroutine);
                roamCoroutine = null;
            }
            StartCoroutine(Attack());
            audioSource.Stop();
            audioSource.PlayOneShot(attackAudio);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    private IEnumerator WaitForNextMoan(float time)
    {
        yield return new WaitForSeconds(time);
        canMoan = true;
    }


    private IEnumerator Hitstun()
    {
        yield return new WaitForSeconds(hitstun);
        canMoan = true;
        canMove = true;
        canAttack = true;
        canRoam = true;
    }

    private IEnumerator ChaseTimeout()
    {
        yield return new WaitForSeconds(chaseTimeout);
        seesPlayer = false;
        chaseCoroutine = null;
        sight.transform.localRotation = Quaternion.identity;
        //weakSpot.transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, rotationSpeed * Time.deltaTime);
    }

    private IEnumerator ActivateAttackHitbox(float time)
    {
        yield return new WaitForSeconds(time);
        hitbox.SetActive(true);
        StartCoroutine(DeactivateAttackHitbox(hitboxDuration));
    }
    private IEnumerator DeactivateAttackHitbox(float time)
    {
        yield return new WaitForSeconds(time);
        hitbox.SetActive(false);
    }

    Vector3 GetRandomPoint(Vector3 center, float radius)
    {
        // Generate a random point within a sphere
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;

        // Ensure the point is on the NavMesh
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // Fallback to the center if no valid point is found
        return center;
    }

    private IEnumerator RoamTimeout(float time)
    {
        yield return new WaitForSeconds(time);
        isRoaming = false;
        StartCoroutine(IdleTimeout(Random.Range(0, 10)));
        roamCoroutine = null;
    }

    private IEnumerator IdleTimeout(float time)
    {
        navMeshAgent.isStopped = true;
        yield return new WaitForSeconds(time);
        navMeshAgent.isStopped = false;
        canRoam = true;
    }
}
