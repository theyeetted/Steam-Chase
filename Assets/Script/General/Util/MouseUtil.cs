using UnityEngine;

public static class MouseUtil
{
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
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Plane dragPlane = new(camera.transform.forward, new Vector3(0, 0, zValue));
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(ray, out float distance))
            return false;

        position = ray.GetPoint(distance);
        return true;
    }
}
