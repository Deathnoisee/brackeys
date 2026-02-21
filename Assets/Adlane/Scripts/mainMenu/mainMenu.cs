using Unity.VisualScripting;
using UnityEngine;

public class mainMenu : MonoBehaviour
{
    [SerializeField] private GameObject[] optionMenu;
    [SerializeField] private GameObject[] mainMenuUI;
    [SerializeField] private GameObject[] soundMenu;
    [SerializeField] private GameObject[] confirmQuitMenu;
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void Start()
    {
        foreach (GameObject menu in optionMenu)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in soundMenu)
        {
            menu.SetActive(false);
        }
    }
    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
    public void ExitGame()
    {
        Application.Quit();
    }
    public void optionsMenu()
    {
        foreach (GameObject menu in optionMenu)
        {
            menu.SetActive(true);
        }
        foreach (GameObject menu in mainMenuUI)
        {
            menu.SetActive(false);
        }
    }
    public void backToMainMenu()
    {
        foreach (GameObject menu in optionMenu)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in mainMenuUI)
        {
            menu.SetActive(true);
        }
        foreach (GameObject menu in soundMenu)
        {
            menu.SetActive(false);
        }
    }
    public void soundsMenu()
    {
        foreach (GameObject menu in optionMenu)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in mainMenuUI)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in soundMenu)
        {
            menu.SetActive(true);
        }
    }
    public void backToOptionsFromSound()
    {
        foreach (GameObject menu in optionMenu)
        {
            menu.SetActive(true);
        }
        foreach (GameObject menu in mainMenuUI)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in soundMenu)
        {
            menu.SetActive(false);
        }
    }
    public void confirmQuit()
    {
        foreach (GameObject menu in optionMenu)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in mainMenuUI)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in soundMenu)
        {
            menu.SetActive(false);
        }
        foreach (GameObject menu in confirmQuitMenu)
        {
            menu.SetActive(true);
        }
    }
    public void changeVolume(float volume)
    {
        soundManager.Instance.changeVolume(volume);
    }
}
