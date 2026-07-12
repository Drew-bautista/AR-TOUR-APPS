using System;
using System.Threading.Tasks;
using UnityEngine;

#if FIREBASE_UNITY_SDK
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
#endif

public sealed class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public bool IsInitialized { get; private set; }
    public bool IsAvailable { get; private set; }
    public string CurrentUserId { get; private set; }
    public string LastError { get; private set; }

#if FIREBASE_UNITY_SDK
    public FirebaseAuth Auth { get; private set; }
    public FirebaseFirestore Firestore { get; private set; }
#endif

    private Task<bool> initializationTask;

    public static FirebaseManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        FirebaseManager existing = FindFirstObjectByType<FirebaseManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("FirebaseManager");
        return managerObject.AddComponent<FirebaseManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public Task<bool> EnsureInitializedAsync()
    {
        if (initializationTask != null)
        {
            return initializationTask;
        }

        initializationTask = InitializeFirebaseAsync();
        return initializationTask;
    }

    private async Task<bool> InitializeFirebaseAsync()
    {
#if FIREBASE_UNITY_SDK
        try
        {
            DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus != DependencyStatus.Available)
            {
                LastError = "Firebase dependencies unavailable: " + dependencyStatus;
                Debug.LogError(LastError);
                IsInitialized = true;
                IsAvailable = false;
                return false;
            }

            Auth = FirebaseAuth.DefaultInstance;
            Firestore = FirebaseFirestore.DefaultInstance;

            if (Auth.CurrentUser == null)
            {
                AuthResult authResult = await Auth.SignInAnonymouslyAsync();
                CurrentUserId = authResult.User.UserId;
            }
            else
            {
                CurrentUserId = Auth.CurrentUser.UserId;
            }

            IsInitialized = true;
            IsAvailable = !string.IsNullOrWhiteSpace(CurrentUserId) && Firestore != null;
            LastError = string.Empty;
            return IsAvailable;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogError("Firebase initialization failed: " + exception);
            IsInitialized = true;
            IsAvailable = false;
            return false;
        }
#else
        await Task.Yield();
        LastError = "Firebase Unity SDK not installed/enabled. Import FirebaseAuth and FirebaseFirestore, then add FIREBASE_UNITY_SDK to Scripting Define Symbols.";
        Debug.LogWarning(LastError);
        IsInitialized = true;
        IsAvailable = false;
        return false;
#endif
    }
}
