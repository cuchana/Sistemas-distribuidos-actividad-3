using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Linq;

public class AuthHandler : MonoBehaviour
{
    private string Token;
    private string Username;

    private string apiiUrl = "https://sid-restapi.onrender.com";

    [Header("Inputs")]
    [SerializeField] TMP_InputField usernameInputField;
    [SerializeField] TMP_InputField passwordInputField;

    [Header("UI")]
    [SerializeField] TMP_Text usernameLabel;
    [SerializeField] TMP_Text leaderboardText;

    [Header("Popup")]
    [SerializeField] TMP_Text popupMessage;

    [Header("Panels")]
    [SerializeField] GameObject panelLogin;
    [SerializeField] GameObject panelUser;

    void Start()
    {
        Token = PlayerPrefs.GetString("Token", "");
        Username = PlayerPrefs.GetString("Username", "");

        if (!string.IsNullOrEmpty(Token))
        {
            panelLogin.SetActive(false);
            panelUser.SetActive(true);

            usernameLabel.text = "Bienvenido/a " + Username;
        }
        else
        {
            panelLogin.SetActive(true);
            panelUser.SetActive(false);
        }
    }

    // LOGIN
    public void LoginButtonHandler()
    {
        string username = usernameInputField.text;
        string password = passwordInputField.text;

        StartCoroutine(LoginCoroutine(username, password));
    }

    IEnumerator LoginCoroutine(string username, string password)
    {
        AuthData authData = new AuthData { username = username, password = password };

        string jsonData = JsonUtility.ToJson(authData);

        UnityWebRequest www = new UnityWebRequest(apiiUrl + "/api/auth/login", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ShowPopup("Error en login");
        }
        else
        {
            AuthResponse authResponse = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);

            Token = authResponse.token;
            Username = authResponse.usuario.username;

            PlayerPrefs.SetString("Token", Token);
            PlayerPrefs.SetString("Username", Username);

            ShowPopup("Login exitoso");

            SetUIForUserLogged();
        }
    }

    // REGISTER
    public void RegisterButtonHandler()
    {
        string username = usernameInputField.text;
        string password = passwordInputField.text;

        StartCoroutine(RegisterCoroutine(username, password));
    }

    IEnumerator RegisterCoroutine(string username, string password)
    {
        AuthData authData = new AuthData { username = username, password = password };

        string jsonData = JsonUtility.ToJson(authData);

        UnityWebRequest www = new UnityWebRequest(apiiUrl + "/api/usuarios", "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ShowPopup("Error registrando usuario");
        }
        else
        {
            ShowPopup("Usuario registrado");

            // LOGIN AUTOMATICO
            StartCoroutine(LoginCoroutine(username, password));
        }
    }

    void SetUIForUserLogged()
    {
        panelLogin.SetActive(false);
        panelUser.SetActive(true);

        usernameLabel.text = "Welcome " + Username;
    }

    // LOGOUT
    public void Logout()
    {
        PlayerPrefs.DeleteKey("Token");
        PlayerPrefs.DeleteKey("Username");

        Token = "";
        Username = "";

        panelUser.SetActive(false);
        panelLogin.SetActive(true);

        leaderboardText.text = "";

        ShowPopup("Sesión cerrada");
    }

    // SUBIR SCORE
    public void UpdateScoreButton()
    {
        int randomScore = Random.Range(50, 999);

        StartCoroutine(UpdateScoreCoroutine(randomScore));
    }

    IEnumerator UpdateScoreCoroutine(int score)
    {
        UpdateUserData data = new UpdateUserData();
        data.username = Username;
        data.data = new ScoreData { score = score };

        string jsonData = JsonUtility.ToJson(data);

        UnityWebRequest www = new UnityWebRequest(apiiUrl + "/api/usuarios", "PATCH");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("x-token", Token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ShowPopup("Error actualizando score");
        }
        else
        {
            ShowPopup("Score actualizado");

            // actualizar tabla automaticamente
            GetUsers();
        }
    }

    // LEADERBOARD
    public void GetUsers()
    {
        StartCoroutine(GetUsersCoroutine());
    }

    IEnumerator GetUsersCoroutine()
    {
        string url = apiiUrl + "/api/usuarios?limit=20&skip=0&sort=true";

        UnityWebRequest www = UnityWebRequest.Get(url);

        www.SetRequestHeader("x-token", Token);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ShowPopup("Error cargando leaderboard");
        }
        else
        {
            LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(www.downloadHandler.text);

            DisplayLeaderboard(response);
        }
    }

    public void ResetScore()
    {
        StartCoroutine(UpdateScoreCoroutine(0));
    }

    void DisplayLeaderboard(LeaderboardResponse response)
    {
        leaderboardText.text = "";

        var sortedUsers = response.usuarios
            .OrderByDescending(user => user.data.score);

        int position = 1;

        foreach (UserScore user in sortedUsers)
        {
            if (user.username == Username)
            {
                leaderboardText.text += "<color=yellow>" + position + ". " + user.username + " - " + user.data.score + "</color>\n";
            }
            else
            {
                leaderboardText.text += position + ". " + user.username + " - " + user.data.score + "\n";
            }

            position++;
        }
    }

    // POPUP SYSTEM
    public void ShowPopup(string message)
    {
        StartCoroutine(PopupCoroutine(message));
    }

    IEnumerator PopupCoroutine(string message)
    {
        popupMessage.text = message;
        popupMessage.gameObject.SetActive(true);

        yield return new WaitForSeconds(2f);

        popupMessage.gameObject.SetActive(false);
    }
}

[System.Serializable]
public class AuthData
{
    public string username;
    public string password;
}

[System.Serializable]
public class User
{
    public string _id;
    public string username;
}

[System.Serializable]
public class AuthResponse
{
    public User usuario;
    public string token;
}

[System.Serializable]
public class ScoreData
{
    public int score;
}

[System.Serializable]
public class UpdateUserData
{
    public string username;
    public ScoreData data;
}

[System.Serializable]
public class UserScore
{
    public string username;
    public ScoreData data;
}

[System.Serializable]
public class LeaderboardResponse
{
    public UserScore[] usuarios;
}