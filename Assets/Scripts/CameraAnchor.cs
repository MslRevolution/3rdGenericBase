using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAnchor : MonoBehaviour
{
    private PlayerInputActions playerInputActions;
    private Transform Xaxis;
    private Transform Yaxis;

    private void Awake()
    {
        Xaxis = transform.GetChild(0);
        Yaxis = Xaxis.GetChild(0);

        playerInputActions = new PlayerInputActions();
        playerInputActions.Standard.Enable();
    }

    void FixedUpdate()
    {
        //FollowTarget();
    }


    [Header("CameraOptions")]
    [SerializeField] private Transform target;
    [SerializeField] private float xSensibility = 1f;
    [SerializeField] private float ySensibility = 1f;
    [SerializeField] private float highYlimit = 70;
    [SerializeField] private float lowYlimit = -22f;

    private void FollowTarget()
    {
        transform.position = target.position;
    }

    public void RotateWithMouse(InputAction.CallbackContext context)
    {
        Vector2 inputVector = playerInputActions.Standard.Look.ReadValue<Vector2>();

        Xaxis.Rotate(new Vector2(0,inputVector.x * xSensibility));
        Yaxis.Rotate(new Vector2(-inputVector.y * ySensibility, 0));

        //Y axis limits
        if(Yaxis.eulerAngles.x < 360 + lowYlimit && Yaxis.eulerAngles.x > highYlimit - lowYlimit)
        {
            Yaxis.eulerAngles = new Vector3(lowYlimit, Yaxis.eulerAngles.y);
        }
        else if(Yaxis.eulerAngles.x > highYlimit && Yaxis.eulerAngles.x < highYlimit - lowYlimit)
        {
            Yaxis.eulerAngles = new Vector3(highYlimit, Yaxis.eulerAngles.y);
        }
    }
}
