# Firebase Rate and Review Setup

This project now has a cloud review system for **Aguinaldo Shrine AR Smart Tour Guide**.

## Firebase Console

1. Open Firebase Console and select project `aguinaldoshrinear`.
2. Confirm the Android app package name is `com.aguinaldoshrine.artour`.
3. Enable **Authentication > Sign-in method > Anonymous**.
4. Enable **Firestore Database**.
5. Create/use the Firestore collection named `reviews`.
6. Publish the rules from [firestore.rules](firestore.rules).

## Unity SDK Install

1. Download the Firebase Unity SDK from Firebase official Unity setup docs.
2. Import these `.unitypackage` files:
   - `FirebaseAuth`
   - `FirebaseFirestore`
3. In Unity, open **Project Settings > Player > Other Settings > Scripting Define Symbols**.
4. Add:
   - `FIREBASE_UNITY_SDK`
5. Keep [Assets/google-services.json](Assets/google-services.json) in the `Assets` folder.

Without the define, the review UI still compiles and caches pending reviews locally, but cloud sync is disabled.

## Android/Gradle Note

Firebase Unity SDK usually manages Android dependencies through External Dependency Manager for Unity. If you later enable custom Gradle templates manually, the Firebase Android plugin/dependency notes provided by Firebase are:

```gradle
plugins {
    id("com.google.gms.google-services") version "4.5.0" apply false
}
```

```gradle
plugins {
    id("com.android.application")
    id("com.google.gms.google-services")
}

dependencies {
    implementation(platform("com.google.firebase:firebase-bom:34.15.0"))
    implementation("com.google.firebase:firebase-analytics")
}
```

For this Unity project, prefer the Firebase Unity SDK import first.

## Runtime Hierarchy

The UI is built automatically at runtime.

Home scene:

```text
Canvas
  ReviewUI
    ReviewSummaryPanel
      Average rating
      Total reviews
      Latest 10 reviews
      Rate button
    ReviewPanelOverlay
      ReviewCard
        Five stars
        TextMeshPro comment input
        Submit button
        Cancel button
```

AR tour scene:

```text
Canvas
  ReviewUI
    ReviewPanelOverlay
      ReviewCard
```

Managers are created automatically:

```text
FirebaseManager
ReviewManager
```

## Script Responsibilities

- `FirebaseManager.cs`: initializes Firebase, anonymous auth, exposes Firestore.
- `ReviewManager.cs`: submit/update review, load average/count/latest 10, cache pending offline review, sync when online.
- `ReviewData.cs`: serializable review model and Firestore mapping.
- `ReviewUIController.cs`: builds Home summary and popup UI.
- `StarRatingController.cs`: five clickable star behavior.

## Event Hooks

- `PremiumHomeUIController.BuildAll()` calls `ReviewUIController.AttachToCanvas(canvas, true)`.
- `NavigationManager.CompleteTour()` calls `ReviewUIController.EnsurePopupInScene()` then `ReviewManager.MarkTourCompletedAndRequestReview()`.
- Review documents are saved as `reviews/{userId}`, so each anonymous user has only one review and updates overwrite the old document.

## Stored Firestore Fields

Each document in `reviews` contains:

```text
userId
rating
comment
createdAt
appVersion
```
