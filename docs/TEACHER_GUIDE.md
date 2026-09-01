# Teacher and operator guide

MA-Teacher records evidence and decisions; it does not replace professional judgement.

MA-Teacher opens in `Simple` view so learners see only the clearest day-to-day choices. Choose `Teacher` in the header to open planning, curriculum, safety, backup, and advanced controls. This is a display choice, not a password or permission boundary.

## First useful workflow

1. Open **Teach > Learners and study plans**.
2. Create a learner with the minimum identifying information needed.
3. Add a subject, stage, and clear goal.
4. Inspect available curriculum source evidence.
5. Prepare and human-review an evidence-linked lesson.
6. Open the clean lesson reader and teach.
7. Create a practice check.
8. Let the learner submit typed work, one supported file, or both.
9. Review the attempt and provide specific feedback.
10. Record the session and inspect the progress record.

The interface uses plain language that many nine-year-olds can understand. Lesson content should not stay at that reading level: increase vocabulary, explanations, examples, and subject precision to match the learner's age, stage, evidence, and needs. See [approachability and layered language](APPROACHABILITY.md).

## Build the app with users

Invite students to say what was easy, hard, broken, or missing through the in-app `Feedback` page. The draft remains in browser memory, makes no API call, and is discarded on reload. Its checks are a prompt, not a safeguarding guarantee. A responsible adult must review every word, replace real examples with made-up ones, and remove names, school and contact details, addresses, work, lesson answers, health information, credentials, photos, and other private data before the app unlocks copy and public-feedback links. Use [GitHub Discussions](https://github.com/neuro-1977/MA-Teacher/discussions) or the guided [issue form](https://github.com/neuro-1977/MA-Teacher/issues/new/choose) only after that review.

## Marking work

Supported files are PDF, TXT, RTF, DOC, DOCX, ODT, PNG, JPEG, and WEBP up to 10 MB. The original bytes are stored in local SQLite with filename, media type, size, and SHA-256.

Choose `met`, `partially met`, `not yet`, or `invalid`, then provide written feedback. MA-Teacher does not OCR, annotate, or automatically grade.

The contextual-vocabulary, descriptive-feedback, evidence-questioning, and worked-example planning banks cover all fourteen current subject guides. Vocabulary cards teach a learner meaning, disciplinary distinction, model, non-example and retrieval cue. Feedback cards start from named observable evidence and suggest one learner-owned action. Questioning cards name what evidence to notice and what conclusion to avoid. Worked examples demonstrate the complete model, check, synthetic attempt, bounded human review and next-evidence loop. Every card states its boundary. These are reusable planning prompts, not proof that evidence was observed, curriculum judgements, or permission to diagnose a learner. Adapt the language and response mode to the learner and lesson.

## Safeguarding and backups

Do not store passwords, medical records, legal documents, government identifiers, or unnecessary personal data. A responsible adult remains accountable for safeguarding decisions. Back up before updates or important teaching periods; a matching hash proves file identity, not successful restore.
