using UnityEngine;

public static class TransformExtensions
{
    public static void LookAtVertical(this Transform tr, Vector3 target)
    {
        Vector3 direction = target - tr.position;

        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            tr.forward = direction.normalized;
        }
    }
}
