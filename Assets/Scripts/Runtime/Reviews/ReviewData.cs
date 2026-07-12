using System;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_UNITY_SDK
using Firebase.Firestore;
#endif

[Serializable]
public sealed class ReviewData
{
    public string userId;
    [Range(1, 5)] public int rating = 5;
    [TextArea(2, 5)] public string comment;
    public string createdAtIso;
    public string appVersion;

    public ReviewData()
    {
    }

    public ReviewData(string userId, int rating, string comment, string appVersion)
    {
        this.userId = userId;
        this.rating = Mathf.Clamp(rating, 1, 5);
        this.comment = string.IsNullOrWhiteSpace(comment) ? string.Empty : comment.Trim();
        this.appVersion = string.IsNullOrWhiteSpace(appVersion) ? Application.version : appVersion;
        createdAtIso = DateTime.UtcNow.ToString("o");
    }

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return "Visitor";
            }

            int visibleChars = Mathf.Min(6, userId.Length);
            return "Visitor " + userId.Substring(0, visibleChars).ToUpperInvariant();
        }
    }

    public string DisplayDate
    {
        get
        {
            if (DateTime.TryParse(createdAtIso, out DateTime parsed))
            {
                return parsed.ToLocalTime().ToString("MMM d, yyyy");
            }

            return "Just now";
        }
    }

#if FIREBASE_UNITY_SDK
    public Dictionary<string, object> ToFirestoreDictionary()
    {
        return new Dictionary<string, object>
        {
            { "userId", userId },
            { "rating", Mathf.Clamp(rating, 1, 5) },
            { "comment", comment ?? string.Empty },
            { "createdAt", FieldValue.ServerTimestamp },
            { "appVersion", string.IsNullOrWhiteSpace(appVersion) ? Application.version : appVersion }
        };
    }

    public static ReviewData FromSnapshot(DocumentSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists)
        {
            return null;
        }

        Dictionary<string, object> data = snapshot.ToDictionary();
        ReviewData review = new ReviewData
        {
            userId = GetString(data, "userId", snapshot.Id),
            rating = Mathf.Clamp(GetInt(data, "rating", 0), 1, 5),
            comment = GetString(data, "comment", string.Empty),
            appVersion = GetString(data, "appVersion", string.Empty),
            createdAtIso = DateTime.UtcNow.ToString("o")
        };

        if (data.TryGetValue("createdAt", out object createdAt) && createdAt is Timestamp timestamp)
        {
            review.createdAtIso = timestamp.ToDateTime().ToString("o");
        }

        return review;
    }

    private static string GetString(Dictionary<string, object> data, string key, string fallback)
    {
        return data.TryGetValue(key, out object value) && value != null ? value.ToString() : fallback;
    }

    private static int GetInt(Dictionary<string, object> data, string key, int fallback)
    {
        if (!data.TryGetValue(key, out object value) || value == null)
        {
            return fallback;
        }

        try
        {
            return Convert.ToInt32(value);
        }
        catch (Exception)
        {
            return fallback;
        }
    }
#endif
}

[Serializable]
public sealed class ReviewSummary
{
    public float averageRating;
    public int totalReviews;
    public string statusMessage;
    public List<ReviewData> latestReviews = new List<ReviewData>();

    public static ReviewSummary Empty(string status)
    {
        return new ReviewSummary
        {
            averageRating = 0f,
            totalReviews = 0,
            statusMessage = status,
            latestReviews = new List<ReviewData>()
        };
    }
}
