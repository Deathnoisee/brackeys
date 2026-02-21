using Unity.VisualScripting;
using UnityEngine;

public class DeathUi : MonoBehaviour
{
    [SerializeField] private GameObject deathScreen;

    void Start()
    {
        deathScreen.SetActive(false);
    }
    public void ShowDeathScreen()
    {
        deathScreen.SetActive(true);
    }
    public void HideDeathScreen()
    {
        deathScreen.SetActive(false);
    }
}
