using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{

    [SerializeField] private GameObject pause;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pause.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void ExitGame()
    {
        Debug.Log("exit");
        SceneManager.LoadScene("MainMenu");
    }

    public void restart()
    {
        SceneManager.LoadScene("level");
    }


    public void Resume()
    {
            pause.SetActive(false);
            Time.timeScale = 1f;
    }

}
