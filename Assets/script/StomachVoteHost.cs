
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class StomachVoteHost : MonoBehaviour
{
    [Header("Firebase")]
    public string roomId = "ab123";
    public StomachHostAuth hostAuth;

    [Header("投票設定")]
    public float minVoteIntervalSeconds = 0.2f;

    [Header("排空等待最長秒數")]
    public float drainTimeoutSeconds = 30f;

    private DatabaseReference roomRef;
    private DatabaseReference requestsRef;

    private readonly ConcurrentQueue<string> queue =
        new ConcurrentQueue<string>();

    private readonly HashSet<string> seen =
        new HashSet<string>();

    private readonly HashSet<string> applied =
        new HashSet<string>();

    private readonly Dictionary<string, long> lastAccepted =
        new Dictionary<string, long>();

    private readonly string[] options =
    {
        "STOMACH_ACID",
        "VIRUS_FRENZY",
        "AIM_DRIFT"
    };

    private bool firebaseReady;
    private bool listening;
    private bool processing;
    private bool lastAuthorized;

    private long hostRoundId = -1;

    // 只允許 VOTING 開始前，Unity 鎖定的觀眾參與本輪。
    private long lockedRoundId = -1;
    private readonly HashSet<string> lockedUids = new HashSet<string>();

    public void LockVotersForRound(long id, HashSet<string> uidSet)
    {
        lockedRoundId = id;
        lockedUids.Clear();
        if (uidSet != null)
            foreach (string uid in uidSet)
                lockedUids.Add(uid);

        Debug.Log("[VoteHost] 第 " + id + " 輪可投票人數：" + lockedUids.Count);
    }

    private bool Authorized
    {
        get
        {
            return hostAuth != null &&
                   hostAuth.isActiveAndEnabled &&
                   hostAuth.IsHostAuthorized;
        }
    }

    public bool IsReady
    {
        get
        {
            return firebaseReady && listening && Authorized;
        }
    }

    public bool IsIdle
    {
        get
        {
            return !processing && queue.IsEmpty;
        }
    }

    public bool DrainSucceeded { get; private set; }

private IEnumerator Start()
{
    if (hostAuth == null)
        hostAuth = FindObjectOfType<StomachHostAuth>();

    Debug.Log("[VoteHost] 等待 FirebaseBootstrap...");

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
        Debug.LogError("[VoteHost] Firebase 初始化失敗");
        yield break;
    }

    roomRef = FirebaseDatabase.GetInstance(
        "https://virusgame-c7baa-default-rtdb.firebaseio.com/"
    ).GetReference("rooms").Child(roomId);

    requestsRef = roomRef.Child("voteRequests");

    firebaseReady = true;

    Debug.Log(
        "[VoteHost] Firebase 已初始化，等待主機授權"
    );
}


    private void Update()
    {
        if (!firebaseReady)
            return;

        if (hostAuth == null)
            hostAuth = FindObjectOfType<StomachHostAuth>();

        bool authorized = Authorized;

        if (authorized != lastAuthorized)
        {
            lastAuthorized = authorized;

            if (authorized)
                StartListening();
            else
                StopListening();
        }

        if (!IsReady || processing)
            return;

        string key;

        if (queue.TryDequeue(out key))
            StartCoroutine(ProcessRequest(key));
    }

    private void StartListening()
    {
        if (listening || requestsRef == null)
            return;

        requestsRef.ChildAdded += OnNewRequest;
        listening = true;

        Debug.Log(
            "[VoteHost] 主機已授權，開始監聽投票"
        );
    }

    private void StopListening()
    {
        if (listening && requestsRef != null)
            requestsRef.ChildAdded -= OnNewRequest;

        listening = false;
        DrainSucceeded = false;

        // 未處理的請求仍保存在 Firebase。
        string discarded;

        while (queue.TryDequeue(out discarded))
        {
        }

        seen.Clear();
        lastAccepted.Clear();
        hostRoundId = -1;
        lockedRoundId = -1;
        lockedUids.Clear();

        Debug.LogWarning(
            "[VoteHost] 主機未授權，停止監聽及新投票處理"
        );
    }

    private void EnqueueOnce(string key)
    {
        if (!IsReady || string.IsNullOrEmpty(key))
            return;

        if (seen.Add(key))
            queue.Enqueue(key);
    }

    private void OnNewRequest(
        object sender,
        ChildChangedEventArgs args)
    {
        if (!IsReady)
            return;

        if (args.DatabaseError != null)
        {
            Debug.LogError(
                "[VoteHost] 監聽錯誤：" +
                args.DatabaseError.Message
            );
            return;
        }

        if (args.Snapshot != null &&
            args.Snapshot.Exists)
        {
            EnqueueOnce(args.Snapshot.Key);
        }
    }

    public IEnumerator DrainPendingRequests(
        float timeoutSeconds)
    {
        DrainSucceeded = false;

        if (!IsReady)
        {
            Debug.LogError(
                "[VoteHost] 主機尚未授權，無法排空"
            );
            yield break;
        }

        float deadline = Time.realtimeSinceStartup +
            Mathf.Max(5f, timeoutSeconds);

        int consecutiveEmpty = 0;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (!IsReady)
                yield break;

            var read = requestsRef.GetValueAsync();

            yield return new WaitUntil(
                () => read.IsCompleted
            );

            if (!IsReady)
                yield break;

            if (read.IsFaulted || read.IsCanceled)
            {
                Debug.LogError(
                    "[VoteHost] 盤點失敗：" +
                    read.Exception
                );

                yield return new WaitForSecondsRealtime(1f);
                continue;
            }

            bool databaseEmpty = true;

            foreach (DataSnapshot child in read.Result.Children)
            {
                databaseEmpty = false;
                EnqueueOnce(child.Key);
            }

            if (databaseEmpty && IsIdle)
            {
                consecutiveEmpty++;

                if (consecutiveEmpty >= 2)
                {
                    DrainSucceeded = true;

                    Debug.Log(
                        "[VoteHost] 投票請求排空完成"
                    );

                    yield break;
                }
            }
            else
            {
                consecutiveEmpty = 0;
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }

        Debug.LogError(
            "[VoteHost] 排空逾時，停止結算"
        );
    }

    private IEnumerator ProcessRequest(string key)
    {
        if (!IsReady)
            yield break;

        processing = true;
        bool retry = false;

        DatabaseReference reqRef =
            requestsRef.Child(key);

        var task = roomRef.GetValueAsync();

        yield return new WaitUntil(
            () => task.IsCompleted
        );

        // Firebase 請求返回後必須再次確認權限。
        if (!IsReady)
        {
            processing = false;
            yield break;
        }

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError(
                "[VoteHost] 讀取失敗：" +
                task.Exception
            );

            retry = true;
        }
        else
        {
            DataSnapshot room = task.Result;
            DataSnapshot req =
                room.Child("voteRequests").Child(key);

            DataSnapshot round =
                room.Child("currentRound");

            string uid = Get(req, "uid");
            string type = Get(req, "type");

            long id = Long(Get(round, "roundId"));
            long requestedRound =
                Long(Get(req, "roundId"));

            long requestMs =
                Long(Get(req, "requestedAt"));

            long now =
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            long voteEndsAt =
                Long(Get(round, "voteEndsAt"));

            long votingStart = voteEndsAt -
                Long(Get(round, "votingSeconds")) * 1000L;

            DataSnapshot participant =
                room.Child("participants").Child(uid);

            string phase = Get(round, "phase");

            bool eligible =
                !string.IsNullOrEmpty(uid) &&
                participant.Exists &&
                lockedRoundId == id &&
                lockedUids.Contains(uid);

            bool valid =
                req.Exists &&
                Array.IndexOf(options, type) >= 0 &&
                eligible &&
                id > 0 &&
                requestedRound == id &&
                (phase == "VOTING" || phase == "RESULT") &&
                voteEndsAt > 0 &&
                requestMs >= votingStart &&
                requestMs <= voteEndsAt &&
                requestMs > 0 &&
                requestMs <= now + 3000L;

            if (hostRoundId != id)
            {
                lastAccepted.Clear();
                hostRoundId = id;
            }

            string cooldownKey = id + ":" + uid;

            long prev;

            if (lastAccepted.TryGetValue(
                cooldownKey, out prev))
            {
                if (requestMs <= prev ||
                    requestMs - prev <
                    minVoteIntervalSeconds * 1000f)
                {
                    valid = false;
                }
            }

            if (applied.Contains(key))
                valid = false;

            if (valid)
            {
                long total = Long(
                    Get(round.Child("totals"), type)
                );

                long individual = Long(
                    Get(
                        round.Child("playerVotes").Child(uid),
                        type
                    )
                );

                var updates =
                    new Dictionary<string, object>
                {
                    {
                        "currentRound/totals/" + type,
                        total + 1
                    },
                    {
                        "currentRound/playerVotes/" +
                        uid + "/" + type,
                        individual + 1
                    }
                };

                if (!IsReady)
                {
                    processing = false;
                    yield break;
                }

                var write =
                    roomRef.UpdateChildrenAsync(updates);

                yield return new WaitUntil(
                    () => write.IsCompleted
                );

                if (!IsReady)
                {
                    processing = false;
                    yield break;
                }

                if (write.IsFaulted || write.IsCanceled)
                {
                    Debug.LogError(
                        "[VoteHost] 寫票失敗：" +
                        write.Exception
                    );

                    retry = true;
                }
                else
                {
                    lastAccepted[cooldownKey] = requestMs;
                    applied.Add(key);

                    Debug.Log(
                        "[VoteHost] +1：" + type +
                        " / " + uid
                    );
                }
            }

            if (!retry && req.Exists)
            {
                if (!IsReady)
                {
                    processing = false;
                    yield break;
                }

                var del = reqRef.RemoveValueAsync();

                yield return new WaitUntil(
                    () => del.IsCompleted
                );

                if (!IsReady)
                {
                    processing = false;
                    yield break;
                }

                if (del.IsFaulted || del.IsCanceled)
                {
                    Debug.LogError(
                        "[VoteHost] 刪票失敗：" +
                        del.Exception
                    );

                    retry = true;
                }
            }
        }

        if (retry && IsReady)
        {
            yield return new WaitForSecondsRealtime(1f);

            if (IsReady)
                queue.Enqueue(key);
        }
        else
        {
            seen.Remove(key);
            applied.Remove(key);
        }

        processing = false;
    }

    private static string Get(
        DataSnapshot snapshot,
        string key)
    {
        return snapshot != null &&
               snapshot.HasChild(key) &&
               snapshot.Child(key).Value != null
            ? Convert.ToString(snapshot.Child(key).Value)
            : "";
    }

    private static long Long(string value)
    {
        long result;

        return long.TryParse(value, out result)
            ? result : 0L;
    }

    private void OnDestroy()
    {
        if (listening && requestsRef != null)
            requestsRef.ChildAdded -= OnNewRequest;
    }
}
