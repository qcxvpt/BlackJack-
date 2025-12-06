using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using SFB;  // Include the StandaloneFileBrowser namespace

public class Login : MonoBehaviour
{
    public InputField nickname_field;
    private string nickname;
    public int balance;
    public int highscore;
    public Text highscore_text;

    void Start()
    {
        // Load balance and highscore from PlayerPrefs
        balance = PlayerPrefs.GetInt("Total", balance);
        highscore = PlayerPrefs.GetInt("Record", highscore);
    }

    // Method to update the nickname string whenever the input field changes
    public void Update()
    {
        nickname = nickname_field.text;
        highscore_text.text = "Your Highscore:\n"+highscore.ToString()+"$";
    }
    public void LoadScene()
    {
        SceneManager.LoadScene("SampleScene");
    }
    // Save the nickname, balance, and highscore to a JSON file
    public void SaveData()
    {
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogError("Nickname cannot be null or empty!");
            return;
        }

        // Create a simple data structure to store the nickname, balance, and highscore
        PlayerData playerData = new PlayerData
        {
            nickname = nickname,
            balance = balance,
            highscore = highscore
        };

        // Convert to JSON
        string json = JsonUtility.ToJson(playerData);

        // Use StandaloneFileBrowser to let the player choose where to save the file
        var path = StandaloneFileBrowser.SaveFilePanel("Save Nickname", "", "PlayerData", "json");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, json);
            Debug.Log($"Data saved to {path}");
        }
        else
        {
            Debug.LogError("Save cancelled or path invalid!");
        }
    }

    // Load the nickname, balance, and highscore from a JSON file
    public void LoadData()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Load Nickname", "", "json", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string path = paths[0];

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);

                nickname = playerData.nickname;
                balance = playerData.balance;
                highscore = playerData.highscore;

                // Update the InputField with the loaded nickname
                nickname_field.text = nickname;

                // Optionally, save the loaded balance and highscore back to PlayerPrefs
                PlayerPrefs.SetInt("Total", balance);
                PlayerPrefs.SetInt("Record", highscore);
                PlayerPrefs.Save();

                Debug.Log($"Data loaded from {path}");
            }
            else
            {
                Debug.LogError("File does not exist!");
            }
        }
        else
        {
            Debug.LogError("No file selected!");
        }
    }

    [System.Serializable]
    public class PlayerData
    {
        public string nickname;
        public int balance;
        public int highscore;
    }
}
