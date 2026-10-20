
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class StomachGamePresence : MonoBehaviour
{
    [Header("Firebase 設定")]
    public string roomId = "ab123";
    public StomachHostAuth hostAuth;

    [Header("在線心跳設定")]
    [Min(1f)]
    public float heartbeatSeconds = 5f;

    [Header("目前狀態（唯讀）")]
    [SerializeField]
    private bool firebaseReady = false;

    [SerializeField]
    private bool isPublishing = false;

    [SerializeField]
    private bool isOnline = false;

    private DatabaseReference gameStatusRef;
    private string sessionId;

    private bool shuttingDown = false;
    private bool previousAuthorized = false;

    private bool Authorized
    {
        get
        {
            return hostAuth != null &&
                   hostAuth.isActiveAndEnabled &&
                   hostAuth.IsHostAuthorized;
        }
    }

    // =========================================
    // 1. 啟動：等待 Bootstrap
    // =========================================

    private IEnumerator Start()
    {
        if (hostAuth == null)
        {
            hostAuth =
                FindObjectOfType<StomachHostAuth>();
        }

        Debug.Log(
            "[Presence] 等待 FirebaseBootstrap..."
        );

        while (FirebaseBootstrap.Instance == null)
        {
            yield return null;
        }

        FirebaseBootstrap bootstrap =
            FirebaseBootstrap.Instance;

        while (!bootstrap.IsReady &&
               !bootstrap.HasFailed)
        {
            yield return null;
        }

        if (bootstrap.HasFailed)
        {
            Debug.LogError(
                "[Presence] Firebase 初始化失敗"
            );

            yield break;
        }

        FirebaseDatabase database =
            FirebaseDatabase.GetInstance(
                "https://virusgame-c7baa-default-rtdb.firebaseio.com/"
            );

        gameStatusRef =
            database.GetReference("rooms")
                .Child(roomId)
                .Child("gameStatus");

        firebaseReady = true;

        Debug.Log(
            "[Presence] 初始化成功，等待主機授權"
        );

        yield return StartCoroutine(
            PublishPresenceLoop()
        );
    }

    // =========================================
    // 2. 每 5 秒發送心跳
    // =========================================

    private IEnumerator PublishPresenceLoop()
    {
        while (!shuttingDown)
        {
            if (!firebaseReady || !Authorized)
            {
                if (previousAuthorized)
                {
                    Debug.LogWarning(
                        "[Presence] 主機授權已失效，停止心跳"
                    );
                }

                previousAuthorized = false;
                isOnline = false;

                yield return new WaitForSecondsRealtime(
                    0.5f
                );

                continue;
            }

            // 每次重新取得授權，都建立新 Session
            if (!previousAuthorized)
            {
                sessionId = Guid.NewGuid().ToString("N");

                previousAuthorized = true;

                Debug.Log(
                    "[Presence] 建立新 Session"
                );
            }

            var presenceData =
                new Dictionary<string, object>
                {
                    { "sessionId", sessionId },
                    {
                        "lastSeen",
                        ServerValue.Timestamp
                    }
                };

            isPublishing = true;

            var task =
                gameStatusRef.SetValueAsync(
                    presenceData
                );

            yield return new WaitUntil(
                () => task.IsCompleted
            );

            isPublishing = false;

            if (shuttingDown)
                yield break;

            if (!Authorized)
            {
                isOnline = false;
                previousAuthorized = false;

                continue;
            }

            if (task.IsCanceled || task.IsFaulted)
            {
                isOnline = false;

                Debug.LogWarning(
                    "[Presence] 心跳寫入失敗：" +
                    task.Exception
                );
            }
            else
            {
                if (!isOnline)
                {
                    Debug.Log(
                        "[Presence] Unity 在線狀態發布成功"
                    );
                }

                isOnline = true;
            }

            yield return new WaitForSecondsRealtime(
                heartbeatSeconds
            );
        }
    }

    // =========================================
    // 3. 停止與清理
    // =========================================

    private void OnDisable()
    {
        shuttingDown = true;

        StopAllCoroutines();

        isOnline = false;
        isPublishing = false;
    }

    private void OnDestroy()
    {
        shuttingDown = true;
    }
}
