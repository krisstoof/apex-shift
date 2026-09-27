# Unity CI

Workflow `.github/workflows/unity-tests.yml` runs separate EditMode and PlayMode test jobs on pushes to `main`, pull requests, and manual dispatch. It uses Unity `6000.6.2f1` with `game-ci/unity-test-runner@v4`; it does not build or publish the game. Git LFS is enabled at checkout, test outputs are uploaded as artifacts for 14 days, and GameCI publishes test results as GitHub Checks.

## Required repository secrets

Configure these three repository secrets:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

For Unity Personal, `UNITY_LICENSE` must contain the contents of an active Unity license `.ulf` file. On Windows, the usual location is:

```text
C:\ProgramData\Unity\Unity_lic.ulf
```

Add each secret in GitHub:

1. Open the `apex-shift` repository.
2. Go to **Settings → Secrets and variables → Actions**.
3. Choose **New repository secret** and add each name/value pair.

Do not commit license files, account email/password, or secret values to the repository. GitHub does not pass repository secrets to workflows triggered by pull requests from external forks; the CI job intentionally skips those runs.

## Run manually

1. Open the repository’s **Actions** tab.
2. Select **Unity Tests**.
3. Choose **Run workflow**.

Each run reports EditMode and PlayMode separately. The PlayMode workflow summary includes `[VegetationPerfBaseline]` from test artifacts/logs when available; absence of that optional performance line does not fail the workflow. Performance numbers are informational—CI has no fixed FPS threshold.

Artifacts are named `unity-editmode-results` and `unity-playmode-results` and are retained for 14 days. Unity test failures and compilation failures fail their respective jobs.
