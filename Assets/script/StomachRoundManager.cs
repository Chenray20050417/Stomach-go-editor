
using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class StomachRoundManager : MonoBehaviour
{
    [Header("Firebase")]
    public string roomId = "ab123";
    public StomachHostAuth hostAuth;

    [Header("Round Timings")]
    [Min(1)] public int waitingSeconds = 20;
    [Min(1)] public int votingSeconds = 20;
    [Min(0)] public float resultsDisplaySeconds = 3f;
    [Min(1)] public float simulatedInterferenceSeconds = 10f;

    public KeyCode testStartKey = KeyCode.T;

    [Header("回合控制")]
    public bool autoNextRound = true;
    public bool simulateInterference = true;

    [Header("Vote Host")]
    public StomachVoteHost voteHost;

    [Header("正式 Unity 干擾")]
    public StomachInterferenceManager interferenceManager;

    private DatabaseReference roundRef;
    private DatabaseReference resultsRef;
    private DatabaseReference participantsRef;
    private DatabaseReference presenceRef;

    private bool firebaseReady;
    private bool running;
    private bool lastWriteSucceeded;

    private bool resetStarted;
    private bool resetCompleted;
    private bool shuttingDown;

    private long roundId;

    private static readonly string[] Types =
    {
        "STOMACH_ACID",
        "VIRUS_FRENZY",
        "AIM_DRIFT"
    };

    private bool Authorized
    {
        get
        {
            return hostAuth != null &&
                   hostAuth.isActiveAndEnabled &&
                   hostAuth.IsHostAuthorized;
        }
    }

    private bool CanOperate
    {
        get
        {
            return firebaseReady && Authorized;
        }
    }

    // =============================================
    // 1. Firebase 初始化
    // =============================================

    private IEnumerator Start()
    {
        if (hostAuth == null)
            hostAuth = FindObjectOfType<StomachHostAuth>();

        if (voteHost == null)
            voteHost = FindObjectOfType<StomachVoteHost>();

        if (interferenceManager == null)
            interferenceManager =
                FindObjectOfType<StomachInterferenceManager>();

        Debug.Log("[Round] 等待 FirebaseBootstrap...");

        while (FirebaseBootstrap.Instance == null)
            yield return null;

        FirebaseBootstrap bootstrap =
            FirebaseBootstrap.Instance;

        while (!bootstrap.IsReady && !bootstrap.HasFailed)
            yield return null;

        if (bootstrap.HasFailed)
        {
            Debug.LogError("[Round] Firebase 初始化失敗");
            yield break;
        }

        DatabaseReference room =
            FirebaseDatabase.GetInstance(
                "https://virusgame-c7baa-default-rtdb.firebaseio.com/"
            ).GetReference("rooms").Child(roomId);

        roundRef = room.Child("currentRound");
        resultsRef = room.Child("results");
        participantsRef = room.Child("participants");
        presenceRef = room.Child("presence");

        firebaseReady = true;

        Debug.Log(
            "[Round] Firebase 已初始化，等待主機授權"
        );
    }

    // =============================================
    // 2. Update
    // =============================================

    private void Update()
    {
        if (shuttingDown)
            return;

        if (hostAuth == null)
            hostAuth = FindObjectOfType<StomachHostAuth>();

        // 主機授權後，自動恢復 IDLE
        if (CanOperate && !resetStarted && !running)
        {
            resetStarted = true;
            StartCoroutine(ResetOnStartup());
        }

        // 登出時停止正在進行的回合
        if (running && !Authorized)
        {
            StopAllCoroutines();

            running = false;
            resetStarted = false;
            resetCompleted = false;

            Debug.LogWarning(
                "[Round] 主機已登出，停止回合控制"
            );
        }

        if (Input.GetKeyDown(testStartKey))
            BeginTestRound();
    }

    // =============================================
    // 3. 啟動時自動恢復 IDLE
    // =============================================

    private IEnumerator ResetOnStartup()
    {
        resetCompleted = false;

        if (!CanOperate || roundRef == null)
        {
            resetStarted = false;
            yield break;
        }

        var values = new Dictionary<string, object>
        {
            { "phase", "IDLE" },
            { "endsAt", 0L },
            { "voteEndsAt", 0L }
        };

        var task = roundRef.UpdateChildrenAsync(values);

        yield return new WaitUntil(
            () => task.IsCompleted
        );

        if (shuttingDown)
            yield break;

        if (!CanOperate || task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError(
                "[Round] 啟動重設 IDLE 失敗：" +
                task.Exception
            );

            resetStarted = false;
            yield break;
        }

        resetCompleted = true;

        Debug.Log(
            "[Round] 啟動後 Firebase 已恢復 IDLE，可以按 T"
        );
    }

    // =============================================
    // 4. 停止 Play 時嘗試恢復 IDLE
    // =============================================

    private void OnApplicationQuit()
    {
        if (shuttingDown)
            return;

        shuttingDown = true;

        if (!CanOperate || roundRef == null)
            return;

        var values = new Dictionary<string, object>
        {
            { "phase", "IDLE" },
            { "endsAt", 0L },
            { "voteEndsAt", 0L }
        };

        roundRef.UpdateChildrenAsync(values);

        Debug.Log(
            "[Round] 停止預覽，已送出 IDLE 重設請求（不保證送達）"
        );
    }

    // =============================================
    // 5. 開始回合
    // =============================================

    [ContextMenu("開始 V2 回合測試")]
    public void BeginTestRound()
    {
        if (!resetCompleted)
        {
            Debug.LogWarning(
                "[Round] 正在等待啟動時重設 IDLE，請稍後再按 T"
            );
            return;
        }

        if (!CanOperate)
        {
            Debug.LogWarning(
                "[Round] 請先完成 Firebase 主機登入"
            );
            return;
        }

        if (running)
        {
            Debug.LogWarning(
                "[Round] 回合正在執行"
            );
            return;
        }

        if (voteHost == null || !voteHost.IsReady)
        {
            Debug.LogWarning(
                "[Round] VoteHost 尚未取得主機授權"
            );
            return;
        }

        StartCoroutine(RunRounds());
    }

    // =============================================
    // 6. 回合循環
    // =============================================

    private IEnumerator RunRounds()
    {
        running = true;

        var get = roundRef.GetValueAsync();

        yield return new WaitUntil(
            () => get.IsCompleted
        );

        if (!CanOperate)
        {
            running = false;
            yield break;
        }

        if (get.IsFaulted || get.IsCanceled)
        {
            Debug.LogError(
                "[Round] 讀取失敗：" + get.Exception
            );

            running = false;
            yield break;
        }

        string phase = Read(get.Result, "phase");

        if (phase != "IDLE")
        {
            Debug.LogWarning(
                "[Round] 必須先為 IDLE，目前：" + phase
            );

            running = false;
            yield break;
        }

        // 正式回合從 1 開始
        roundId = Math.Max(
            1L,
            AsLong(Read(get.Result, "roundId"))
        );

        do
        {
            if (!CanOperate || !voteHost.IsReady)
                break;

            yield return RunRound(roundId);

            if (!lastWriteSucceeded)
                break;

            roundId++;

        } while (
            autoNextRound &&
            CanOperate &&
            voteHost.IsReady &&
            this != null &&
            isActiveAndEnabled
        );

        running = false;
    }

    // =============================================
    // 7. 執行一個回合
    // =============================================

    private IEnumerator RunRound(long id)
    {
        lastWriteSucceeded = false;

        if (!CanOperate)
            yield break;

        // -----------------------------------------
        // WAITING：準備階段
        // -----------------------------------------

        var reset = new Dictionary<string, object>
        {
            { "roundId", id },
            { "phase", "WAITING" },
            { "waitingSeconds", waitingSeconds },
            { "votingSeconds", votingSeconds },
            { "endsAt", Now() + waitingSeconds * 1000L },
            { "voteEndsAt", 0L },

            { "eligibleVoters", null },
            { "totals/STOMACH_ACID", 0L },
            { "totals/VIRUS_FRENZY", 0L },
            { "totals/AIM_DRIFT", 0L },

            { "playerVotes", null },
            { "winner", null },
            { "mvpUid", null },
            { "mvpName", null },
            { "mvpAvatarId", null },
            { "mvpVotes", null }
        };

        yield return Write(reset);

        if (!lastWriteSucceeded)
            yield break;

        Debug.Log(
            "[Round] 第 " + id + " 輪：WAITING"
        );

        yield return new WaitForSecondsRealtime(
            waitingSeconds
        );

        if (!CanOperate)
        {
            lastWriteSucceeded = false;
            yield break;
        }

        // -----------------------------------------
        // 鎖定本輪觀眾名單
        // -----------------------------------------

        var onlineTask = presenceRef.GetValueAsync();

        yield return new WaitUntil(
            () => onlineTask.IsCompleted
        );

        if (!CanOperate ||
            onlineTask.IsFaulted ||
            onlineTask.IsCanceled)
        {
            Debug.LogError(
                "[Round] 無法取得在線名單，取消開放投票：" +
                onlineTask.Exception
            );

            lastWriteSucceeded = false;
            yield break;
        }

        var allowedUids = new HashSet<string>();

        long snapshotTime = Now();

        const long onlineTimeoutMs = 90000L;

        foreach (
            DataSnapshot visitor in onlineTask.Result.Children
        )
        {
            long lastSeen = AsLong(
                Read(visitor, "lastSeen")
            );

            // 排除過久沒有更新在線紀錄的觀眾
            if (
                lastSeen > 0 &&
                lastSeen <= snapshotTime + 5000L &&
                snapshotTime - lastSeen <= onlineTimeoutMs
            )
            {
                allowedUids.Add(visitor.Key);
            }
        }

        voteHost.LockVotersForRound(id, allowedUids);

        // 網頁與主機必須使用同一份名單；空名單以 null 清除舊資料。
        var eligibleVoters = new Dictionary<string, object>();
        foreach (string uid in allowedUids)
            eligibleVoters[uid] = true;

        Debug.Log(
            "[Round] 第 " + id +
            " 輪鎖定觀眾：" +
            allowedUids.Count + " 人"
        );

        // -----------------------------------------
        // VOTING：開始點擊
        // -----------------------------------------

        long voteEnd =
            Now() + votingSeconds * 1000L;

        yield return Write(
            new Dictionary<string, object>
            {
                { "phase", "VOTING" },
                { "eligibleVoters", eligibleVoters.Count > 0 ? (object)eligibleVoters : null },
                { "endsAt", voteEnd },
                { "voteEndsAt", voteEnd }
            }
        );

        if (!lastWriteSucceeded)
            yield break;

        Debug.Log(
            "[Round] 第 " + id + " 輪：VOTING"
        );

        yield return new WaitForSecondsRealtime(
            votingSeconds
        );

        if (!CanOperate)
        {
            lastWriteSucceeded = false;
            yield break;
        }

        // -----------------------------------------
        // RESULT：結算
        // -----------------------------------------

        yield return Write(
            new Dictionary<string, object>
            {
                { "phase", "RESULT" },
                { "endsAt", 0L }
            }
        );

        if (!lastWriteSucceeded)
            yield break;

        yield return StartCoroutine(
            voteHost.DrainPendingRequests(
                voteHost.drainTimeoutSeconds
            )
        );

        if (!CanOperate || !voteHost.DrainSucceeded)
        {
            Debug.LogError(
                "[Round] 投票排空失敗，保留 RESULT"
            );

            lastWriteSucceeded = false;
            yield break;
        }

        var resultGet = roundRef.GetValueAsync();

        yield return new WaitUntil(
            () => resultGet.IsCompleted
        );

        if (
            !CanOperate ||
            resultGet.IsCanceled ||
            resultGet.IsFaulted
        )
        {
            Debug.LogError(
                "[Round] 結算資料讀取失敗"
            );

            lastWriteSucceeded = false;
            yield break;
        }

        DataSnapshot round = resultGet.Result;
        DataSnapshot totals = round.Child("totals");

        long max = 0;
        string winner = null;
        bool tie = false;

        foreach (string type in Types)
        {
            long score =
                AsLong(Read(totals, type));

            if (score > max)
            {
                max = score;
                winner = type;
                tie = false;
            }
            else if (score == max && score > 0)
            {
                tie = true;
            }
        }

        if (tie)
            winner = null;

        string mvpUid = null;
        string mvpName = null;

        int mvpAvatar = -1;
        long mvpVotes = 0;

        // -----------------------------------------
        // MVP 計算
        // -----------------------------------------

        if (winner != null)
        {
            DataSnapshot playerVotes =
                round.Child("playerVotes");

            foreach (
                DataSnapshot player in playerVotes.Children
            )
            {
                long clicks =
                    AsLong(Read(player, winner));

                if (
                    clicks > mvpVotes ||
                    (
                        clicks == mvpVotes &&
                        clicks > 0 &&
                        (
                            mvpUid == null ||
                            string.CompareOrdinal(
                                player.Key,
                                mvpUid
                            ) < 0
                        )
                    )
                )
                {
                    mvpVotes = clicks;
                    mvpUid = player.Key;
                }
            }

            if (mvpUid != null)
            {
                var profileTask =
                    participantsRef.Child(mvpUid)
                    .GetValueAsync();

                yield return new WaitUntil(
                    () => profileTask.IsCompleted
                );

                if (!CanOperate)
                {
                    lastWriteSucceeded = false;
                    yield break;
                }

                if (
                    !profileTask.IsFaulted &&
                    !profileTask.IsCanceled
                )
                {
                    mvpName = Read(
                        profileTask.Result,
                        "name"
                    );

                    mvpAvatar = (int)AsLong(
                        Read(
                            profileTask.Result,
                            "avatarId"
                        )
                    );
                }

                if (string.IsNullOrWhiteSpace(mvpName))
                    mvpName = "玩家 " + mvpUid;
            }
        }

        // -----------------------------------------
        // 儲存歷史結果
        // -----------------------------------------

        var result = new Dictionary<string, object>
        {
            { "roundId", id },
            { "winner", winner },
            { "winnerVotes", max },
            { "isTie", tie },
            { "mvpUid", mvpUid },
            { "mvpName", mvpName },
            { "mvpAvatarId", mvpAvatar },
            { "mvpVotes", mvpVotes },
            { "settledAt", Now() }
        };

        if (!CanOperate)
        {
            lastWriteSucceeded = false;
            yield break;
        }

        var historyTask =
            resultsRef.Child("round" + id)
            .SetValueAsync(result);

        yield return new WaitUntil(
            () => historyTask.IsCompleted
        );

        if (
            !CanOperate ||
            historyTask.IsFaulted ||
            historyTask.IsCanceled
        )
        {
            Debug.LogError(
                "[Round] 無法儲存歷史結果"
            );

            lastWriteSucceeded = false;
            yield break;
        }

        yield return Write(
            new Dictionary<string, object>
            {
                { "winner", winner },
                { "mvpUid", mvpUid },
                { "mvpName", mvpName },
                { "mvpAvatarId", mvpAvatar },
                { "mvpVotes", mvpVotes },
                { "winnerVotes", max },
                { "isTie", tie }
            }
        );

        if (!lastWriteSucceeded)
            yield break;

        Debug.Log(
            "[Round] RESULT：" +
            (winner ?? (tie ? "平手" : "無投票"))
        );

        if (resultsDisplaySeconds > 0)
        {
            yield return new WaitForSecondsRealtime(
                resultsDisplaySeconds
            );
        }

        if (!CanOperate)
        {
            lastWriteSucceeded = false;
            yield break;
        }

        // -----------------------------------------
        // INTERFERENCE：執行干擾
        // -----------------------------------------

        if (winner != null)
        {
            bool canRun =
                !simulateInterference &&
                interferenceManager != null;

            float seconds = canRun
                ? interferenceManager.durationSeconds
                : simulatedInterferenceSeconds;

            yield return Write(
                new Dictionary<string, object>
                {
                    { "phase", "INTERFERENCE" },
                    {
                        "endsAt",
                        Now() + (long)(seconds * 1000f)
                    }
                }
            );

            if (!lastWriteSucceeded)
                yield break;

            if (canRun)
            {
                if (!interferenceManager
                    .BeginInterference(winner))
                {
                    Debug.LogError(
                        "[Round] 干擾啟動失敗：" +
                        winner
                    );

                    lastWriteSucceeded = false;
                    yield break;
                }

                yield return new WaitUntil(
                    () => !interferenceManager.IsRunning
                );
            }
            else
            {
                Debug.LogWarning(
                    "[Round] 正在使用模擬干擾"
                );

                yield return new WaitForSecondsRealtime(
                    simulatedInterferenceSeconds
                );
            }
        }

        if (!CanOperate)
        {
            lastWriteSucceeded = false;
            yield break;
        }

        // -----------------------------------------
        // 回合自然結束，恢復 IDLE
        // -----------------------------------------

        yield return Write(
            new Dictionary<string, object>
            {
                { "phase", "IDLE" },
                { "endsAt", 0L },
                { "voteEndsAt", 0L }
            }
        );
    }

    // =============================================
    // 8. Firebase 寫入
    // =============================================

    private IEnumerator Write(
        Dictionary<string, object> values)
    {
        lastWriteSucceeded = false;

        if (!CanOperate)
        {
            Debug.LogWarning(
                "[Round] 無主機權限，拒絕寫入"
            );

            yield break;
        }

        var task = roundRef.UpdateChildrenAsync(values);

        yield return new WaitUntil(
            () => task.IsCompleted
        );

        if (
            !CanOperate ||
            task.IsFaulted ||
            task.IsCanceled
        )
        {
            Debug.LogError(
                "[Round] 寫入失敗：" +
                task.Exception
            );

            yield break;
        }

        lastWriteSucceeded = true;
    }

    // =============================================
    // 9. Firebase 讀取工具
    // =============================================

    private static string Read(
        DataSnapshot snapshot,
        string field)
    {
        return snapshot != null &&
               snapshot.HasChild(field)
            ? Convert.ToString(
                snapshot.Child(field).Value
            )
            : "";
    }

    private static long AsLong(string value)
    {
        long result;

        return long.TryParse(value, out result)
            ? result
            : 0L;
    }

    private static long Now()
    {
        return DateTimeOffset.UtcNow
            .ToUnixTimeMilliseconds();
    }
}
