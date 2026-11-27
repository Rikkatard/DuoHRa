using UnityEngine;
using UnityEngine.SceneManagement; 
public class MainMenu : MonoBehaviour
{
    [Header("UI Panely")]
    public GameObject mainButtonsPanel; 
    public GameObject settingsPanel;    

    public void PlayGame()
    {
        //nacte roller
        SceneManager.LoadScene(1);
    }

    // Zavolá se po kliknutí na SETTINGS
    public void OpenSettings()
    {
        mainButtonsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }


    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainButtonsPanel.SetActive(true);
    }

    // Zavolá se po kliknutí na QUIT
    public void QuitGame()
    {
        

        Debug.Log("Hra se ukončuje..."); // V editoru Unity se hra nevypne, jen vypíše zprávu

        Application.Quit();             
    }
}