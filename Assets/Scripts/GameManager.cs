using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public DataGrid dataGrid;
    public Light[] editorLights;

    protected override void Awake()
    {
        base.Awake();
        
        foreach (Light editorLight in editorLights)
        {
            editorLight.gameObject.SetActive(false);
        }

        dataGrid = GetComponent<DataGrid>();
        dataGrid.GenerateRailGrid();
        dataGrid.GenerateBlockDictionary();
    }

    private void Start()
    {
        dataGrid.PropegatePowerSources();
    }

    public void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void LoadLevel(string sceneToLoad)
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
