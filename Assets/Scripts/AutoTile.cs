using UnityEngine;

public class AutoTile : MonoBehaviour
{
    public Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        Vector3 scale = transform.localScale;
        rend.material.mainTextureScale = new Vector2(scale.z, scale.x);
    }
}