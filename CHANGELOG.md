# Changelog

## 0.1.0 - Learner-safe practice and work submission

- Simple view now shows only the learner's current question, answer/file submission and a short explanation of what happens next.
- Check authoring, human-review controls, currency data, submitted-work ledgers, stable IDs, database wording and SHA-256 evidence remain available in Teacher view only.
- Learner submissions receive a generated local ID, stay on the teacher's computer and remain explicitly human-reviewed rather than automatically marked.
- The Simple effect badge now says `SAVES YOUR WORK` instead of exposing database language.
- Permanent shell and learning-check contracts prevent teacher-only practice controls from leaking back into Simple view.

## Classroom web asset budget

- Preserved the full 1254 px MA-Teacher artwork as the installer and documentation master while giving all three web shells a dedicated 256 px runtime logo.
- Removed roughly 2 MB from the classroom's required image payload without changing the product identity or learner-facing design.
- Added a CI contract that rejects oversized, incorrectly dimensioned, or accidentally restored master-logo imports before the fixed `0.1.0` installer can be published.

## 0.1.0 - Reliable human marking workspace

- The teacher marking workspace now rejects empty or malformed local responses with bounded explanations instead of exposing raw JSON failures.
- Refresh now drops deleted lesson selections and already-reviewed attempt selections rather than retaining stale hidden values.
- Lesson-detail requests are generation-safe: a slower response for an earlier selection cannot overwrite the teacher's newer choice.
- A permanent contract now binds the UI attachment list to the server allowlist and preserves the 10 MB, SHA-256 integrity, and human-only marking boundaries.

## 0.1.0 - Learner classroom state freshness

- An active learner classroom now checks server authority every five seconds and whenever its window regains focus, so stopped sharing or revoked access removes stale lesson content without waiting for another learner action.
- First-time entry still gives the ordinary code prompt, while a learner whose active classroom was ended receives a clear, non-technical explanation and can request a new code.
- Lesson sections are now sorted from a copy during rendering instead of mutating the authoritative lesson payload.
- The permanent classroom journey contract proves refresh cleanup, teacher-ended guidance, and immutable lesson rendering.

## 0.1.0 - Learner classroom response safety

- The learner classroom now treats empty or malformed local API replies as a recoverable classroom problem instead of allowing a raw JSON parsing error to escape.
- Lesson refresh, joining, work submission, and print requests all use the same bounded response reader while keeping their existing child-friendly guidance and human-authority boundaries.
- The permanent classroom journey contract now rejects direct `response.json()` parsing in the learner surface and proves all four JSON endpoints use the guarded boundary.

- Simple view now enforces its learner-safe destination list at startup, browser history, internal navigation and clicks; teacher-only setup diagnostics stay hidden until Teacher view is selected.

- Classroom sharing now presents a plain three-step teacher journey with distinct ready, waiting, joined and IT-check states, visible invite/learner counts, one-use-code wording, and an explicit stop action that signs learners out and revokes every invite.

- Release proof now exercises the real classroom relay end to end on an ephemeral loopback listener: teacher invite, learner join, one-use-code refusal, scoped lesson/check read, persisted work submission, and immediate teacher revocation. Production still uses opt-in TCP 5202 and still requires the documented elevated second-device acceptance test.

- Subject exploration now gives a short age-stage approach cue, keeps its fourteen-domain learner activities compact, and reserves dense curriculum/planning notes for Teacher view without presenting practice ideas as official curriculum.

- Progress now keeps the full answer-and-feedback ledger in Teacher view, preserves the internal All subjects filter value, and verifies that trail markers remain gentle activity recognition rather than grades, rankings or streak pressure.
- Feedback now keeps developer commands and storage jargon out of the learner journey, checks common account, age, class, teacher and obfuscated rude-word disclosures, and makes the responsible adult's final decision to post explicit. Public issue forms mirror the expanded privacy boundary.

## 2026-09-01 - Calm, evidence-based learner progress

- Rebuilt Explore subjects as a single-subject learner journey instead of a long teacher catalogue: fourteen compact choices, one stage-aware practice idea, one way to show learning, and teacher notes kept closed until requested.
- Separated Safe Code Lab from Subject guidance and registered it as its own local-sandbox workspace so each page has one clear job.
- Added a permanent fourteen-domain subject-explorer contract to the production build.
- Refreshed the Windows release workflow to the official Node 24 action-runtime generations.
- Added a clear next-step card derived only from saved work and human-review evidence.
- Added friendly empty-state guidance and explicit `Reached` / `Not yet` marker text so progress is not communicated by colour or symbols alone.
- Kept trail markers non-competitive: no streaks, leaderboards, invented scores, ability guesses, unlocks, or automated grades.
- Replaced raw JSON failures with plain-language local-service messages that say no learning work was changed.
- Preserved the complete teacher evidence ledger and the Simple-to-Teacher navigation path.

## 0.1.0 learner-startup refresh - 2026-09-01

- Kept the full Teacher workspace while moving its individual panels behind on-demand loading boundaries.
- Reduced the initial production JavaScript from approximately 665 kB to 251 kB; learner and teacher destinations now load as independent chunks when opened.
- Added a calm, accessible loading card that says saved work is not changing and makes no timer or completion claim.
- Added a build-time Simple-shell contract that refuses eager panel imports, missing accessibility markers, collapsed chunk ownership or an oversized initial entry.
- Browser-checked Simple home, Guided setup, Teacher navigation, Advanced workspace index and safe return from Advanced to Simple.
- Recorded the boundary and evidence in [Simple and Teacher loading boundary](docs/SIMPLE_AND_TEACHER_LOADING.md).

## 0.1.0 public preview

- Added an explicit, administrator-only installer option for classroom browser links. It creates only the exact TCP 5202 URL reservation and MA-Teacher executable firewall rule on Domain/Private profiles, records ownership, rolls back partial failure, and removes only its owned entries on uninstall; Public networks remain blocked.
- Replaced direct learner-facing public feedback links with a three-step local draft, privacy check, and responsible-adult review gate. The page makes no API call, stores no draft, invokes no model, and cannot open or copy public feedback until review is confirmed; both GitHub issue forms now require privacy and adult-review declarations.
- Added a child-friendly four-stop activity meter and evidence-backed trail stars for opening a lesson, sending work, receiving human feedback, and trying again. They use existing local records and never become points, grades, streaks, ranks, rewards, ability labels, or mastery claims.
- Isolated the packaged release self-test onto a free ephemeral loopback port so an installed MA-Teacher already serving the real classroom on port 5201 no longer causes a false release failure; production classroom identity and school-network guidance remain unchanged.
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
