using UnityEngine;

public class CameraFollowScript : MonoBehaviour
{
    [Tooltip("Higher = tighter follow. ~6 feels like the old 0.1 at 60fps")]
    [SerializeField] private float followSpeed = 6f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    private bool snapped;

    private void OnEnable()
    {
        snapped = false; // snap again whenever the camera is (re)enabled
    }

    private void LateUpdate()
    {
        if (PlayerController.Instance == null) return;

        Vector3 target = PlayerController.Instance.transform.position + offset;

        if (!snapped)
        {
            transform.position = target;
            snapped = true;
            return;
        }

        float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, target, t);
    }
}