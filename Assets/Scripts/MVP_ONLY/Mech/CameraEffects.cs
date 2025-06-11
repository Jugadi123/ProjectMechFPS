using System.Collections;
using UnityEngine;
public class CameraEffects : MonoBehaviour
{
    [SerializeField] Camera playerCamera; 
    [SerializeField] Camera playerCockpitCamera;
    [SerializeField] GameObject playerCockpitMesh;

    // private Transform primaryWeapon; // get dynamically
    // private Transform secondaryWeapon; // get dynamically

    public bool allowRotate = true;

    public Vector3 defaultPos;
    public Vector3 defaultRotation;

    public float bobAmount;
    public float bobSpeed;
    private float bobTimer;
    private float verticalVelocity;
    private bool sprintButton;

    private HandleMovement movementHandler;

    private void Awake()
    {
        movementHandler = GetComponent<HandleMovement>();
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        defaultPos = Vector3.zero;
        defaultRotation = playerCamera.transform.rotation.eulerAngles;
        bobAmount = 0.3f;
        bobSpeed = 10f;
    }


    Vector3 GetLeanRotation(float leanAmount) {
        // get move direction
        Vector3 moveDir = movementHandler.wishDir;

        // convert it to local space
        Vector3 localMoveDir = transform.InverseTransformDirection(moveDir);

        // calculate tilt angles
        float tiltX = localMoveDir.z * leanAmount; // forward/back = pitch
        float tiltZ = localMoveDir.x * leanAmount; // left/right = roll

        // return target angles
        return new Vector3(tiltX, 180, tiltZ);
    }


    private void HandleLandingShake()
    {
        if (!movementHandler.IsOnGround)
        {
            verticalVelocity = movementHandler.currentVelocity.y;
            return;
        }

        float impactForce = Mathf.Abs(verticalVelocity);
        if (impactForce > 10f)
        {
            float strength = Mathf.Clamp01(impactForce / 20f);
            StartCoroutine(ShakeObject(strength * 0.1f, 0.2f, playerCamera.transform));
            StartCoroutine(ShakeObject(strength * 0.1f, 0.2f, playerCockpitCamera.transform));
        }

        verticalVelocity = 0f;
    }


    private void HandleThrustingEffects()
    {
        if (movementHandler.IsJumping) return;

        Vector3 fromCockpit = defaultRotation;
        // downward cockpit rotation
        // Vector3 cockpitTarget = new Vector3(defaultRotation.x + 6f, defaultRotation.y, defaultRotation.z);

        // StartCoroutine(RotateTransform(fromCockpit, cockpitTarget, 1f, playerCockpitMesh.transform));
    }


    private void HandleWalkingBobbing()
    {
        if (movementHandler.IsOnGround && movementHandler.WantsToMove && !movementHandler.isDashing && !sprintButton)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
            Quaternion bobRot = Quaternion.Euler(bobOffset, defaultRotation.y, defaultRotation.z);

            playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, bobRot, Time.deltaTime * bobSpeed);
            playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, bobRot, Time.deltaTime * bobSpeed);
        }
        else
        {
            Quaternion defaultRot = Quaternion.Euler(defaultRotation);
            playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, defaultRot, Time.deltaTime * bobSpeed);
            playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, defaultRot, Time.deltaTime * bobSpeed);
        }
    }



    private void HandleSprintLeaning()
    {
        if (!sprintButton || movementHandler.isDashing) return;

        Vector3 targetAngles = GetLeanRotation(2f);
        playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, Quaternion.Euler(targetAngles), Time.deltaTime * 5f);

        bobTimer += Time.deltaTime * bobSpeed * 1.2f;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmount * 1.2f;
        Quaternion bobAngles = Quaternion.Euler(bobOffset, targetAngles.y, targetAngles.z);

        playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
        playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
    }


    private void HandleDashLean()
    {
        // todo needs to be fixed. 
        // * Only tilt on diagonal dashes. Rotate on vertical and horizontal dashes.
        if (movementHandler.isDashing)
        {
            if (!allowRotate) return;
            allowRotate = false;

            Vector3 fromCam = defaultRotation;
            Vector3 toCam = GetLeanRotation(2f);
            Vector3 fromCockpit = defaultRotation;
            Vector3 toCockpit = GetLeanRotation(3f);

            StartCoroutine(RotateTransform(fromCockpit, -toCockpit, 0.4f, playerCockpitCamera.transform));
            StartCoroutine(RotateTransform(fromCam, toCam, 0.4f, playerCamera.transform));
        }
        else
        {
            allowRotate = true;
        }
    }



    private void RunWallJumpCameraEffects(bool canRotate, float targetZRotation)
    {
        // Default rotation is the base camera rotation
        Vector3 target = defaultRotation;

        // apply Z rotation
        if (canRotate)
        {
            target = new Vector3(defaultRotation.x, defaultRotation.y, targetZRotation);
        }

        playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, Quaternion.Euler(target), Time.deltaTime * 2f);

        // Cockpit camera always returns to default rotation (gyro-stabilized)
        // playerCockpitCamera.transform.localRotation = Quaternion.Lerp(
        //     playerCockpitCamera.transform.localRotation,
        //     Quaternion.Euler(defaultRotation),
        //     Time.deltaTime * 2f
        // );

    }

    void Update()
    {
        sprintButton = Input.GetKey(KeyCode.LeftShift);
        RunWallJumpCameraEffects(movementHandler.rotateCameraZ, movementHandler.cameraRotateZAngle);
        HandleThrustingEffects();
        HandleLandingShake();
        HandleWalkingBobbing();
        HandleSprintLeaning();
        HandleDashLean();
    }

    private IEnumerator RotateTransform(Vector3 originalAngles, Vector3 targetAngles, float duration, Transform objectTransform)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            objectTransform.localRotation = Quaternion.Slerp(
                objectTransform.localRotation,
                Quaternion.Euler(targetAngles.x, targetAngles.y, targetAngles.z),
                t
            );
            yield return null;
        }

        objectTransform.localRotation = Quaternion.Slerp(
            objectTransform.localRotation,
            Quaternion.Euler(originalAngles.x, originalAngles.y, originalAngles.z),
            Time.deltaTime * 5f
        );
    }

    private IEnumerator ShakeObject(float strength, float duration, Transform objectToShake)
    {
        Vector3 originalPos = objectToShake.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 offset = Random.insideUnitSphere * strength;
            objectToShake.localPosition = originalPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        objectToShake.localPosition = originalPos;
    }
}