using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using TMPro;

[System.Serializable]
public class APIResponse
{
    public string id;
}

[System.Serializable]
public class ThreadMessageResponse
{
    public List<ThreadMessageData> data;
}

[System.Serializable]
public class ThreadMessageData
{
    public string id;
    public string role;
    public List<ThreadMessageContent> content;
}

[System.Serializable]
public class ThreadMessageContent
{
    public ThreadMessageText text;
}

[System.Serializable]
public class ThreadMessageText
{
    public string value;
}

[System.Serializable]
public class ImageResponse
{
    public List<ImageData> data;
}

[System.Serializable]
public class ImageData
{
    public string url;
}

[System.Serializable]
public class MessageData
{
    public string role;
    public string content;
}

[System.Serializable]
public class RunsData
{
    public string assistant_id;
}

[System.Serializable]
public class ImageGenerationRequest
{
    public string prompt;
    public int n;
    public string size;
}

[System.Serializable]
public class ConfigData
{
    public string openAIKey;
    public string assistantID;
}



public class OpenAIInterface : MonoBehaviour
{
    public static OpenAIInterface Instance { get; private set; }
    private string user_APIKey = null;
    private string assistant_ID = null;
    private string apiBaseUrl = "https://api.openai.com/v1/threads";
    private string game_APIThread;
    public string current_Page = "0";
    public string current_Nerrative;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("OpenAIInterface instance initialized.");
            LoadConfig();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadConfig()
    {
        string configPath = Path.Combine(Application.dataPath, "Scripts/OpenAI API/config.json");
        Debug.Log(configPath);
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            ConfigData configData = JsonUtility.FromJson<ConfigData>(json);
            user_APIKey = configData.openAIKey;
            assistant_ID = configData.assistantID;
            Debug.Log("Config loaded successfully.");
        }
        else
        {
            Debug.LogError("Config file not found.");
        }
    }

    public void SendNarrativeToAPI(string bookName, string narrative, string pagenum)
    {
        Debug.Log($"SendNarrativeToAPI called with bookName: {bookName}, narrative: {narrative}");
        this.current_Page = pagenum;
        this.current_Nerrative = narrative;
        StartCoroutine(SendNarrativeCoroutine(bookName, narrative));
    }

    private IEnumerator SendNarrativeCoroutine(string bookName, string narrative)
    {
        Debug.Log("SendNarrativeCoroutine started.");
        var bookData = new
        {
            page = new 
            {
                book_name = bookName,
                narrative = narrative
            }
        };

        string json = JsonUtility.ToJson(bookData, true);
        Debug.Log("SendNarrative JSON: " + json);

        using (UnityWebRequest request = new UnityWebRequest(apiBaseUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {user_APIKey}");
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Text: " + request.downloadHandler.text);
            }
            else
            {
                var response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);
                game_APIThread = response.id;
                Debug.Log($"API Thread ID: {game_APIThread}");
                SendMessageToThread(narrative, bookName);
            }
        }
    }

    private void SendMessageToThread(string narrative, string bookName)
    {
        StartCoroutine(SendMessageCoroutine(narrative, bookName));
    }

    private IEnumerator SendMessageCoroutine(string narrative, string bookName)
    {
        string url = $"{apiBaseUrl}/{game_APIThread}/messages";
        var messageData = new MessageData
        {
            role = "user",
            content = narrative
        };

        string json = JsonUtility.ToJson(messageData);
        Debug.Log("SendMessage JSON: " + json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {user_APIKey}");
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Text: " + request.downloadHandler.text);
            }
            else
            {
                RunThread(bookName);
            }
        }
    }

    private void RunThread(string bookName)
    {
        StartCoroutine(RunThreadCoroutine(bookName));
    }

    private IEnumerator RunThreadCoroutine(string bookName)
    {
        string url = $"{apiBaseUrl}/{game_APIThread}/runs";
        var runData = new RunsData
        {
            assistant_id = $"{assistant_ID}"
        };

        string json = JsonUtility.ToJson(runData);
        Debug.Log("RunThread JSON: " + json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {user_APIKey}");
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Text: " + request.downloadHandler.text);
            }
            else
            {
                GetMessageResponse(bookName);
            }
        }
    }

    private void GetMessageResponse(string bookName)
    {
        StartCoroutine(GetMessageResponseCoroutine(bookName));
    }

    private IEnumerator GetMessageResponseCoroutine(string bookName)
    {
        string url = $"{apiBaseUrl}/{game_APIThread}/messages?limit=1";
        int maxAttempts = 10;
        int attempt = 0;
        bool success = false;

        while (attempt < maxAttempts && !success)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "GET"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {user_APIKey}");
                request.SetRequestHeader("OpenAI-Beta", "assistants=v1");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(request.error);
                    Debug.LogError("Response Code: " + request.responseCode);
                    Debug.LogError("Response Text: " + request.downloadHandler.text);
                    yield break; // Exit the coroutine if there's a network error
                }
                else
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log("Response Text: " + responseText);

                    var response = JsonUtility.FromJson<ThreadMessageResponse>(responseText);
                    if (response.data != null && response.data.Count > 0)
                    {
                        var messageData = response.data[0];
                        if (messageData.content != null && messageData.content.Count > 0)
                        {
                            var contentData = messageData.content[0];
                            if (contentData.text != null && !string.IsNullOrEmpty(contentData.text.value))
                            {
                                var messageContent = contentData.text.value;
                                Debug.Log("Received Message Content: " + messageContent);
                                string imageDescription = ExtractImageDescription(messageContent);
                                Debug.Log("imageDescription Message Content: " + imageDescription);
                                if (!string.IsNullOrEmpty(imageDescription))
                                {
                                    SendDescriptionToDalle(imageDescription, messageContent, bookName);
                                    success = true;
                                }
                            }
                        }
                    }
                }
            }

            if (!success)
            {
                Debug.LogWarning("No valid message content received. Retrying...");
                yield return new WaitForSeconds(3);
                attempt++;
            }
        }

        if (!success)
        {
            Debug.LogError("Failed to get a valid response after multiple attempts. Please try again later.");
            // Notify the player here
        }
    }

    private string ExtractImageDescription(string messageContent)
    {
        string startTag = "image generation";
        string endTag = "end image generation";
        string lowerCaseContent = messageContent.ToLower();

        int startIndex = lowerCaseContent.IndexOf(startTag) + startTag.Length;
        int endIndex = lowerCaseContent.IndexOf(endTag);

        if (startIndex == -1 || endIndex == -1)
        {
            return null;
        }

        return messageContent.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private void SendDescriptionToDalle(string description, string pageText, string bookName)
    {
        Debug.Log($"Sending description to DALL-E: {description}");
        if (string.IsNullOrEmpty(description))
        {
            Debug.LogError("Description is empty. Cannot send to DALL-E.");
            return;
        }

        StartCoroutine(SendDescriptionToDalleCoroutine(description, pageText, bookName));
    }

    private IEnumerator SendDescriptionToDalleCoroutine(string description, string pageText, string bookName)
    {
        string url = "https://api.openai.com/v1/images/generations";
        var imageRequest = new ImageGenerationRequest
        {
            prompt = description,
            n = 1,
            size = "1024x1024"
        };

        string json = JsonUtility.ToJson(imageRequest);
        Debug.Log("SendDescriptionToDalle JSON: " + json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {user_APIKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Text: " + request.downloadHandler.text);
            }
            else
            {
                var response = JsonUtility.FromJson<ImageResponse>(request.downloadHandler.text);
                string imageUrl = response.data[0].url;
                StartCoroutine(DownloadImageCoroutine(imageUrl, pageText, bookName));
            }
        }
    }

    private IEnumerator DownloadImageCoroutine(string url, string pageText, string bookName)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Text: " + request.downloadHandler.text);
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                byte[] imageBytes = texture.EncodeToPNG();
                string imagePath = Path.Combine(Application.persistentDataPath, PlayerSession.SelectedPlayerName, bookName, $"page{this.current_Page}_image.png");
                File.WriteAllBytes(imagePath, imageBytes);

                string bookFolderPath = Path.Combine(Application.persistentDataPath, PlayerSession.SelectedPlayerName, bookName);
                DataManager.CreateDirectoryIfNotExists(bookFolderPath);

                string bookFilePath = Path.Combine(bookFolderPath, "bookData.json");
                Book bookData;
                if (File.Exists(bookFilePath))
                {
                    string bookJson = File.ReadAllText(bookFilePath);
                    bookData = JsonUtility.FromJson<Book>(bookJson);
                }
                else
                {
                    bookData = new Book(bookName, this.current_Nerrative);
                }

                bookData.Pages.Add(new Page(pageText, imagePath));

                string json = JsonUtility.ToJson(bookData, true);
                Debug.Log("Final JSON: " + json);
                File.WriteAllText(bookFilePath, json);
            }
        }
    }
}