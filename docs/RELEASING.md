# Releasing to the Microsoft Store

After a one-time setup, releasing is one command: push a version tag and GitHub Actions builds the Store package, submits it to Partner Center and creates a GitHub release.

```powershell
git tag v1.0.1
git push origin v1.0.1
```

## How the pipeline works

| Workflow | Runs on | Does |
| --- | --- | --- |
| [`ci.yml`](../.github/workflows/ci.yml) | every push to `main` and every pull request | Builds the MSIX and uploads it as a build artifact |
| [`release.yml`](../.github/workflows/release.yml) | a `v*.*.*` tag, or by hand | Stamps the Store identity and version, builds the `.msixupload`, submits it to the Store, creates the GitHub release |

The tag `v1.2.3` becomes package version `1.2.3.0`; the Store requires the last number to be 0. Every release needs a version higher than the last one.

The committed `Package.appxmanifest` has placeholder identity values so anyone can build the app. The release workflow replaces them with your Partner Center values before building.

Submissions go through Store certification, which usually takes a few hours to three business days.

## One-time setup

### 1. Reserve the app in Partner Center

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard/apps-and-games/overview) with a Microsoft Store developer account (free for individuals).
2. **Apps and games → New product → MSIX or PWA app**, and reserve the name **Sonar Flyout**.
3. Open **Product management → Product identity** and note:
   - **Package/Identity/Name**, for example `RajKishore.SonarFlyout`
   - **Package/Identity/Publisher**, for example `CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX`
   - **Package/Properties/PublisherDisplayName**
   - **Store ID**, for example `9NXXXXXXXXXX`

### 2. Give GitHub access to Partner Center

1. In Partner Center: **Account settings → User management → Microsoft Entra applications → Create Microsoft Entra application** (or add an existing one). Give it the **Manager** role.
2. Create a **client secret** (key) for it.
3. Note the **Tenant ID**, **Client ID** and **client secret**, and your **Seller ID** (**Account settings → Legal info → Developer**).

### 3. Configure the GitHub repository

**Settings → Environments → New environment**, name it `microsoft-store`. Optionally add yourself as a required reviewer so every Store submission waits for your approval.

In that environment, add these **secrets**:

| Secret | Value |
| --- | --- |
| `PARTNER_CENTER_TENANT_ID` | Tenant ID |
| `PARTNER_CENTER_SELLER_ID` | Seller ID |
| `PARTNER_CENTER_CLIENT_ID` | Client ID |
| `PARTNER_CENTER_CLIENT_SECRET` | Client secret |

and these **variables**:

| Variable | Value |
| --- | --- |
| `STORE_PRODUCT_ID` | Store ID |
| `STORE_PACKAGE_NAME` | Package/Identity/Name |
| `STORE_PUBLISHER` | Package/Identity/Publisher |
| `STORE_PUBLISHER_DISPLAY_NAME` | Package/Properties/PublisherDisplayName |

### 4. Make the first submission by hand

The Store's API can only update an app that already has a published submission, so the first one goes through Partner Center:

1. **Actions → Release → Run workflow**, version `1.0.0`, leave **Submit** unticked. Download the `.msixupload` from the run's artifacts.
2. In Partner Center, start a submission and fill it in:
   - **Pricing and availability:** Free, all markets.
   - **Properties:** category **Music** or **Utilities & tools**; privacy policy URL `https://github.com/rkzofficial/SonarFlyout/blob/main/PRIVACY.md`.
   - **Age ratings:** complete the questionnaire (no user interaction, no data collection).
   - **Packages:** upload the `.msixupload`.
   - **Store listings:** the app ships in 13 languages, so Partner Center asks for a listing in each. The text for every language is in [`store/listings/`](../store/listings), and the images in [`store/images/`](../store/images); see [`store/README.md`](../store/README.md) for which image goes where.
   - **Submission options → Notes for certification:** "Requires SteelSeries GG with Sonar (free, https://steelseries.com/gg). The app is a tray icon; click it to open the mixer." Certification testers may not have Sonar, in which case the flyout shows a "Can't reach Sonar" message, which is the expected behavior.
   - The `runFullTrust` capability needs a short justification: "Desktop tray app: talks to SteelSeries GG's local server and registers a notification-area icon."
3. Submit. For this first version, create the GitHub release by hand instead of pushing a tag, so the workflow doesn't submit 1.0.0 a second time:

   ```powershell
   gh release create v1.0.0 --title "Sonar Flyout v1.0.0" --generate-notes
   ```

From then on, every tag goes all the way to the Store by itself.

### 5. Add the Store badge to the README

Once the app is live, replace "coming soon" in the README's **Install** section with:

```markdown
<a href="https://apps.microsoft.com/detail/STORE_ID"><img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200" alt="Get it from Microsoft" /></a>
```

## Updating the listing

The workflow only uploads the package; listing text and images stay as they are in Partner Center. To change them, edit the files under `store/`, then update the submission in Partner Center (or use `msstore submission updateMetadata`).

## Regenerating assets

| Change | Run |
| --- | --- |
| Tray icons or app logo | `powershell -File tools\make-assets.ps1` |
| UI translations | `python tools\translations.py` |
| Store screenshots and art (after replacing the captures in `docs\images\`) | `powershell -File tools\make-store-art.ps1` |
