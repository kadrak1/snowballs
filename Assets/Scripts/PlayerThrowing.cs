
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrowing : MonoBehaviour
{
    public GameObject snowballPrefab;
    public Transform firePoint;
    public float fireForce = 20f;

    public UIManager uiManager;
    public Camera playerCamera;
    public Animator animator;
    public LayerMask snowLayerMask;
    public float snowCheckDistance = 1.5f;
    public bool requireSnowToGather = true;
    public Animator fpAnimator;
    public GameObject heldSnowballVisual;

	private enum HandState { Empty, SnowInHands, SnowballReady }
	private HandState handState = HandState.Empty;
	public int moldClicksRequired = 3;
	private int currentMoldClicks = 0;

    void Start()
    {
        if (uiManager != null)
        {
            uiManager.UpdateHandsStatus(GetHandsStatusString(), currentMoldClicks, moldClicksRequired);
        }
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (handState == HandState.SnowballReady)
            {
                TriggerIfExists("Throw");
                TriggerIfExistsFP("Throw");
                Fire();
                handState = HandState.Empty;
                currentMoldClicks = 0;
                if (uiManager != null)
                {
                    uiManager.UpdateHandsStatus(GetHandsStatusString(), currentMoldClicks, moldClicksRequired);
                }
                UpdateHeldVisual();
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (handState == HandState.Empty)
            {
                if (!requireSnowToGather || IsSnowUnderPlayer())
                {
                    handState = HandState.SnowInHands;
                    currentMoldClicks = 0;
                    TriggerIfExists("Gather");
                    TriggerIfExistsFP("Gather");
                }
            }
            else if (handState == HandState.SnowInHands)
            {
                currentMoldClicks++;
                TriggerIfExists("Mold");
                TriggerIfExistsFP("Mold");
                if (currentMoldClicks >= moldClicksRequired)
                {
                    currentMoldClicks = moldClicksRequired;
                    handState = HandState.SnowballReady;
                }
            }

            if (uiManager != null)
            {
                uiManager.UpdateHandsStatus(GetHandsStatusString(), currentMoldClicks, moldClicksRequired);
            }
            UpdateHeldVisual();
        }
    }

    void Fire()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(75);
        }

        Vector3 direction = (targetPoint - firePoint.position).normalized;
        GameObject snowball = Instantiate(snowballPrefab, firePoint.position, Quaternion.LookRotation(direction));
        snowball.layer = gameObject.layer;
        Rigidbody rb = snowball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * fireForce, ForceMode.Impulse);
        }
    }

	string GetHandsStatusString()
	{
		switch (handState)
		{
			case HandState.Empty:
				return "Empty";
			case HandState.SnowInHands:
				return "Snow";
			case HandState.SnowballReady:
				return "Snowball";
		}
		return "";
	}

    bool IsSnowUnderPlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, snowCheckDistance, snowLayerMask))
        {
            return true;
        }
        return false;
    }

    void TriggerIfExists(string triggerName)
    {
        if (animator == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }

    void TriggerIfExistsFP(string triggerName)
    {
        if (fpAnimator == null) return;
        foreach (var p in fpAnimator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            {
                fpAnimator.SetTrigger(triggerName);
                return;
            }
        }
    }

    void UpdateHeldVisual()
    {
        if (heldSnowballVisual == null) return;
        bool show = handState == HandState.SnowballReady;
        if (heldSnowballVisual.activeSelf != show)
        {
            heldSnowballVisual.SetActive(show);
        }
    }
}
