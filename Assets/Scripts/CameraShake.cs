
using System.Collections;
using UnityEngine;
using Unity.Netcode;
public class CameraShake : NetworkBehaviour
{
    [SerializeField] Camera mainCamera; // main camera. NOT cockpit camera. That camera is only for the ui and cockpit layer.

    [SerializeField] Camera cockpitCamera;

    private handle_movement handle_movement;

    private Coroutine currentRotationRoutine;

    public bool allowRotate = true;

    public Vector3 defaultPos;

    public Vector3 defaultRotation;

    public float bobAmount;

    public float bobSpeed;

    private float bobTimer;

    private float verticalVelocity;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) {
            mainCamera.enabled = false;
            cockpitCamera.enabled = false;
            return;
        }

        handle_movement = GetComponent<handle_movement>();
        defaultPos = Vector3.zero; // relative to upper body
        defaultRotation = new Vector3(0, 180, 0);
        bobAmount = 0.3f;
        bobSpeed = 10f;
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

        if (!IsOwner) {
            return;
        }

        // cockpit rotate while trusting

        if (handle_movement.IsThrusting) {

            float pingPongTimer = Mathf.PingPong(Time.deltaTime * 5f, 1);

            mainCamera.transform.localRotation = Quaternion.Slerp(mainCamera.transform.localRotation, Quaternion.Euler(defaultRotation.x + 5f, defaultRotation.y, defaultRotation.z), Time.deltaTime * 5f);

          mainCamera.transform.localRotation = Quaternion.Slerp(mainCamera.transform.localRotation, Quaternion.Euler(defaultRotation.x, defaultRotation.y + Random.Range(-5f,5f), defaultRotation.z), pingPongTimer);
            
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
                StartCoroutine(ShakeCamera(strength * 0.1f, 0.2f, mainCamera));
                StartCoroutine(ShakeCamera(strength * 0.1f, 0.2f, cockpitCamera));
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

            cockpitCamera.transform.localRotation = Quaternion.Lerp(cockpitCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
            mainCamera.transform.localRotation = Quaternion.Lerp(mainCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
            
        }
        else {
            cockpitCamera.transform.localRotation = Quaternion.Lerp(cockpitCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * bobSpeed);
            mainCamera.transform.localRotation = Quaternion.Lerp(mainCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * bobSpeed);
        }

        // camera leans when we're boosting
        if (handle_movement.sprintButton && !handle_movement.IsDashing) {
            // lean while dashing
            Vector3 targetAngles = GetLeanRotation(2f);
            mainCamera.transform.localRotation = Quaternion.Lerp(mainCamera.transform.localRotation, Quaternion.Euler(targetAngles), Time.deltaTime * 5f);
            // cockpitCamera.transform.localRotation = Quaternion.Lerp(cockpitCamera.transform.localRotation, Quaternion.Euler(targetAngles), Time.deltaTime * 5f);

            bobTimer += Time.deltaTime * bobSpeed * 1.2f;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount * 1.2f;
            
            Quaternion bobAngles = Quaternion.Euler(bobOffset, targetAngles.y, targetAngles.z);

            cockpitCamera.transform.localRotation = Quaternion.Lerp(cockpitCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);
            mainCamera.transform.localRotation = Quaternion.Lerp(mainCamera.transform.localRotation, bobAngles, Time.deltaTime * bobSpeed);


        }
        else {
            mainCamera.transform.localRotation = Quaternion.Lerp(mainCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * 5f);
            // cockpitCamera.transform.localRotation = Quaternion.Lerp(cockpitCamera.transform.localRotation, Quaternion.Euler(defaultRotation), Time.deltaTime * 5f);
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

                StartCoroutine(RotateCamera(originalAnglesCockpit, -targetAnglesCockpit, duration, cockpitCamera.transform));
                StartCoroutine(RotateCamera(originalAnglesCamera, targetAnglesCamera, duration, mainCamera.transform));
            }
        }
        else {
            allowRotate = true;
        }
    }

    // must be called inside the update loop
    public IEnumerator RotateCamera(Vector3 originalAngles, Vector3 targetAngles, float duration, Transform camera) 
    {
        if (camera == null) yield break; 

        float elapsed = 0f;

        // float halfDuration = duration / 2f;

        while  (elapsed < duration)
        {


            // float t; // Dynamic based on elapsed time

            // if (elapsed < halfDuration)
            // {
            //     // Forward: original → target
            //     t = elapsed / halfDuration;
            //     camera.localRotation = Quaternion.Slerp(camera.localRotation, Quaternion.Euler(targetAngles), t);
            // }
            // else
            // {
            //     // Backward: target → original
            //     t = (elapsed - halfDuration) / halfDuration;
            //     camera.localRotation = Quaternion.Slerp(camera.localRotation, Quaternion.Euler(originalAngles), t);
            // }

            elapsed += Time.deltaTime;
            camera.localRotation = Quaternion.Slerp(camera.localRotation, Quaternion.Euler(targetAngles), elapsed / duration);

            yield return null; // wait for next frame
        }

        camera.localRotation = Quaternion.Slerp(camera.localRotation, Quaternion.Euler(originalAngles), Time.deltaTime * 5f);
        currentRotationRoutine = null; // clear reference
    }

    public IEnumerator ShakeCamera(float strength, float duration, Camera cameraToShake)
    {

        Vector3 originalPos = cameraToShake.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 offset = Random.insideUnitSphere * strength;
            cameraToShake.transform.localPosition = originalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraToShake.transform.localPosition = originalPos;
    }
}