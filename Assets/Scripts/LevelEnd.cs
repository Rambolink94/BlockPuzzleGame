using UnityEngine;

public class LevelEnd : MonoBehaviour
{
    [SerializeField] private string levelToLoad = "";
    
    private void OnTriggerEnter(Collider other)
    {
        LayerMask playerLayer = LayerMask.GetMask("Player");
        if (playerLayer == (playerLayer | (1 << other.gameObject.layer)))
        {
            // TODO: Change this to load next level
            GameManager.Instance.LoadLevel(levelToLoad);
        }
    }
}
