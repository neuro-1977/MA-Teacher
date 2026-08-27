# MA-Teacher

Private, installable Windows foundation for an all-age learning companion owned
by CaptainNeuro. The current release is a truthful shell, not a claim that the
curriculum, teaching, assessment, or learner-data systems already exist.

## Current release: 0.1.0

- self-contained x64 WPF desktop application;
- React/Vite interface embedded into the application package;
- local HTTP identity and static-content host on `127.0.0.1:5201`;
- WebView2 state kept beneath the selected MA-Teacher install folder;
- dedicated MA-Teacher artwork and executable/installer metadata;
- one-folder, selectable-location Inno Setup installation;
- no Windows service, autostart task, cloud account, or background agent.

## Intended learning breadth

MA-Teacher is intended to support learners and teachers across age groups and
across science, English, maths, history, languages, and information technology.
This breadth is a product direction, not a claim of current lesson coverage.

Future curriculum and lesson content must be evidence-backed. The current
English National Curriculum is the first governing curriculum lane. Official
government curriculum publications outrank summaries; exam-board and awarding-
body material is used only within its actual stage and qualification scope.

## Deliberately absent from 0.1.0

- learner or teacher accounts and personal records;
- imported curriculum or lesson-plan corpus;
- assessment, grading, progress inference, or automated tutoring;
- safeguarding decisions or unsupervised learner-facing agents;
- cloud sync, public deployment, or Mostly Armless runtime dependency.

## Build the installer

Prerequisites: .NET 9 SDK, Node.js/npm, and Inno Setup 6.

```powershell
.\Installer\build-installer.ps1
```

The build compiles the web bundle, publishes the self-contained Windows shell,
compiles the Inno installer, verifies its payload, and copies the final artifact
to `D:\MA-Updates\MA-Teacher-Setup-latest.exe`.

See `docs/CONCEPT.md` for product boundaries and `docs/INSTALLER.md` for the
packaging, installation, rollback, and proof contract.
