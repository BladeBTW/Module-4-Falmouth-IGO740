using UnityEngine;

public class ChickenLODController : MonoBehaviour
{
    [Header("Distance Settings")]
    public float highDetailDistance = 10f;
    public float randomDistanceOffset = 2f;

    [Header("Targets")]
    public Transform distanceTarget;
    public Transform positionSource;
    public string playerTag = "Player";
    public bool useCameraForDistance = true;

    [Header("References")]
    public GameObject highDetailObject;
    public Animator animator;

    [Header("Billboard Root")]
    public GameObject billboardRoot;
    public bool keepBillboardY = true;
    public float billboardY = 0f;
    public Vector3 billboardPositionOffset = Vector3.zero;

    [Header("Billboard Direction Objects")]
    public GameObject billboardTopLeft;
    public GameObject billboardTopRight;
    public GameObject billboardBottomLeft;
    public GameObject billboardBottomRight;

    [Header("Rotation Calibration")]
    public float yawOffset = 90f;

    [Header("Billboard Facing")]
    public bool faceCamera = true;
    public float billboardYRotationOffset = 180f;

    [Header("Performance")]
    public float checkInterval = 0.3f;
    public float directionCheckInterval = 0.15f;

    private Camera mainCam;
    private float lodTimer;
    private float directionTimer;
    private float actualHighDetailDistance;
    private GameObject activeBillboard;

    private enum LODState
    {
        High,
        Billboard
    }

    private LODState currentState = LODState.High;

    private void Awake()
    {
        mainCam = Camera.main;

        actualHighDetailDistance = highDetailDistance + Random.Range(-randomDistanceOffset, randomDistanceOffset);

        if (positionSource == null)
        {
            Transform normalChicken = transform.Find("NPC_NormalChicken");
            if (normalChicken != null)
                positionSource = normalChicken;
            else
                positionSource = transform;
        }

        if (billboardRoot != null)
            billboardY = billboardRoot.transform.position.y;

        SetAllBillboards(false);

        if (billboardRoot != null)
            billboardRoot.SetActive(false);

        if (highDetailObject != null)
            highDetailObject.SetActive(true);

        if (animator != null)
            animator.enabled = true;
    }

    private void Start()
    {
        if (distanceTarget == null)
        {
            if (useCameraForDistance && Camera.main != null)
            {
                distanceTarget = Camera.main.transform;
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                    distanceTarget = player.transform;
                else if (Camera.main != null)
                    distanceTarget = Camera.main.transform;
            }
        }

        UpdateLOD(true);
    }

    private void Update()
    {
        FollowPositionOnly();

        lodTimer += Time.deltaTime;

        if (lodTimer >= checkInterval)
        {
            lodTimer = 0f;
            UpdateLOD(false);
        }

        if (currentState == LODState.Billboard)
        {
            directionTimer += Time.deltaTime;

            if (directionTimer >= directionCheckInterval)
            {
                directionTimer = 0f;
                UpdateBillboardImage();
            }
        }
    }

    private void LateUpdate()
    {
        FollowPositionOnly();

        if (currentState == LODState.Billboard)
            FaceBillboardRootToCamera();
    }

    private void FollowPositionOnly()
    {
        if (billboardRoot == null || positionSource == null)
            return;

        Vector3 source = positionSource.position;

        billboardRoot.transform.position = new Vector3(
            source.x + billboardPositionOffset.x,
            keepBillboardY ? billboardY + billboardPositionOffset.y : source.y + billboardPositionOffset.y,
            source.z + billboardPositionOffset.z
        );
    }

    private void UpdateLOD(bool force)
    {
        if (distanceTarget == null || positionSource == null)
            return;

        float distance = Vector3.Distance(positionSource.position, distanceTarget.position);

        if (distance <= actualHighDetailDistance)
            ApplyLOD(LODState.High, force);
        else
            ApplyLOD(LODState.Billboard, force);
    }

    private void ApplyLOD(LODState newState, bool force)
    {
        if (!force && currentState == newState)
            return;

        currentState = newState;

        bool useHigh = newState == LODState.High;
        bool useBillboard = newState == LODState.Billboard;

        if (highDetailObject != null)
            highDetailObject.SetActive(useHigh);

        if (animator != null)
            animator.enabled = useHigh;

        if (billboardRoot != null)
            billboardRoot.SetActive(useBillboard);

        if (useBillboard)
        {
            FollowPositionOnly();
            UpdateBillboardImage();
            FaceBillboardRootToCamera();
        }
        else
        {
            SetAllBillboards(false);
            activeBillboard = null;
        }
    }

    private void UpdateBillboardImage()
    {
        GameObject next = GetBillboardForChickenRotation();

        if (next == null)
            next = GetFallbackBillboard();

        if (next == activeBillboard)
            return;

        SetAllBillboards(false);

        activeBillboard = next;

        if (activeBillboard != null)
        {
            activeBillboard.SetActive(true);
            activeBillboard.transform.localRotation = Quaternion.identity;
        }
    }

    private GameObject GetBillboardForChickenRotation()
    {
        Transform rotationSource = positionSource != null ? positionSource : transform;
        float yaw = NormalizeAngle(rotationSource.eulerAngles.y + yawOffset);

        if (yaw >= 315f || yaw < 45f)
            return billboardBottomRight;

        if (yaw >= 45f && yaw < 135f)
            return billboardTopRight;

        if (yaw >= 135f && yaw < 225f)
            return billboardTopLeft;

        return billboardBottomLeft;
    }

    private void FaceBillboardRootToCamera()
    {
        if (!faceCamera || billboardRoot == null || mainCam == null)
            return;

        Vector3 toCamera = mainCam.transform.position - billboardRoot.transform.position;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        billboardRoot.transform.rotation = lookRotation * Quaternion.Euler(0f, billboardYRotationOffset, 0f);
    }

    private void SetAllBillboards(bool active)
    {
        if (billboardTopLeft != null) billboardTopLeft.SetActive(active);
        if (billboardTopRight != null) billboardTopRight.SetActive(active);
        if (billboardBottomLeft != null) billboardBottomLeft.SetActive(active);
        if (billboardBottomRight != null) billboardBottomRight.SetActive(active);
    }

    private GameObject GetFallbackBillboard()
    {
        if (billboardBottomRight != null) return billboardBottomRight;
        if (billboardTopRight != null) return billboardTopRight;
        if (billboardTopLeft != null) return billboardTopLeft;
        if (billboardBottomLeft != null) return billboardBottomLeft;

        return null;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}