using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform player;

    // Update is called once per frame
    void Update()
    {
        Debug.Log(player);
        transform.position = player.transform.position;
    }
}
