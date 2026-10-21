using UnityEngine;

public static class MouseUtil
{
    private static Camera cachedCamera = Camera.main;
    private static bool hasWarnedMissingCamera;

    public static Vector3 GetMousePositionInWorldSpace(float zValue = 0f)
    {
        return TryGetMousePositionInWorldSpace(zValue, out Vector3 position)
            ? position
            : Vector3.zero;
    }

    public static bool TryGetMousePositionInWorldSpace(out Vector3 position)
    {
        return TryGetMousePositionInWorldSpace(0f, out position);
    }

    public static bool TryGetMousePositionInWorldSpace(float zValue, out Vector3 position)
    {
        position = Vector3.zero;

        // Refresh cached camera reference if it's missing or destroyed (Friend's safety feature)
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null)
        {
            if (!hasWarnedMissingCamera)
            {
                Debug.LogWarning("[MouseUtil] Main Camera not found. Check that an active camera is tagged MainCamera.");
                hasWarnedMissingCamera = true;
            }
            return false;
        }

        hasWarnedMissingCamera = false;

        Plane dragPlane = new(cachedCamera.transform.forward, new Vector3(0, 0, zValue));
        Ray ray = camera.ScreenPointToRay(Input.mousePosition); // Or cachedCamera.ScreenPointToRay
        
        // Using your concise plane raycast logic with fallback
        if (!dragPlane.Raycast(ray, out float distance))
        {
            Debug.Log("[MouseUtil] Mouse ray did not intersect the drag plane.");
            return false;
        }

        position = ray.GetPoint(distance);
        return true;
    }
}