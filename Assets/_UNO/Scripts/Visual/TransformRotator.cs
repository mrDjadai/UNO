using UnityEngine;

public class TransformRotator : MonoBehaviour
{
    [SerializeField] private Vector3 axis;
    [SerializeField] private float speed;
    private Transform tr;

    private void Awake()
    {
        tr = transform;
        axis *= speed;
    }

    private void Update()
    {
        tr.Rotate(axis * Time.deltaTime, Space.Self);    
    }
}
