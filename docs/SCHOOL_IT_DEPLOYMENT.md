# School IT deployment and security guide

This guide describes MA-Teacher `0.1.0` as shipped. It is for IT administrators evaluating or deploying the Windows application. MA-Teacher is a local-first public preview, not a cloud service, safeguarding platform, automatic marking system, or school management system.

## Short answer

- Supported client: 64-bit Windows 10 or Windows 11.
- End-user dependencies: none installed manually. Setup contains the .NET runtime and carries Microsoft's WebView2 bootstrapper.
- Local port: TCP `5201` on `127.0.0.1` only.
- Inbound network rule: none. Do not expose TCP `5201` to the LAN, VPN, Wi-Fi, or internet.
- Outbound network: HTTPS TCP `443` for first-time WebView2 installation, installer retrieval, and optional curriculum-source capture.
- Data location: below the selected MA-Teacher installation folder, including SQLite records, attachments, backups, and the WebView2 profile.
- Authentication: no multi-user or network authentication boundary. Windows account and NTFS isolation are required.
- Encryption at rest: not provided by MA-Teacher. Use BitLocker or the organisation's managed full-disk encryption.
- Code signing: the current installer is not Authenticode-signed. Verify `SHA256SUMS.txt` and use hash-based application control.
- Update model: IT-managed replacement from the fixed GitHub `0.1.0` release. There is no unattended in-app updater.

## Application-control allowlist

Prefer a hash rule for each verified release artifact. Do not create a broad folder, publisher, PowerShell, or user-writable-path exemption.

Allow these product files when their hashes match the approved release:

| File or process | Why it is needed |
| --- | --- |
| `MA-Teacher-Setup-latest.exe` | Per-user Inno Setup installer. |
| `MA-Teacher.exe` | WPF desktop host, local loopback API, and SQLite owner. |
| `WebView2Loader.dll` | Microsoft WebView2 loader shipped with the app. |
| `MicrosoftEdgeWebview2Setup.exe` | Microsoft-signed bootstrapper embedded in setup and run only when WebView2 is absent. |
| `%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\<version>\msedgewebview2.exe` | Microsoft WebView2 runtime process used to render the packaged interface. |
| `%LOCALAPPDATA%\Temp\is-*\MA-Teacher-Setup.tmp` | Short-lived Inno extraction process during installation only. Scope any exception to the verified parent installer and installation window. |

The app does not require `dotnet.exe`, `node.exe`, `npm`, Inno Setup, a compiler, a browser extension, PowerShell execution, or administrator rights at runtime.

## Ports and firewall policy

| Direction | Protocol and destination | Requirement | Safe policy |
| --- | --- | --- | --- |
| Local only | TCP `127.0.0.1:5201` | Required while MA-Teacher is open | Permit the MA-Teacher process to listen on loopback only. Do not create a remote inbound rule. |
| Outbound | HTTPS TCP `443` | Required if WebView2 is missing; optional for updates and curriculum capture | Allow only approved destinations through the school proxy or firewall. |
| Inbound LAN/WAN | Any address, including TCP `5201` | Not required | Block. |
| UDP, multicast, discovery | Any | Not required | Block by default. |

Endpoint protection may describe the loopback listener as a local server. That is expected. The source binds the `HttpListener` prefix `http://127.0.0.1:5201/`; it does not bind `0.0.0.0`, `localhost`, the device LAN address, or an IPv6 wildcard.

Do not port-forward `5201`, add a Windows Firewall inbound allow rule, publish it through a reverse proxy, or place it behind school single sign-on. The API was designed for the packaged interface on the same Windows account, not remote clients. Mutation routes check the loopback origin and a specific intent header, but a hostile native process running as the same Windows user is still inside the local trust boundary.

## Outbound HTTPS allowlist

Keep the allowlist destination-based and limited to TCP `443`. TLS inspection must retain normal certificate validation and must not replace failed validation with an allow-all exception.

Required only when WebView2 is absent:

| Destination | Purpose |
| --- | --- |
| `msedge.api.cdp.microsoft.com` | Microsoft WebView2 bootstrapper service metadata. |
| `msedge.sf.dl.delivery.mp.microsoft.com` | Microsoft WebView2 runtime delivery. |
| `*.dl.delivery.mp.microsoft.com` | Microsoft delivery endpoints selected by the signed Evergreen bootstrapper. Use the organisation's existing Microsoft Edge Update policy where possible. |

Required on the administrator's download or software-distribution system, not necessarily every learner device:

| Destination | Purpose |
| --- | --- |
| `github.com` | MA-Teacher release page and source. |
| `release-assets.githubusercontent.com` | GitHub release installer assets. |
| `objects.githubusercontent.com` | GitHub-hosted release objects where selected by GitHub. |

Optional current curriculum-source destinations:

| Destination | Current catalogue purpose |
| --- | --- |
| `www.gov.uk` | England statutory and qualification material. |
| `assets.publishing.service.gov.uk` | Documents linked from GOV.UK. |
| `education.gov.scot` | Scotland curriculum material. |
| `www.gov.wales` and `hwb.gov.wales` | Wales curriculum and guidance. |
| `www.education-ni.gov.uk` and `www.nidirect.gov.uk` | Northern Ireland curriculum material. |
| `skillsengland.education.gov.uk` | Skills England occupational maps. |
| `www.thenational.academy` and `support.thenational.academy` | Optional teaching references and rights guidance. |
| `educationendowmentfoundation.org.uk` | Optional evidence and guidance references. |

MA-Teacher remains usable without the optional curriculum destinations, but source refresh or document capture for blocked sites will fail. Do not grant unrestricted web access merely to hide those failures. Review and approve each additional HTTPS source before use.

## Recommended managed installation

Deploy in the interactive user's context because the default install and data root is per-user:

```powershell
MA-Teacher-Setup-latest.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /DIR="$env:LOCALAPPDATA\Programs\MA-Teacher"
```

Do not run that command as `SYSTEM` unless the deployment platform deliberately substitutes the intended user's path. A system-context install would place the app under the system profile and make it unavailable or confusing for the learner.

The selected folder must be writable by that user because MA-Teacher deliberately keeps its database, attachments, backups, and WebView2 profile under one root. Do not install it in a read-only `Program Files` location. Do not place the live database on OneDrive, a roaming profile, a synchronised folder, or a network share; SQLite and WebView2 require reliable local filesystem semantics.

For shared devices, use separate managed Windows accounts. Do not give several learners one shared Windows login. MA-Teacher `0.1.0` does not provide an internal authentication boundary between people using the same Windows account.

Silent removal:

```powershell
& "$env:LOCALAPPDATA\Programs\MA-Teacher\unins000.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

Back up and verify required records before removal. Uninstall behavior and local retention policy should be tested against the school's requirements before broad deployment.

## Data protection baseline

- Enable BitLocker or the organisation's equivalent managed full-disk encryption.
- Apply NTFS access so only the intended Windows user, authorised administrators, and required backup service can read the installation root.
- Treat `data\ma-teacher.db`, `data\attachments`, `data\backups`, and `data\webview` as sensitive learner information.
- Do not email, ticket, or attach a real database, backup, or learner submission to a public GitHub issue.
- Use synthetic data for bug reports.
- Define a retention period before use. MA-Teacher does not currently enforce a school policy automatically.
- Keep endpoint protection enabled. Prefer narrow hash or process rules over antivirus folder exclusions.
- Use separate least-privilege user accounts; no administrator membership is required for normal use.
- Restrict access to curriculum capture and learner records according to the school's role model and safeguarding policy.
- Confirm whether local storage and the public-preview status satisfy the school's DPIA, records-management, safeguarding, and procurement processes.

## Acceptance checks

1. Download the installer and `SHA256SUMS.txt` from the `0.1.0` GitHub release using a managed administrator workstation.
2. Compare `Get-FileHash .\MA-Teacher-Setup-latest.exe -Algorithm SHA256` with the published checksum.
3. Scan the installer with the organisation's endpoint protection before deployment.
4. Install for a disposable non-administrator test account.
5. Open MA-Teacher and confirm the interface, logo, learner setup, lesson workflow, work submission, manual review, backup, and restore-boundary messaging.
6. Run `Get-NetTCPConnection -State Listen -LocalPort 5201` while the app is open and confirm `LocalAddress` is exactly `127.0.0.1`.
7. Match `OwningProcess` to `MA-Teacher.exe` with `Get-Process -Id <pid>`.
8. From another device, verify `Test-NetConnection <test-device-ip> -Port 5201` fails.
9. Confirm no inbound Windows Firewall allow rule was created for MA-Teacher.
10. Confirm files are written only beneath the selected install root during ordinary use.
11. Check NTFS permissions with `icacls <install-root>` and verify another standard user cannot read the learner data.
12. Create and verify an in-app backup, then test the school's protected backup handling without publishing real data.
13. Close MA-Teacher and confirm TCP `5201` is no longer listening.
14. Test silent uninstall and the school's chosen retain-or-remove-data process on the disposable account.

## Incident and support handling

For a reproducible product bug, use the public issue template with synthetic data only. For a vulnerability or any report involving a path to learner information, use a private GitHub security advisory. Record the MA-Teacher version, installer SHA-256, Windows build, WebView2 version, relevant policy rule, exact observed result, and whether the test used synthetic data.

The installer is currently unsigned. A checksum proves that a file matches the CI-published artifact; it does not prove publisher identity in the way Authenticode would. Schools that require signed applications should treat code signing as a deployment blocker rather than weakening application-control policy.
