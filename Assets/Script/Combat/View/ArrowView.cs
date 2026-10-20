using UnityEngine;

public class ArrowView : MonoBehaviour
{
    [SerializeField] private GameObject arrowHead;
    [SerializeField] private LineRenderer lineRenderer;

    private Vector3 startPosition;

    private void Update()
    {
        if (arrowHead == null || lineRenderer == null)
            return;
        if (!MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 endPosition))
        {
            ManualTargetSystem.Instance?.CancelTargeting();
            return;
        }

        Vector3 direction = (endPosition - startPosition).normalized;
        lineRenderer.SetPosition(1, endPosition - direction * 0.5f);
        arrowHead.transform.position = endPosition;
        if (direction.sqrMagnitude > 0f)
            arrowHead.transform.right = direction;
    }

    public bool SetupArrow(Vector3 startPosition)
    {
        if (arrowHead == null || lineRenderer == null ||
            !MouseUtil.TryGetMousePositionInWorldSpace(out Vector3 endPosition))
            return false;

        this.startPosition = startPosition;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        arrowHead.transform.position = endPosition;
        Vector3 direction = (endPosition - startPosition).normalized;
        if (direction.sqrMagnitude > 0f)
            arrowHead.transform.right = direction;
        return true;
    }
}
