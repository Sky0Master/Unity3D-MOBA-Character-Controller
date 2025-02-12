using UnityEngine;

public static class GameUtils
{
    public static bool TryGetMouseWorldPosition(out Vector3 result, LayerMask clickLayerMask)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000, clickLayerMask))
        {
            result = hit.point;
            Debug.Log("hit point: " + hit.point);
            return true;
        }
        result = Vector3.zero;
        return false;
    }
}
