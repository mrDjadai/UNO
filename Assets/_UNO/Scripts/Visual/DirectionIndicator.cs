using UnityEngine;
using UnityEngine.Rendering.Universal;
using Mirror;

public class DirectionIndicator : NetworkBehaviour
{
    [SerializeField] private DecalProjector projector;
    [SerializeField] private Material forwardMaterial;
    [SerializeField] private Material backwardMaterial;
    [SerializeField] private float rotationSpeed = 10f;
    private Transform tr;

    private void Awake()
    {
        tr = transform;
    }

    private void Indicate(bool v)
    {
        projector.material = v ? forwardMaterial : backwardMaterial;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        TurnManager.Instance.OnDirectionChange += Indicate;
        Indicate(TurnManager.Instance.Direction);
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance == null) return;

        TurnManager.Instance.OnDirectionChange -= Indicate;
    }

    private void Update()
    {
        if (TurnManager.Instance.Direction)
        {
            tr.Rotate(Vector3.down * rotationSpeed * Time.deltaTime);
        }
        else
        {
            tr.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }
}
