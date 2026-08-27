# MA-Teacher Installer Contract

## Artifact

The canonical local release alias is `D:\MA-Updates\MA-Teacher-Setup-latest.exe`.
It is built by `Installer\build-installer.ps1`. A filename is not proof of
freshness; the receipt must include repository commit, byte length, SHA-256, and
synchronization state.

## Installation ownership

- The Captain chooses the installation folder.
- Application binaries, packaged UI, WebView2 state, and runtime data stay beneath that folder.
- Setup creates no Windows service, scheduler, startup entry, firewall rule, remote listener, or AppData data root.
- The application binds only to `127.0.0.1:5201`.
- The install-root `data` directory is writable beneath a protected Program Files location.

## Upgrade and rollback

Inno uses a stable AppId so rerunning a newer setup upgrades the existing product.
Windows Restart Manager closes the application when files must be replaced.
Version 0.1.0 has no learner-data migration. Persistent data requires backup,
schema migration, rollback, and restart-survival proof before introduction.

## Build stages

1. Build and inspect the Vite output references.
2. Publish the WPF shell for `win-x64` as a self-contained application.
3. Verify the executable and embedded `ui\index.html` payload.
4. Compile the Inno definition and copy the exact artifact to `D:\MA-Updates`.
5. Report SHA-256 and artifact length.

Build success proves compilation and package construction. It does not prove
interactive install, first launch, uninstall, upgrade, or curriculum accuracy.
