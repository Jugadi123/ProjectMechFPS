using System.Collections;
using UnityEngine;
using Unity.Netcode;
public class CameraEffects : NetworkBehaviour
{
    [SerializeField] Camera playerCamera; // Player's main camera. NOT cockpit camera. That camera is only for the ui and cockpit layer.
    [SerializeField] Camera playerCockpitCamera;

    [SerializeField] GameObject playerCockpitMesh;

    private Transform primaryWeapon; // get dynamically
    private Transform secondaryWeapon; // get dynamically

    private HandleMovement movementInput;

    public bool allowRotate = true;

    public Vector3 defaultPos;
    public Vector3 defaultRotation;

    public float bobAmount;
    public float bobSpeed;
    private float bobTimer;
    private float verticalVelocity;
    private bool sprintButton;

    public override void OnNetworkSpawn()
    {
        movementInput = GetComponent<HandleMovement>();
        InitializeDefaults();
        SetupWeapons();
    }

    private void InitializeDefaults()
    {
        defaultPos = Vector3.zero;
        defaultRotation = new Vector3(0, 180, 0);
        bobAmount = 0.3f;
        bobSpeed = 10f;
    }

    private void SetupWeapons()
    {
        primaryWeapon = transform.GetChild(0).GetChild(1).GetChild(0);
        secondaryWeapon = transform.GetChild(0).GetChild(1).GetChild(1);
    }


    Vector3 GetLeanRotation(float leanAmount) {
        // get move direction
        Vector3 moveDir = movementInput.horizontalMoveDirection;

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
        if (!movementInput.IsOnGround)
        {
            verticalVelocity = movementInput.currentPlayerVelocity.y;
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
        if (!movementInput.IsThrusting) return;

        float pingPongTimer = Mathf.PingPong(Time.deltaTime * 5f, 1);
        Quaternion camTarget = Quaternion.Euler(
            defaultRotation.x,
            defaultRotation.y + Random.Range(-5f, 5f),
            defaultRotation.z
        );
        playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, camTarget, pingPongTimer);

        Quaternion cockpitTarget = Quaternion.Euler(
            defaultRotation.x + 10f,
            defaultRotation.y,
            defaultRotation.z
        );
        playerCockpitCamera.transform.localRotation = Quaternion.Slerp(playerCockpitCamera.transform.localRotation, cockpitTarget, Time.deltaTime);
    }


    private void HandleWalkingBobbing()
    {
        if (movementInput.IsOnGround && movementInput.IsMovingHorizontally && !movementInput.IsDashing && !sprintButton)
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
        if (!sprintButton || movementInput.IsDashing) return;

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
        if (movementInput.IsDashing)
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

    void Update()
    {
        if (!IsOwner) return;
        sprintButton = Input.GetKey(KeyCode.LeftShift);
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