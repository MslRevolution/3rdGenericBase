using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    #region General Variables

    private Rigidbody rb;
    private PlayerInputActions playerInputActions;
    private Transform cameraAnchor;
    private Animator animator;

    [SerializeField] private Camera cam;
    [SerializeField] private CinemachineVirtualCamera vCam;

    Cinemachine3rdPersonFollow vCam3Person;
    bool aimMode = false;
    #endregion


    #region MONO


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        vCam3Person = vCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        cameraAnchor = transform.Find("CameraAnchor");

        progressiveAdd = ProgressiveAdd();

        playerInputActions = new PlayerInputActions();
        playerInputActions.Standard.Enable();
    }

    private void Start()
    {
        StartCoroutine(TransitionToAimMode());
    }

    private void FixedUpdate()
    {
        Movement();
        Hover();
        PlayerOrientation(aimMode);
    }

    #endregion


    #region Movement
    [Header("Movement")]
    [SerializeField] private float movementAcceleration = 5f;
    [SerializeField] private float maxWalkingSpeed = 5f;
    [SerializeField] private float maxRunningSpeed = 10f;
    [SerializeField] private float deceleration = 2f;

    private float maxSpeed = 5f;
    Vector3 rbVelocityNoYaxis = Vector3.zero;
    Vector3 relativeForward = Vector3.zero;
    Vector3 relativeRight = Vector3.zero;

    private void Movement()
    {
        Vector2 inputVector = playerInputActions.Standard.Movement.ReadValue<Vector2>();

        relativeForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized * inputVector.y;
        relativeRight = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized * inputVector.x;

        //Adding force if we haven't passed the maxSpeed
        if (rbVelocityNoYaxis.magnitude < maxSpeed)
        {
            rb.AddForce(movementAcceleration * rb.mass * relativeForward, ForceMode.Force);
            rb.AddForce(movementAcceleration * rb.mass * relativeRight, ForceMode.Force);
        }

        //Deceleration
        rb.linearVelocity = new Vector3(rb.linearVelocity.x / deceleration, rb.linearVelocity.y, rb.linearVelocity.z / deceleration);
        
        rbVelocityNoYaxis = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        animator.SetFloat("velocityZ", transform.InverseTransformDirection(rb.linearVelocity).z);
        animator.SetFloat("velocityX", transform.InverseTransformDirection(rb.linearVelocity).x);
    }

    public void Run(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            maxSpeed = maxRunningSpeed;
            animator.SetBool("isRunning", true);
        }
        if (context.canceled)
        {
            maxSpeed = maxWalkingSpeed;
            animator.SetBool("isRunning", false);
        }
    }

    #region Hover
    [Header("Hover")]
    [SerializeField] private float hoverDistance = 1f;
    [SerializeField] private float hoverForce = 1f;
    [SerializeField] private float hoverDamping = 1f;
    private void Hover()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position + Vector3.up/2, -Vector3.up * hoverDistance);
        Debug.DrawRay(ray.origin, ray.direction, Color.cyan);

        grounded = false;
        
        if (Physics.Raycast(ray, out hit, hoverDistance))
        {
            if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain"))
            {
                grounded = true;

                float springForce = hoverDistance - (ray.origin.y - hit.point.y);
                springForce = springForce - (rb.linearVelocity.y * hoverDamping);

                rb.AddForce(Vector3.up * rb.mass * Time.deltaTime * hoverForce * springForce);
            }
        }
    }
    #endregion
    #endregion

    #region Rotation
    [Header("Player Orientation")]
    [SerializeField] private float rotationSpeed;
    Quaternion finalRotation = new Quaternion();
    private void PlayerOrientation(bool towardsCamera)
    {
        if (towardsCamera)
        {
            RaycastHit hit;
            
            //TODO
             
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.magenta);//DEBUG

            Vector3 lookPoint = transform.position + cam.transform.forward.normalized;

            if (Physics.Raycast(ray, out hit))
            {
                lookPoint = hit.point;
            }


            lookPoint = new Vector3(lookPoint.x, transform.position.y, lookPoint.z) - transform.position;

            finalRotation = Quaternion.LookRotation(lookPoint);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 lookToMovement = new Vector3(relativeForward.x, 0, relativeForward.z) + new Vector3(relativeRight.x, 0, relativeRight.z);
            Debug.DrawRay(transform.position, lookToMovement * 10, Color.magenta);//DEBUG

            if (relativeForward != Vector3.zero || relativeRight != Vector3.zero)
            {
                finalRotation = Quaternion.LookRotation(lookToMovement.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
            }
        }

        cameraAnchor.transform.rotation =
            Quaternion.Euler(gameObject.transform.rotation.x * -1.0f, gameObject.transform.rotation.y * -1.0f, gameObject.transform.rotation.z * -1.0f);
    }
    #endregion


    #region AimMode
    [Header("AimMode")]
    [SerializeField] private float transitionCameraSideSpeed = 2f;
    private float cameraAimOffset = 0.5f;
    public void AimMode(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Entered aiming mode");//
            cameraAimOffset = 1f;
            aimMode = true;
        }
        if (context.canceled)
        {
            Debug.Log("Exited aiming mode");
            cameraAimOffset = 0.5f;
            aimMode = false;
        }
    }

    IEnumerator TransitionToAimMode()
    {
        while (true)
        {
            vCam3Person.CameraSide = Mathf.Lerp(vCam3Person.CameraSide, cameraAimOffset, Time.deltaTime * transitionCameraSideSpeed);
            yield return null;
        }
    }
    #endregion


    #region Attack
    [Header("Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileForce = 1f;
    [SerializeField] private float forceMultiplierGrowth = 1f;
    [SerializeField] private float forceMultiplierThreshold = 2f;
    [SerializeField] private float maxForceMultiplier = 5f;
    IEnumerator progressiveAdd;
    float forceMultiplier = 1;
    public void Attack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            forceMultiplier = 1;
            StartCoroutine(progressiveAdd);
        }
        if (context.canceled)
        {
            StopCoroutine(progressiveAdd);
            if(forceMultiplier > forceMultiplierThreshold)
            {
                GameObject projectile = Instantiate(projectilePrefab, transform.position + transform.forward + transform.up, new Quaternion());
                projectile.GetComponent<Rigidbody>().AddForce((CameraLookPoint() * projectileForce) * forceMultiplier, ForceMode.Impulse);
            }
        }

    }
    [SerializeField] private Image uiClosingCircle;

    private IEnumerator ProgressiveAdd()
    {
        while (forceMultiplier < maxForceMultiplier)
        {
            forceMultiplier += Time.deltaTime * forceMultiplierGrowth;
            uiClosingCircle.color = new Color(uiClosingCircle.color.r, uiClosingCircle.color.g, uiClosingCircle.color.b, forceMultiplier);
            uiClosingCircle.rectTransform.sizeDelta = new Vector2(forceMultiplier*20, forceMultiplier*20);
            yield return null;
        }
    }
    #endregion

    #region Jump
    [Header("Jump")]
    [SerializeField] private float jumpForce = 5;
    [SerializeField] private bool grounded = true;
    public void Jump(InputAction.CallbackContext context)
    {
        if (!context.performed || !grounded) { return; }
        rb.AddForce(Vector3.up * (rb.mass * jumpForce), ForceMode.Impulse);
    }
    #endregion


    #region Utilities
    private Vector3 CameraLookPoint()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000/*, LayerMask.NameToLayer("")*/))
        {
           return (hit.point - transform.position - Vector3.up).normalized;
        }
        return cam.transform.forward.normalized;
    }
    #endregion


}
