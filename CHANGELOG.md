# Changelog

## 0.1.0 public preview

- Fixed Guided Setup so an interrupted local response cannot expose raw JavaScript JSON parser text: the host now returns a valid, privacy-safe JSON error, the page validates response bodies before parsing, and the packaged self-test checks all four setup APIs.
- Corrected sixteen local stores that accidentally appended a second `data` directory. Every feature now uses the canonical install-root `data/ma-teacher.db`; a transactional startup migration copies and verifies legacy `data/data/ma-teacher.db` rows before removing the stale nested database.
- Expanded contextual-vocabulary, descriptive-feedback, evidence-questioning, and synthetic worked-example planning from six to fourteen subject domains, with models, non-examples, observable-evidence triggers, learner-owned actions, follow-ups, bounded human reviews, next-evidence steps, and explicit cautions.
- Added a feedback-to-retry loop that returns learners to the matching practice check while preserving every earlier attempt and human review, and aligned the answer length with the server's 10,000-character boundary.
- Added a learner classroom trail that shows lesson, practice, submitted-work, and human-feedback activity from real records, plus one clear next action without grades, ranks, streaks, or mastery claims.
- Added a learner-friendly Safe Code Lab that runs bounded JavaScript in a disposable Web Worker with network, storage and host access blocked.
- Enforced an official learning-source allowlist before curriculum downloads and at every redirect destination; news, social, search and arbitrary hosts fail closed.
- Added high-confidence learner text moderation with durable privacy-minimised teacher reports for profanity, explicit content, slurs, unsafe links, obfuscation and safety-bypass attempts; learner internet search remains unavailable.
- Added Windows printer detection and a durable learner-request/teacher-approval queue for generated lesson, feedback and teacher safety reports; learner files and markup never enter the spooler.
- Added a separate opt-in TCP 5202 classroom relay and child-facing lesson/check/feedback surface; installer firewall/URL reservation and real-device school-network proof remain an explicit release blocker.

- Added task-led navigation instead of an endless panel wall.
- Added local learner, plan, curriculum, lesson, review, session, and progress records.
- Added typed and file work submission with SQLite BLOB storage and SHA-256 verification.
- Added human marking without automatic grading claims.
- Added standalone product identity and removed unrelated runtime coupling.
- Added teacher, student, installer, privacy, contributor, and architecture guides.
- Added packaged self-test, silent installer lifecycle testing, leakage scanning, CI artifacts, and immutable GitHub releases.
- Added a Simple view for learners and a separate Teacher view for planning, safety, evidence, and advanced controls.
- Added one-click header links for help, feedback, student and teacher guides, privacy, and this changelog.
- Rewrote the main journey and progress screens in plain language designed to be understood from about age nine, while allowing lesson language to grow with the learner.
- Added activity-only trail markers and progress infographics that never claim grades, rank, ability, or mastery.
- Added a privacy-safe feedback hub so students and teachers can shape future development through Issues and Discussions.
- Expanded subject planning guidance from six to fourteen domains, including geography, arts, music, physical education, citizenship, wellbeing, life skills, and religion or philosophy.
