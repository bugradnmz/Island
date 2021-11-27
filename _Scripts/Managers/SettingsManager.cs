using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SettingsManager : MonoBehaviour
{
    private SettingsVariables settingsVariables;

    private string language;

    #region function & methods
    private void SaveSettings()
    {
        settingsVariables = new SettingsVariables
        {
            savedLanguage = language,
        };

        string jsonData = JsonUtility.ToJson(settingsVariables);
        File.WriteAllText(Application.persistentDataPath + "/Settings.txt", jsonData);
    }

    private void LoadSettings()
    {
        settingsVariables = new SettingsVariables();
        settingsVariables = JsonUtility.FromJson<SettingsVariables>(File.ReadAllText(Application.persistentDataPath + "/Settings.txt"));

        language = settingsVariables.savedLanguage;
    }
    #endregion
}
