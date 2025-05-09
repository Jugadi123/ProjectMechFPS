
using System.Collections;
using UnityEngine;
using Unity.Netcode;
public class CameraShakes : NetworkBehaviour
{
    [SerializeField] Camera playerCamera; // Player's main camera. NOT cockpit camera. That camera is only for the ui and cockpit layer.
    [SerializeField] Camera playerCockpitCamera;

    [SerializeField] GameObject playerCockpitMesh;

    [SerializeField] AudioListener playerCameraAudioListener;

    [SerializeField] Transform primaryWeapon; // get dynamically
    [SerializeField] Transform secondaryWeapon; // get dynamically

    private handleMovement handle_movement;

    public bool allowRotate = true;

    public Vector3 defaultPos;
    public Vector3 defaultRotation;

    public float bobAmount;
    public float bobSpeed;
    private float bobTimer;
    private float verticalVelocity;

    public override void OnNetworkSpawn()
    {

        if (IsOwner)
        {
            // Enable cockpit visuals + camera for local player
            playerCockpitMesh.SetActive(true);
            playerCamera.enabled = true;

            // Make sure the camera only sees local cockpit layer
            playerCamera.cullingMask = ~LayerMask.GetMask("UI", "CockpitView", "SpawnPoint Mask");
            playerCockpitCamera.cullingMask = LayerMask.GetMask("UI", "CockpitView");

        }
        else
        {
            playerCockpitMesh.SetActive(false);
            playerCamera.enabled = false;
            playerCockpitCamera.enabled = false;
            playerCameraAudioListener = playerCamera.GetComponent<AudioListener>();
            playerCameraAudioListener.enabled = false;
            return;
        }


        handle_movement = GetComponent<handleMovement>();
        defaultPos = Vector3.zero; // relative to upper body
        defaultRotation = new Vector3(0, 180, 0);
        bobAmount = 0.3f;
        bobSpeed = 10f;
        primaryWeapon = transform.GetChild(0).GetChild(1).GetChild(0);
        secondaryWeapon = transform.GetChild(0).GetChild(1).GetChild(1);
    }

    Vector3 GetLeanRotation(float leanAmount) {
        // get move direction
        Vector3 moveDir = handle_movement.horizontalMoveDirection;

        // convert it to local space
        Vector3 localMoveDir = transform.InverseTransformDirection(moveDir);

        // calculate tilt angles
        float tiltX = localMoveDir.z * leanAmount; // forward/back = pitch
        float tiltZ = localMoveDir.x * leanAmount; // left/right = roll

        // return target angles
        return new Vector3(tiltX, 180, tiltZ);
    }

    void Update()
    {

        if (!IsOwner) return;

        // cockpit rotate while trusting lift
        if (handle_movement.IsThrusting) {

            float pingPongTimer = Mathf.PingPong(Time.deltaTime * 5f, 1);

            playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, Quaternion.Euler(defaultRotation.x, defaultRotation.y + Random.Range(-5f,5f), defaultRotation.z), pingPongTimer);

            playerCockpitCamera.transform.localRotation = Quaternion.Slerp(playerCockpitCamera.transform.localRotation, Quaternion.Euler(defaultRotation.x + 10f, defaultRotation.y, defaultRotation.z), Time.deltaTime);
            
        }


        // screen shake on landing impact
        if (!handle_movement.IsOnGround)
        {
            verticalVelocity = handle_movement.currentPlayerVelocity.y;
        } 
        else {
            // Just landed — use the last recorded vertical velocity
            float impactForce = Mathf.Abs(verticalVelocity);

            if (impactForce > 10f) // ignore baby steps
            {
                float strength = Mathf.Clamp01(impactForce / 20f); // normalize impact
                Vector3 targetAnglesCamera = new Vector3(strength * 0.2f, defaultRotation.y + strength * 0.2f, strength * 0.2f);
                StartCoroutine(ShakeObject(strength * 0.1f, 0.2f, playerCamera.transform));
                StartCoroutine(ShakeObject(strength * 0.1f, 0.2f, playerCockpitCamera.transform));
            }
            // reset after impact
            verticalVelocity = 0f;
        }
        
        if (handle_movement.IsOnGround && handle_movement.IsMovingHorizontally && !handle_movement.IsDashing && !handle_movement.sprintButton)
        {
            // camera bobs (walking)
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
            
            Quaternion bobAngles = Quaternion.Euler(bobOffset, defaultRotation.y, defaultRotation.z);

            playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
            playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
            
            
        }
        else {
            playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * bobSpeed);
            playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * bobSpeed);
        }

        // camera leans when we're boosting
        if (handle_movement.sprintButton && !handle_movement.IsDashing) {
            // lean while dashing
            Vector3 targetAngles = GetLeanRotation(2f);
            playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, Quaternion.Euler(targetAngles), Time.deltaTime * 5f);
            // playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, Quaternion.Euler(targetAngles), Time.deltaTime * 5f);

            bobTimer += Time.deltaTime * bobSpeed * 1.2f;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount * 1.2f;
            
            Quaternion bobAngles = Quaternion.Euler(bobOffset, targetAngles.y, targetAngles.z);

            playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
            playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);


        }
        else {
            playerCamera.transform.localRotation = Quaternion.Lerp(playerCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * 5f);
            // playerCockpitCamera.transform.localRotation = Quaternion.Lerp(playerCockpitCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * 5f);
        }


        // if we are dashing
        if (handle_movement.IsDashing)
        {
            if (allowRotate)
            {
                allowRotate = false;
                float duration = 0.4f;
                Vector3 originalAnglesCamera = defaultRotation;
                Vector3 targetAnglesCamera = GetLeanRotation(2f);
                Vector3 originalAnglesCockpit = defaultRotation;
                Vector3 targetAnglesCockpit = GetLeanRotation(3f);

                StartCoroutine(RotateTransform(originalAnglesCockpit, -targetAnglesCockpit, duration, playerCockpitCamera.transform));
                StartCoroutine(RotateTransform(originalAnglesCamera, targetAnglesCamera, duration, playerCamera.transform));
            }
        }
        else {
            allowRotate = true;
        }
    }


    private IEnumerator RotateTransform(Vector3 originalAngles, Vector3 targetAngles, float duration, Transform objectTransform) {
        if (objectTransform == null) yield break; 

        float elapsed = 0f;

        while  (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            objectTransform.localRotation = Quaternion.Slerp(objectTransform.localRotation, Quaternion.Euler(targetAngles), elapsed / duration);
            yield return null; // wait for next frame
        }

        objectTransform.localRotation = Quaternion.Slerp(objectTransform.localRotation, Quaternion.Euler(originalAngles), Time.deltaTime * 5f);
    }


    private IEnumerator RotateTransformWithDuration(Vector3 originalAngles, Vector3 targetAngles, float duration, Transform objectTransform) {
        if (objectTransform == null) yield break; 

        float elapsed = 0f;

        float halfDuration = duration / 2f;

        while  (elapsed < duration)
        {
            float t; // Dynamic based on elapsed time

            if (elapsed < halfDuration)
            {
                // Forward: original → target
                t = elapsed / halfDuration;
                objectTransform.localRotation = Quaternion.Slerp(objectTransform.localRotation, Quaternion.Euler(targetAngles), t);
            }
            else
            {
                // Backward: target → original
                t = (elapsed - halfDuration) / halfDuration;
                objectTransform.localRotation = Quaternion.Slerp(objectTransform.localRotation, Quaternion.Euler(originalAngles), t);
            }

            elapsed += Time.deltaTime;
            
            yield return null; // wait for next frame
        }
    }


    private IEnumerator ShakeObject(float strength, float duration, Transform objectToShake) {
        Vector3 originalPos = objectToShake.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 offset = Random.insideUnitSphere * strength;
            objectToShake.transform.localPosition = originalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        objectToShake.transform.localPosition = originalPos;
    }
}