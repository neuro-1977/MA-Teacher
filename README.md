# MA-Teacher

<p align="center">
  <img src="icon-large.png" alt="MA-Teacher potato-shaped tutor wearing a graduation cap" width="280">
</p>

<p align="center"><strong>Built by Serenity, with engineering assistance from OpenAI Codex.</strong></p>

[![Windows release](https://github.com/neuro-1977/MA-Teacher/actions/workflows/windows-release.yml/badge.svg)](https://github.com/neuro-1977/MA-Teacher/actions/workflows/windows-release.yml)
[![Latest installer](https://img.shields.io/badge/download-Windows%20installer-1d6f66)](https://github.com/neuro-1977/MA-Teacher/releases/latest)

MA-Teacher is a local-first Windows teaching workspace for planning evidence-linked lessons, recording supervised learning, accepting student work, and keeping human feedback and progress evidence together.

**Quick links:** [Download](https://github.com/neuro-1977/MA-Teacher/releases/tag/0.1.0) · [Student guide](docs/STUDENT_GUIDE.md) · [Teacher guide](docs/TEACHER_GUIDE.md) · [School IT guide](docs/SCHOOL_IT_DEPLOYMENT.md) · [What changed](CHANGELOG.md) · [Share feedback](https://github.com/neuro-1977/MA-Teacher/discussions) · [Report a bug](https://github.com/neuro-1977/MA-Teacher/issues/new/choose)

Version `0.1.0` is a public preview. It is usable for feedback, but it is not a finished curriculum, learning-management system, safeguarding service, or automatic AI tutor.

## What works

- Create local learner and study-plan records.
- Inspect registered curriculum sources and evidence status.
- Prepare, review, approve, open, and print evidence-linked lessons.
- Create practice checks.
- Submit typed work or one PDF, office document, text file, or image up to 10 MB.
- Review work manually with `met`, `partially met`, `not yet`, or `invalid`, plus written feedback.
- Record teaching sessions and progress evidence without inventing scores.
- Create and verify local database backups.
- Keep the application, database, WebView data, and assets under the chosen install folder.
- Start in a calm Simple view, with the full planning and evidence toolkit available in Teacher view.
- Explore fourteen subject guides and activity-only progress trail markers without invented grades.

MA-Teacher does **not** automatically grade work, recognise handwriting, diagnose learning needs, guarantee curriculum accuracy, or send learner records to a cloud service.

## Install on Windows

1. Open the [latest release](https://github.com/neuro-1977/MA-Teacher/releases/latest).
2. Download `MA-Teacher-Setup-latest.exe` and `SHA256SUMS.txt`.
3. Compare the installer SHA-256 with the checksum file.
4. Run the installer and choose a location you can write to.
5. Start MA-Teacher from the Start menu or desktop shortcut.

The installer is unsigned, so Windows SmartScreen may show an unknown-publisher warning. Do not install a file whose checksum does not match the release.

Teachers and students use the **same installer**. The teacher or operator sets up learners, plans, lessons, checks, and reviews. Students use the supervised lesson and submission surfaces on that installation.

- [Installation and updates](docs/INSTALLER.md)
- [Teacher guide](docs/TEACHER_GUIDE.md)
- [Student guide](docs/STUDENT_GUIDE.md)
- [Privacy and safeguarding boundaries](docs/PRIVACY_AND_SAFETY.md)
- [Why the interface uses layered, approachable language](docs/APPROACHABILITY.md)
- [Safe Code Lab security model](docs/SAFE_CODE_LAB.md)

## Feedback

Use [GitHub Issues](https://github.com/neuro-1977/MA-Teacher/issues) for reproducible bugs and workflow feedback. Never post real learner names, submitted work, school records, credentials, or a copied MA-Teacher database. Use synthetic examples.

Teachers and supervised students may comment on existing issues or use [GitHub Discussions](https://github.com/neuro-1977/MA-Teacher/discussions) for general product feedback. Current issue bodies, labels, and comments can be imported idempotently into MA-Teacher's local development feedback queue so Serenity can review them before planning future work.

Security and privacy reports belong in a [private security advisory](https://github.com/neuro-1977/MA-Teacher/security/advisories/new).

## Build and verify

End users need only 64-bit Windows 10 or Windows 11 and the MA-Teacher installer. Setup includes the .NET runtime and installs WebView2 when required.

To build from source, developers need the .NET 9 SDK, Node.js 22, and Inno Setup 6.

    ./scripts/verify-public-release.ps1

That command scans the public boundary, type-checks and builds the UI, builds the desktop host and installer, self-tests the published and installed payloads, silently uninstalls, and emits installers plus checksums under `artifacts/`.

See [development and CI](docs/DEVELOPMENT.md) and [architecture](docs/ARCHITECTURE.md).

School deployment teams should use the [school IT deployment and security guide](docs/SCHOOL_IT_DEPLOYMENT.md) for the exact port, firewall, application-control, outbound-domain, storage, privacy, and acceptance-test requirements.

## Licence

No open-source licence has been granted yet. The source is publicly visible for evaluation and feedback; copyright remains with CaptainNeuro. Third-party components retain their own licences in [THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md).
