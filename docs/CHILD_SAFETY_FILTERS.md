# Learner safety filters and safe search

MA-Teacher is built for children as well as adults. Learner-facing text and search boundaries therefore need more than a polite warning.

## What is blocked and reported

Classroom submissions are checked on the teacher computer before they are stored as work. High-confidence profanity, explicit sexual material, hate/slur terms, unsafe external links, hidden-character obfuscation and instructions to disable or bypass safety rules are refused.

The learner sees a calm message asking for school-safe words. The teacher sees a durable safety incident with:

- learner and assigned lesson;
- time and input surface;
- category or categories;
- input length and repeat count;
- action taken.

The rejected phrase is not copied into the incident table. MA-Teacher keeps an installation-salted HMAC fingerprint so identical repeat attempts can be grouped without storing the words. Reports support human follow-up; they never trigger automatic punishment, diagnosis or scoring.

If a learner needs to report abuse, danger or other unsafe material, the filter directs them to speak to the teacher. A future safeguarding lane must be designed with safeguarding professionals; an ordinary assignment box must not pretend to be one.

## Safe search

There is no learner internet-search feature in `0.1.0`. Search boxes in the teacher workspace are local filters over bundled or database-owned records. They do not contact search engines.

Future learner search must stay within teacher-approved, locally captured material from the trusted-source policy. It must not become a general web proxy. General search, news, social media, adult content, arbitrary URLs and unreviewed model-generated answers are outside the learner boundary.

## Avoiding harmful over-filtering

Teacher-approved lessons may legitimately discuss anatomy, history, literature, discrimination or safeguarding. MA-Teacher does not run learner-submission rules over reviewed lesson content or silently rewrite records. Filtering is scoped by surface and authority.

The first dictionary is deliberately high-confidence and English-focused. It cannot prove that text is harmless, understand every language or replace teacher supervision. False positives and missed forms should be reported without including real learner text in public GitHub issues.
