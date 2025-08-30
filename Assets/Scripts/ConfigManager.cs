using System;
using System.IO;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    SettingsConfig config = null;
    public SettingsConfig Config => config;

    const string CONFIG_FILE_NAME = "SettingsConfig.json";
    string configPath;

    public void LoadConfig()
    {
        // Config file is in the build's Data/StreamingAssets folder
        configPath = Path.Combine(Application.streamingAssetsPath, CONFIG_FILE_NAME);

        string configJson = "";
        
        // Open file:
        if (File.Exists(configPath))
        {
            try
            {
                configJson = File.ReadAllText(configPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading config file: {e.Message}");
                CreateDefaultSettingsConfig();
            }
        }
        else
        {
            Debug.LogWarning($"Config file not found at: {configPath}");
            CreateDefaultSettingsConfig();
        }
        
        // Parse to SettingsConfig object:
        try
        {
            config = JsonUtility.FromJson<SettingsConfig>(configJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing config JSON: {e.Message}");
            CreateDefaultSettingsConfig();
        }
    }

    void CreateDefaultSettingsConfig()
    {
        config = new SettingsConfig();
    }
}