using UnityEngine;

public class ClickVisual : MonoBehaviour
{
    [SerializeField] LayerMask clickPanelLayerMask;
    [SerializeField] ParticleSystem clickFx;

    // Update is called once per frame
    void Update()
    {
         if (Input.GetMouseButtonDown(1))
        {
            if (GameUtils.TryGetMouseWorldPosition(out Vector3 targetPos, clickPanelLayerMask))
            {
                transform.position = targetPos;
                clickFx?.Play();
            }
        }
    }
}
