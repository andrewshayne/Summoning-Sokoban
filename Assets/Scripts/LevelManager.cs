using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

using Assets.Scripts;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public Vector2Int HubWorldPlayerPosition = new Vector2Int(0, 0);
    public HashSet<string> CompletedLevels = new HashSet<string>();

    private ScreenWipe screenWipe;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
        screenWipe = FindObjectOfType<ScreenWipe>();
    }

    void Update()
    {
        // if event fired from the grid manager???
        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    StartCoroutine(LoadNextLevel());
        //}
    }

    public void LoadLevel(string name)
    {
        StartCoroutine(LoadNextLevel(name));
    }

    private IEnumerator LoadNextLevel(string name)
    {
        screenWipe.ToggleWipe(true);
        while (!screenWipe.isDone)
        {
            yield return null;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(name);
        while (!operation.isDone)
        {
            yield return null;
        }

        screenWipe.ToggleWipe(false);
    }
}
