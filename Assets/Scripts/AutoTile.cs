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
            rend.material.mainTextureScale = new Vector2(40, 1);
        }
        else
        {
            
            rend.material.mainTextureScale = new Vector2(scale.x, scale.z);
        }
        
    }
}