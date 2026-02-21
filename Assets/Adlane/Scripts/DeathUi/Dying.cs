using UnityEngine;

public class Dying: MonoBehaviour
{
    [SerializeField] private DeathUi deathUi;
    public void Die()
    {
        deathUi.ShowDeathScreen();
        Time.timeScale = 0f;
    }
    //
}
