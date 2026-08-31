# Installation, update, backup, and removal

## Requirements

MA-Teacher supports 64-bit Windows 10 and Windows 11. Users do not need to install the .NET SDK, Node.js, npm, or Inno Setup. The installer contains the required .NET runtime and silently installs or updates Microsoft Edge WebView2 when required. An internet connection may be needed during the first installation so Microsoft's WebView2 bootstrapper can retrieve the current runtime.

## Install

1. Download `MA-Teacher-Setup-latest.exe` and `SHA256SUMS.txt` from the latest GitHub release.
2. Run `Get-FileHash .\MA-Teacher-Setup-latest.exe -Algorithm SHA256` in PowerShell.
3. Confirm the result matches `SHA256SUMS.txt`.
4. Run the installer and keep the default per-user location or select another writable folder.
5. Launch MA-Teacher.

The installer is unsigned. A matching SHA-256 proves byte identity with the CI artifact; it does not provide code-signing identity.

## One-folder storage

The executable, UI, WebView profile, SQLite database, attachments, and backups stay below the selected installation folder. Do not install into a read-only folder. Treat the folder and every backup as sensitive.

## Update

1. Close MA-Teacher.
2. Create and verify a backup from **Manage > Database backups**.
3. Download and verify the new installer.
4. Install over the same folder.
5. Reopen MA-Teacher and check expected records.

CI proves fresh silent installation and removal. Upgrade compatibility on varied user machines still needs real feedback.

## Uninstall and troubleshooting

Use **Settings > Apps > Installed apps > MA-Teacher > Uninstall**. Back up first.

- White window: close MA-Teacher, connect to the internet, and rerun the MA-Teacher installer so it can repair WebView2.
- Port 5201 unavailable: close another MA-Teacher instance or process using the port.
- Missing records after changing folders: records belong to the original installation folder.
