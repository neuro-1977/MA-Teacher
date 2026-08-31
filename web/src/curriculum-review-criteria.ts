export const curriculumReviewPhases = [
  {
    id: 'registration',
    label: 'Source registration',
    purpose: 'Decide whether an origin belongs in a named curriculum evidence lane before any content is trusted.',
    inspect: ['Publisher and institutional authority', 'HTTPS origin and exact publication family', 'Jurisdiction, curriculum, subject, stage, and qualification scope', 'Publication status, revision route, and available document formats'],
    progressWhen: 'The source has a clear authoritative owner, bounded scope, allowed origin, and named review purpose.',
    refuseWhen: 'Ownership is unclear, the origin is unofficial or mixed with user content, scope is generic, or the source is being added only because it agrees with an expected answer.',
  },
  {
    id: 'capture',
    label: 'Versioned capture',
    purpose: 'Preserve what was retrieved without confusing transport success with content approval.',
    inspect: ['Final origin after redirects', 'HTTP status, media type, size, and retrieval time', 'Content hash and duplicate relationship', 'Whether the response is the intended publication rather than login, error, consent, or navigation content'],
    progressWhen: 'The bounded response matches the registered origin and intended publication type, and immutable version evidence is recorded.',
    refuseWhen: 'Redirect leaves the allowed boundary, content is unexpectedly large or wrong type, a login/error page was captured, or the response cannot be tied to the intended publication.',
  },
  {
    id: 'extraction',
    label: 'Text extraction',
    purpose: 'Recover inspectable text while preserving the distinction between source artifact and parser output.',
    inspect: ['Document identity and hash', 'Page, paragraph, table, heading, list, and reading-order fidelity', 'Missing symbols, formulae, diagrams, footnotes, headers, and columns', 'Truncation, duplication, encoding damage, and OCR requirements'],
    progressWhen: 'Extracted blocks can be traced to the exact artifact and enough surrounding context survives for human review.',
    refuseWhen: 'Layout loss changes meaning, text is empty or corrupted, tables or conditions are detached, or the block cannot be located in the artifact.',
  },
  {
    id: 'candidate',
    label: 'Curriculum candidate',
    purpose: 'Turn a traceable source passage into a bounded review proposal without automatic authority.',
    inspect: ['Exact quoted or faithfully summarized source block', 'Subject, stage, jurisdiction, and curriculum lane', 'Whether the statement is requirement, aim, guidance, example, definition, or contextual prose', 'Dependencies, exceptions, progression wording, and neighboring clauses'],
    progressWhen: 'The candidate preserves meaning, scope, provenance, and statement type, and can be reviewed independently.',
    refuseWhen: 'The candidate broadens the source, merges separate requirements, invents progression, drops a condition, or assigns the wrong subject or stage.',
  },
  {
    id: 'acceptance',
    label: 'Human acceptance',
    purpose: 'Authorize one candidate for bounded lesson linkage after explicit human review.',
    inspect: ['Current source version and drift state', 'Candidate wording against surrounding source context', 'Correct subject, stage, curriculum lane, and statement type', 'Reviewer identity, decision reason, and any limitations'],
    progressWhen: 'A human confirms the candidate is faithful, relevant, current enough for the intended use, and correctly classified.',
    refuseWhen: 'Evidence is stale without review, ambiguous, out of scope, duplicated, contradicted by the current source, or not useful for the intended lesson lane.',
  },
  {
    id: 'drift',
    label: 'Drift reconciliation',
    purpose: 'Compare a new source version without rewriting historical evidence or silently changing linked lessons.',
    inspect: ['Old and new content hashes', 'Added, removed, and changed source blocks', 'Impact on accepted candidates and linked lesson claims', 'Whether change is editorial, structural, substantive, superseding, or currently unclear'],
    progressWhen: 'A human records a bounded disposition and identifies every affected candidate or lesson requiring separate review.',
    refuseWhen: 'A new capture silently replaces old evidence, a hash change is treated as semantic change without inspection, or linked lessons are updated automatically.',
  },
] as const;

export type CurriculumReviewPhase = typeof curriculumReviewPhases[number];
export type CurriculumReviewPhaseId = CurriculumReviewPhase['id'];
