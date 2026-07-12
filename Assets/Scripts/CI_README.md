CI Build instructions (GitHub Actions)

Goal: produce an Android APK automatically using GitHub Actions.

Files created in this repo:
- Assets/Scripts/BuildCommand.cs  -- static method BuildCommand.PerformAndroidBuild invoked by Unity CLI
- Assets/Scripts/android-ci-build.yml -- workflow text (copy to .github/workflows/android-ci-build.yml)

Steps to enable CI build:
1. Copy the workflow file to .github/workflows/android-ci-build.yml
   - mkdir -p .github/workflows
   - cp Assets/Scripts/android-ci-build.yml .github/workflows/android-ci-build.yml

2. Create GitHub repository and push your project (or add this repo to GitHub if already).

3. Add GitHub Secrets (Repository -> Settings -> Secrets & variables -> Actions):
   - KEYSTORE_BASE64 : base64-encoded JKS keystore (use: base64 -w0 yourkey.jks | pbcopy)
   - KEYSTORE_PASS : keystore password
   - KEY_ALIAS : alias name inside keystore
   - KEY_ALIAS_PASS : alias password
   - BUNDLE_ID : com.yourcompany.aguinaldoshrine (optional, overrides PlayerSettings)
   - (Optional) UNITY_LICENSE : base64-encoded Unity license .ulf file for activation

4. Trigger the workflow (Actions tab -> Build Android APK -> Run workflow) or push to main.

5. When workflow finishes, download the APK artifact from the workflow run (Actions -> run -> Artifacts).

Local alternative (Unity Editor):
- Open project in Unity Editor.
- Menu -> Tools -> Build -> Android APK (provided script) — configure options and click Build APK.

Notes & troubleshooting:
- The workflow uses game-ci/unity-setup to install Unity. If your project requires a specific Unity version, edit the workflow unityVersion value.
- If build fails due to missing scenes, ensure Home and AR scenes are in File -> Build Settings and checked.
- If you prefer I set up the GitHub repo + secrets for you, I can prepare the workflow and instructions, but I cannot upload secrets or run GitHub actions myself; you must add the secrets in GitHub.

Need help copying the workflow to .github or configuring secrets? Say "do it for me" and I will produce exact shell commands to run locally to move files and encode keystore/license.
