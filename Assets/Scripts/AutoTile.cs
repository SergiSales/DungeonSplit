using UnityEngine;

public class AutoTile : MonoBehaviour
{
    public Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        Vector3 scale = transform.localScale;
        if(this.gameObject.name.Contains("Wall"))
        {
            if(scale.z>scale.x) rend.material.mainTextureScale = new Vector2(scale.z / 8, scale.y / 4);
            else rend.material.mainTextureScale = new Vector2(scale.x / 8, scale.y / 4);
        }
        else
        {
            
            rend.material.mainTextureScale = new Vector2(scale.x / 4, scale.z / 4);
        }
        
    }
}