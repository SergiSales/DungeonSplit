using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0f, 15f, 0f);
    public float smoothSpeed = 5f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = Vector3.Lerp(
            transform.position,
            target.position + offset,
            smoothSpeed * Time.deltaTime
        );
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position + offset;
    }
}
