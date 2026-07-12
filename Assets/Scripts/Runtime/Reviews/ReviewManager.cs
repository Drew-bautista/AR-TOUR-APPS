using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if FIREBASE_UNITY_SDK
using Firebase.Firestore;
#endif

public sealed class ReviewManager : MonoBehaviour
{
    public const string ReviewsCollectionName = "reviews";

    private const string PendingReviewKey = "AguinaldoShrine.PendingReview";
    private const string PromptShownKey = "AguinaldoShrine.ReviewPromptShown";
    private const float SyncIntervalSeconds = 8f;

    public static ReviewManager Instance { get; private set; }

    public event Action<ReviewSummary> SummaryUpdated;
    public event Action<string> StatusUpdated;
    public event Action ReviewPromptRequested;

    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private bool autoLoadOnStart = true;

    private ReviewSummary currentSummary = ReviewSummary.Empty("Reviews loading...");
    private float nextSyncAt;
    private bool syncInProgress;

    public ReviewSummary CurrentSummary => currentSummary;
    public bool HasPendingReview => PlayerPrefs.HasKey(PendingReviewKey);

    public static ReviewManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        ReviewManager existing = FindFirstObjectByType<ReviewManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("ReviewManager");
        return managerObject.AddComponent<ReviewManager>();
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
        if (firebaseManager == null)
        {
            firebaseManager = FirebaseManager.EnsureExists();
        }
    }

    private async void Start()
    {
        if (autoLoadOnStart)
        {
            await RefreshReviewsAsync();
            await TrySyncPendingReviewAsync();
        }
    }

    private async void Update()
    {
        if (!HasPendingReview || syncInProgress || Time.unscaledTime < nextSyncAt)
        {
            return;
        }

        nextSyncAt = Time.unscaledTime + SyncIntervalSeconds;
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await TrySyncPendingReviewAsync();
        }
    }

    public async Task SubmitReviewAsync(int rating, string comment)
    {
        ReviewData review = new ReviewData(GetBestKnownUserId(), rating, comment, Application.version);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            CachePendingReview(review);
            SetStatus("Review saved offline. It will sync when internet is available.");
            return;
        }

        bool ready = await firebaseManager.EnsureInitializedAsync();
        review.userId = GetBestKnownUserId();
        if (!ready)
        {
            CachePendingReview(review);
            SetStatus("Review saved locally. Firebase is not ready yet.");
            return;
        }

        try
        {
            await SaveReviewToCloudAsync(review);
            ClearPendingReview();
            SetStatus("Review submitted. Thank you!");
            await RefreshReviewsAsync();
        }
        catch (Exception exception)
        {
            CachePendingReview(review);
            SetStatus("Review saved offline. Sync will retry automatically.");
            Debug.LogWarning("Review submit failed, cached locally: " + exception.Message);
        }
    }

    public async Task RefreshReviewsAsync()
    {
        bool ready = await firebaseManager.EnsureInitializedAsync();
        if (!ready)
        {
            currentSummary = BuildOfflineSummary();
            SummaryUpdated?.Invoke(currentSummary);
            return;
        }

        try
        {
            currentSummary = await LoadSummaryFromCloudAsync();
            SummaryUpdated?.Invoke(currentSummary);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load reviews: " + exception.Message);
            currentSummary = BuildOfflineSummary();
            currentSummary.statusMessage = "Could not load cloud reviews. Showing local state.";
            SummaryUpdated?.Invoke(currentSummary);
        }
    }

    public void MarkTourCompletedAndRequestReview()
    {
        if (PlayerPrefs.GetInt(PromptShownKey, 0) == 1)
        {
            return;
        }

        PlayerPrefs.SetInt(PromptShownKey, 1);
        PlayerPrefs.Save();
        ReviewPromptRequested?.Invoke();
    }

    public async Task TrySyncPendingReviewAsync()
    {
        if (!HasPendingReview || syncInProgress)
        {
            return;
        }

        syncInProgress = true;
        try
        {
            ReviewData pendingReview = LoadPendingReview();
            if (pendingReview == null)
            {
                ClearPendingReview();
                return;
            }

            bool ready = await firebaseManager.EnsureInitializedAsync();
            if (!ready || Application.internetReachability == NetworkReachability.NotReachable)
            {
                return;
            }

            pendingReview.userId = GetBestKnownUserId();
            await SaveReviewToCloudAsync(pendingReview);
            ClearPendingReview();
            SetStatus("Offline review synced.");
            await RefreshReviewsAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Pending review sync failed: " + exception.Message);
        }
        finally
        {
            syncInProgress = false;
        }
    }

    private string GetBestKnownUserId()
    {
        if (firebaseManager != null && !string.IsNullOrWhiteSpace(firebaseManager.CurrentUserId))
        {
            return firebaseManager.CurrentUserId;
        }

        string localId = PlayerPrefs.GetString("AguinaldoShrine.LocalAnonymousUserId", string.Empty);
        if (string.IsNullOrWhiteSpace(localId))
        {
            localId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString("AguinaldoShrine.LocalAnonymousUserId", localId);
            PlayerPrefs.Save();
        }

        return localId;
    }

    private void CachePendingReview(ReviewData review)
    {
        PlayerPrefs.SetString(PendingReviewKey, JsonUtility.ToJson(review));
        PlayerPrefs.Save();
        currentSummary = BuildOfflineSummary();
        SummaryUpdated?.Invoke(currentSummary);
    }

    private ReviewData LoadPendingReview()
    {
        string json = PlayerPrefs.GetString(PendingReviewKey, string.Empty);
        return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<ReviewData>(json);
    }

    private void ClearPendingReview()
    {
        PlayerPrefs.DeleteKey(PendingReviewKey);
        PlayerPrefs.Save();
    }

    private ReviewSummary BuildOfflineSummary()
    {
        ReviewSummary summary = ReviewSummary.Empty(HasPendingReview
            ? "1 pending review will sync online."
            : "Reviews unavailable offline.");

        ReviewData pending = LoadPendingReview();
        if (pending != null)
        {
            summary.totalReviews = 1;
            summary.averageRating = pending.rating;
            summary.latestReviews.Add(pending);
        }

        return summary;
    }

    private void SetStatus(string message)
    {
        StatusUpdated?.Invoke(message);
    }

#if FIREBASE_UNITY_SDK
    private async Task SaveReviewToCloudAsync(ReviewData review)
    {
        DocumentReference document = firebaseManager.Firestore
            .Collection(ReviewsCollectionName)
            .Document(review.userId);

        await document.SetAsync(review.ToFirestoreDictionary());
    }

    private async Task<ReviewSummary> LoadSummaryFromCloudAsync()
    {
        CollectionReference reviews = firebaseManager.Firestore.Collection(ReviewsCollectionName);
        QuerySnapshot allSnapshot = await reviews.GetSnapshotAsync();
        QuerySnapshot latestSnapshot = await reviews
            .OrderByDescending("createdAt")
            .Limit(10)
            .GetSnapshotAsync();

        int total = 0;
        int ratingTotal = 0;
        foreach (DocumentSnapshot document in allSnapshot.Documents)
        {
            ReviewData review = ReviewData.FromSnapshot(document);
            if (review == null)
            {
                continue;
            }

            total++;
            ratingTotal += Mathf.Clamp(review.rating, 1, 5);
        }

        List<ReviewData> latest = new List<ReviewData>();
        foreach (DocumentSnapshot document in latestSnapshot.Documents)
        {
            ReviewData review = ReviewData.FromSnapshot(document);
            if (review != null)
            {
                latest.Add(review);
            }
        }

        return new ReviewSummary
        {
            averageRating = total == 0 ? 0f : ratingTotal / (float)total,
            totalReviews = total,
            latestReviews = latest,
            statusMessage = total == 0 ? "No reviews yet." : "Reviews loaded."
        };
    }
#else
    private async Task SaveReviewToCloudAsync(ReviewData review)
    {
        await Task.Yield();
        throw new InvalidOperationException("Firebase Unity SDK is not installed/enabled.");
    }

    private async Task<ReviewSummary> LoadSummaryFromCloudAsync()
    {
        await Task.Yield();
        return BuildOfflineSummary();
    }
#endif
}
