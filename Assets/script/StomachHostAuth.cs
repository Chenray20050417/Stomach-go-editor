
using System;
using System.Collections;
using System.Text;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class StomachHostAuth : MonoBehaviour
{
    public static StomachHostAuth Instance { get; private set; }

    [Header("Firebase 主機狀態")]
    [SerializeField] private bool isReady = false;
    [SerializeField] private bool isHostAuthorized = false;
    [SerializeField] private bool isLoggingIn = false;

    [Header("登入期間暫停的控制腳本")]
    [Tooltip("可拖入 JoyConCameraLook 或其他會鎖定滑鼠的腳本")]
    public Behaviour[] pauseDuringLogin;

    [Header("登入完成設定")]
    public bool restoreControlsAfterLogin = true;
    public bool hideLoginAfterSuccess = true;

    private FirebaseAuth auth;

    private string statusMessage = "等待 Firebase 初始化";

    private bool[] previousStates;
    private bool controlsPaused = false;

private string hostEmail = "";
private string hostPassword = "";
private bool focusEmail = false;
    public bool IsReady
    {
        get { return isReady; }
    }

    public bool IsHostAuthorized
    {
        get { return isReady && isHostAuthorized; }
    }

    // ==================================================
    // 1. 初始化
    // ==================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[HostAuth] 重複的主機驗證元件"
            );

            enabled = false;
            return;
        }

        Instance = this;
    }

   private IEnumerator Start()
{
    PauseControls();

    Debug.Log("[HostAuth] 等待 FirebaseBootstrap...");

    while (FirebaseBootstrap.Instance == null)
    {
        yield return null;
    }

    FirebaseBootstrap bootstrap =
        FirebaseBootstrap.Instance;

    while (!bootstrap.IsReady && !bootstrap.HasFailed)
    {
        yield return null;
    }

    if (bootstrap.HasFailed)
    {
        statusMessage = "Firebase 初始化失敗";
        Debug.LogError("[HostAuth] Bootstrap 初始化失敗");
        yield break;
    }

    auth = FirebaseAuth.DefaultInstance;
    isReady = true;

    statusMessage = "Firebase 準備完成，請登入主機";

    Debug.Log("[HostAuth] Firebase Auth 初始化完成");


 if (auth.CurrentUser != null)
 {
     yield return StartCoroutine(CheckHostClaim());
 }
}
    // ==================================================
    // 2. 登入期間控制滑鼠
    // ==================================================
private void Update()
{
    if (!IsHostAuthorized)
    {
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }
}

    private void PauseControls()
    {
        if (controlsPaused)
            return;

        if (pauseDuringLogin == null)
            return;

        previousStates =
            new bool[pauseDuringLogin.Length];

        for (int i = 0; i < pauseDuringLogin.Length; i++)
        {
            Behaviour component = pauseDuringLogin[i];

            if (component == null)
                continue;

            if (component == this)
                continue;

            previousStates[i] = component.enabled;
            component.enabled = false;
        }

        controlsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[HostAuth] 已暫停視角控制");
    }

    private void RestoreControls()
    {
        if (!restoreControlsAfterLogin)
            return;

        if (!controlsPaused || previousStates == null)
            return;

        for (int i = 0; i < pauseDuringLogin.Length; i++)
        {
            Behaviour component = pauseDuringLogin[i];

            if (component == null || component == this)
                continue;

            component.enabled = previousStates[i];
        }

        controlsPaused = false;

        Debug.Log("[HostAuth] 已恢復原本控制");
    }

    // ==================================================
    // 3. 主機登入
    // ==================================================

    public void LoginHost(string email, string password)
    {
        if (!isReady || auth == null)
        {
            statusMessage = "Firebase 尚未準備完成";
            return;
        }

        if (isLoggingIn)
            return;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrEmpty(password))
        {
            statusMessage = "請輸入 Email 和密碼";
            return;
        }

        isLoggingIn = true;
        isHostAuthorized = false;

        statusMessage = "正在登入 Firebase...";

        auth.SignInWithEmailAndPasswordAsync(
            email.Trim(),
            password
        ).ContinueWithOnMainThread(task =>
        {
            if (this == null)
                return;

            if (task.IsFaulted || task.IsCanceled)
            {
                isLoggingIn = false;
                statusMessage = "登入失敗，請檢查帳號密碼";

                Debug.LogError(
                    "[HostAuth] 登入失敗：" +
                    task.Exception
                );

                return;
            }

            Debug.Log("[HostAuth] 主機登入成功");

            StartCoroutine(CheckHostClaim());
        });
    }

    // ==================================================
    // 4. 驗證 Firebase host: true
    // ==================================================

    private IEnumerator CheckHostClaim()
    {
        isHostAuthorized = false;
        isLoggingIn = true;

        FirebaseUser user =
            auth != null ? auth.CurrentUser : null;

        if (user == null)
        {
            isLoggingIn = false;
            statusMessage = "找不到登入使用者";
            yield break;
        }

        statusMessage = "正在確認主機權限...";

        // 強制更新 Firebase Token
        var tokenTask = user.TokenAsync(true);

        yield return new WaitUntil(
            () => tokenTask.IsCompleted
        );

        if (tokenTask.IsFaulted || tokenTask.IsCanceled)
        {
            isLoggingIn = false;
            statusMessage = "Token 驗證失敗";

            Debug.LogError(
                "[HostAuth] Token 取得失敗：" +
                tokenTask.Exception
            );

            yield break;
        }

        bool hasHostClaim = false;

        try
        {
            string token = tokenTask.Result;
            string[] parts = token.Split('.');

            if (parts.Length != 3)
                throw new Exception("JWT 格式錯誤");

            string payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            int padding =
                (4 - payload.Length % 4) % 4;

            payload += new string('=', padding);

            byte[] bytes =
                Convert.FromBase64String(payload);

            string json =
                Encoding.UTF8.GetString(bytes);

            HostClaims claims =
                JsonUtility.FromJson<HostClaims>(json);

            hasHostClaim =
                claims != null && claims.host;
        }
        catch (Exception error)
        {
            Debug.LogError(
                "[HostAuth] 權限解析失敗：" +
                error.Message
            );
        }

        if (!hasHostClaim)
        {
            isLoggingIn = false;
            isHostAuthorized = false;

            statusMessage = "此帳號沒有 host 權限";

            Debug.LogError(
                "[HostAuth] host 權限驗證失敗"
            );

            yield break;
        }

        // 避免驗證期間切換帳號
        if (auth.CurrentUser == null ||
            auth.CurrentUser.UserId != user.UserId)
        {
            isLoggingIn = false;
            statusMessage = "主機登入狀態已變更";
            yield break;
        }

        isHostAuthorized = true;
        isLoggingIn = false;

        statusMessage = "主機驗證成功";

        Debug.Log(
            "[HostAuth] 主機授權成功！host: true"
        );

        RestoreControls();
    }

    [Serializable]
    private class HostClaims
    {
        public bool host;
    }

    // ==================================================
    // 5. 登出主機
    // ==================================================

    public void LogoutHost()
    {
        if (auth != null)
        {
            auth.SignOut();
        }

        isHostAuthorized = false;
        isLoggingIn = false;

        statusMessage = "主機已登出";

        PauseControls();

        Debug.Log("[HostAuth] 主機已登出");
    }

    // ==================================================
    // 6. Unity Editor 登入畫面
    // ==================================================



    private void OnGUI()
    {
        if (!enabled)
            return;

        if (IsHostAuthorized)
        {
            if (hideLoginAfterSuccess)
                return;

            GUILayout.BeginArea(
                new Rect(20, 20, 380, 105),
                GUI.skin.box
            );

            GUILayout.Label(
                "Stomach GO - Host Authorized"
            );

            GUILayout.Label("host: true");

            if (GUILayout.Button("Logout"))
            {
                LogoutHost();
            }

            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginArea(
            new Rect(20, 20, 400, 270),
            GUI.skin.box
        );

        GUILayout.Label("STOMACH GO - HOST LOGIN");

        GUILayout.Space(10);

        GUILayout.Label("Email");

        GUI.SetNextControlName("HostEmail");

        hostEmail = GUILayout.TextField(
            hostEmail,
            GUILayout.Height(28)
        );

        GUILayout.Space(8);

        GUILayout.Label("Password");

        GUI.SetNextControlName("HostPassword");

        hostPassword = GUILayout.PasswordField(
            hostPassword,
            '*',
            GUILayout.Height(28)
        );

        GUILayout.Space(10);

        if (focusEmail)
        {
            GUI.FocusControl("HostEmail");
            focusEmail = false;
        }

        GUI.enabled = isReady && !isLoggingIn;

        if (GUILayout.Button(
            "Login Host",
            GUILayout.Height(35)))
        {
            string enteredPassword = hostPassword;

            hostPassword = "";

            // 移除輸入框的鍵盤焦點
            GUI.FocusControl("");

            LoginHost(hostEmail, enteredPassword);
        }

        GUI.enabled = true;

        GUILayout.Space(8);
        GUILayout.Label(statusMessage);

        GUILayout.EndArea();
    }



    // ==================================================
    // 7. 清理
    // ==================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
