
using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class FirebaseBootstrap : MonoBehaviour
{
    public static FirebaseBootstrap Instance { get; private set; }

    public bool IsReady { get; private set; }
    public bool HasFailed { get; private set; }

    private Task<DependencyStatus> dependencyTask;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[FirebaseBootstrap] 重複的初始化管理器"
            );

            enabled = false;
            return;
        }

        Instance = this;

        IsReady = false;
        HasFailed = false;

        dependencyTask =
            FirebaseApp.CheckAndFixDependenciesAsync();

        dependencyTask.ContinueWithOnMainThread(task =>
        {
            if (this == null)
                return;

            if (task.IsCanceled ||
                task.IsFaulted ||
                task.Result != DependencyStatus.Available)
            {
                HasFailed = true;

                Debug.LogError(
                    "[FirebaseBootstrap] 初始化失敗：" +
                    task.Exception
                );

                return;
            }

            IsReady = true;

            Debug.Log(
                "[FirebaseBootstrap] Firebase 初始化成功"
            );
        });
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
