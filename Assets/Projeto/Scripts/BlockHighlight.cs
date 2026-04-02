using UnityEngine;

public class BlockHighlight : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Transform highlight;
    [SerializeField] private float reach = 6f;
    [SerializeField] private LayerMask layerMap;

    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, reach,layerMap))
        {
            Vector3 pos = hit.point - hit.normal * 0.01f;

            int x = Mathf.FloorToInt(pos.x);
            int y = Mathf.FloorToInt(pos.y);
            int z = Mathf.FloorToInt(pos.z);

            highlight.gameObject.SetActive(true);
            highlight.position = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
        }
        else
        {
            highlight.gameObject.SetActive(false);
        }
    }
}