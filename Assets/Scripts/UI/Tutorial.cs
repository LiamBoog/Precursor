using UnityEngine;

public class Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject ui;
    
    private void Start()
    {
        ui.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D _)
    {
        ui.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D _)
    {
        ui.SetActive(false);
    }
}
