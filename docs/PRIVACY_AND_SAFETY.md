# Privacy and safety boundary

Version 0.2.0 has no product cloud account, telemetry service, remote learner sync, or automatic model-based grading.

The selected install folder can contain learner and plan records, curriculum evidence, lessons, reviews, session and progress records, submitted work, feedback, backups, and WebView2 profile data. Treat all of it as sensitive.

The UI communicates with its own loopback host at `127.0.0.1:5201`. Operator-triggered curriculum capture may contact allowlisted official public sources. It is not a general web browser.

MA-Teacher does not make safeguarding, diagnosis, legal, curriculum-approval, or mastery decisions. Use synthetic data in public bug reports and private security advisories for vulnerabilities.
