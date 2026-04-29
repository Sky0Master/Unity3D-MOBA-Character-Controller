using UnityEngine;

public class ClickVisual : MonoBehaviour
{
    [SerializeField] LayerMask clickPanelLayerMask;
    [SerializeField] private GameObject indicatorPrefab;

    // Update is called once per frame
    void Update()
    {
         if (Input.GetMouseButtonDown(1))
        {
            if (GameUtils.TryGetMouseWorldPosition(out Vector3 targetPos, clickPanelLayerMask))
            {
                transform.position = targetPos + new Vector3(0, 0.01f, 0);
                var go = GameObject.Instantiate(indicatorPrefab, transform.position, Quaternion.identity);
                go.GetComponent<Animator>().Play("ClickMove");
                Destroy(go,1.5f);
            }
        }
    }
}
