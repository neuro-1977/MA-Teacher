export type WorkspaceEffect = 'read-only' | 'database-write' | 'backup-write' | 'clipboard-optional';

export type WorkspaceSurface = {
  id: string;
  label: string;
  description: string;
  effect: WorkspaceEffect;
};

export type WorkspaceGroup = {
  id: string;
  label: string;
  purpose: string;
  surfaces: WorkspaceSurface[];
};

export const workspaceGroups: WorkspaceGroup[] = [
  {
    id: 'orient',
    label: 'Orient and resume',
    purpose: 'Understand current boundaries before changing teaching records.',
    surfaces: [
      { id: 'workspace-start', label: 'Start Here', description: 'Reads persisted counts and points to the first missing record.', effect: 'read-only' },
      { id: 'workspace-view-mode', label: 'Workspace view', description: 'Switches browser-memory presentation among the complete Teacher workspace, Planning focus, and Lesson focus.', effect: 'read-only' },
      { id: 'workspace-registry-audit', label: 'Workspace registry audit', description: 'Compares registered surface IDs with mounted workspace destinations only when explicitly requested.', effect: 'read-only' },
      { id: 'workspace-continuation', label: 'Continuation snapshot', description: 'Reads readiness, coverage, and bounded canonical history once; optional clipboard copy remains non-canonical.', effect: 'clipboard-optional' },
      { id: 'workspace-development-history', label: 'Development history', description: 'Reads bounded integrity-aware canonical breadcrumbs with explicit older-page loading.', effect: 'read-only' },
      { id: 'workspace-development-receipt', label: 'Development receipt', description: 'Appends one explicitly confirmed immutable canonical breadcrumb through the local API.', effect: 'database-write' },
      { id: 'workspace-evidence', label: 'Evidence language', description: 'Defines the bounded meanings of source, observation, review, acceptance, failure, and support states.', effect: 'read-only' },
      { id: 'workspace-safety', label: 'Safety and privacy', description: 'States learner-data, safeguarding, backup, and accessibility boundaries.', effect: 'read-only' },
      { id: 'workspace-accessibility-reviews', label: 'Accessibility reviews', description: 'Appends criterion-complete human observations for one named surface and environment without certifying the product.', effect: 'database-write' },
      { id: 'workspace-readiness', label: 'Project readiness', description: 'Shows database-backed gates while preserving the deliberately incomplete project state.', effect: 'read-only' },
    ],
  },
  {
    id: 'protect',
    label: 'Protect local records',
    purpose: 'Inspect coverage and create explicit local database snapshots.',
    surfaces: [
      { id: 'workspace-backups', label: 'Database backups', description: 'Creates or verifies an operator-requested install-root SQLite snapshot.', effect: 'backup-write' },
      { id: 'workspace-data-stewardship', label: 'Data stewardship', description: 'Reads bounded record counts and appends human-authored retention-policy evidence without deleting data.', effect: 'database-write' },
      { id: 'workspace-coverage', label: 'Curriculum coverage', description: 'Shows supported, partial, reference-only, and unsupported curriculum lanes.', effect: 'read-only' },
      { id: 'workspace-source-acquisition', label: 'Curriculum source guide', description: 'Filters official acquisition routes while preserving an explicit not-imported state.', effect: 'read-only' },
      { id: 'workspace-curriculum-review', label: 'Curriculum review guide', description: 'Filters evidence requirements and refusal boundaries for registration through drift reconciliation.', effect: 'read-only' },
    ],
  },
  {
    id: 'author',
    label: 'Author and teach',
    purpose: 'Create the database-owned learner, curriculum, lesson, and practice records.',
    surfaces: [
      { id: 'workspace-learning', label: 'Learner workspace', description: 'Creates learners, plans, source reviews, and accepted curriculum candidates through explicit actions.', effect: 'database-write' },
      { id: 'workspace-lesson-draft', label: 'Lesson drafting', description: 'Creates evidence-linked lesson drafts and sections.', effect: 'database-write' },
      { id: 'workspace-proposals', label: 'Teaching proposals', description: 'Records evidence-linked unreviewed proposals and immutable operator reviews without applying content.', effect: 'database-write' },
      { id: 'workspace-lesson-reader', label: 'Lesson reader', description: 'Reads the selected lesson and its curriculum provenance.', effect: 'read-only' },
      { id: 'workspace-lesson-review-records', label: 'Lesson review records', description: 'Appends criterion-complete reviews bound to exact lesson fingerprints without editing lessons.', effect: 'database-write' },
      { id: 'workspace-teaching-sessions', label: 'Teaching-session receipts', description: 'Appends claimed delivery evidence bound to an exact currently approved lesson fingerprint.', effect: 'database-write' },
      { id: 'workspace-teaching-operations', label: 'Teaching operations', description: 'Reads approval, delivery, practice, attempt, and human-review evidence to expose the next missing record.', effect: 'read-only' },
      { id: 'workspace-learning-checks', label: 'Practice and review', description: 'Creates manual checks, learner attempts, and immutable human reviews.', effect: 'database-write' },
      { id: 'workspace-progress', label: 'Progress evidence', description: 'Reads learner and subject evidence without scores, ranks, or mastery inference.', effect: 'read-only' },
    ],
  },
  {
    id: 'reference',
    label: 'Plan with references',
    purpose: 'Use product-owned guidance without silently creating teaching records.',
    surfaces: [
      { id: 'workspace-references', label: 'Teaching references', description: 'Reads the bounded ITTECF and EEF reference registry.', effect: 'read-only' },
      { id: 'workspace-reference-review', label: 'Teaching reference reviews', description: 'Appends immutable source-fingerprint review dispositions without changing the registry.', effect: 'database-write' },
      { id: 'workspace-evidence-appraisal', label: 'Evidence appraisal', description: 'Filters claim-specific review questions and stop conditions without scoring or accepting a source.', effect: 'read-only' },
      { id: 'workspace-patterns', label: 'Teaching patterns', description: 'Filters subject-aware lesson structures in browser memory.', effect: 'read-only' },
      { id: 'workspace-teaching-toolkit-path', label: 'Teaching toolkit path', description: 'Links the human-led plan, ask, notice, and respond sequence without carrying learner data or approval state.', effect: 'read-only' },
      { id: 'workspace-teaching-data-provenance', label: 'Teaching data provenance', description: 'Shows declared source, author, evidence status, authority boundary, and next review for current teaching banks.', effect: 'read-only' },
      { id: 'workspace-curriculum-reference-candidates', label: 'Curriculum reference candidates', description: 'Filters official-source intake candidates by jurisdiction and review state without fetching or accepting curriculum.', effect: 'read-only' },
      { id: 'workspace-jurisdiction-stage-guidance', label: 'Jurisdiction stage guidance', description: 'Shows native stage structures and explicit non-equivalence cautions without mapping or classifying learners.', effect: 'read-only' },
      { id: 'workspace-teaching-evidence-checklist', label: 'Teaching evidence checklist', description: 'Provides ten expandable human-review prompts without scores, completion state, persistence, or approval authority.', effect: 'read-only' },
      { id: 'workspace-vocabulary-planning', label: 'Vocabulary planning', description: 'Filters original disciplinary meanings, models, non-examples, retrieval cues, and cautions without profiling learners.', effect: 'read-only' },
      { id: 'workspace-questioning-planning', label: 'Questioning planning', description: 'Filters original prompts, adaptive follow-ups, evidence to notice, and interpretation cautions without scoring learner responses.', effect: 'read-only' },
      { id: 'workspace-feedback-planning', label: 'Feedback planning', description: 'Filters original evidence-based feedback stems, learner actions, and cautions without capturing or grading learner work.', effect: 'read-only' },
      { id: 'workspace-session-brief', label: 'Teaching-session brief', description: 'Combines one subject lens and one stage lens with teacher-authored intent in browser memory; optional copy remains non-canonical.', effect: 'clipboard-optional' },
      { id: 'workspace-planning-packet', label: 'Teaching planning packet', description: 'Selects provenance-rich vocabulary, questioning, feedback, and worked-example entries by subject and stage without inventing missing matches.', effect: 'clipboard-optional' },
      { id: 'workspace-bank-coverage', label: 'Teaching-bank coverage debt', description: 'Computes four-bank presence for every subject-stage combination and exposes partial or empty source lanes without scoring quality.', effect: 'read-only' },
      { id: 'workspace-authoring-queue', label: 'Teaching-data authoring queue', description: 'Turns exact missing bank matches into unfilled, schema-specific contribution templates without generating or prioritizing content.', effect: 'clipboard-optional' },
      { id: 'workspace-draft-validator', label: 'Teaching-data draft validator', description: 'Checks one browser-memory JSON draft for source shape, identifiers, exact subject/stage values, duplicate IDs and unknown fields without applying it.', effect: 'clipboard-optional' },
      { id: 'workspace-subjects', label: 'Subject lenses', description: 'Filters disciplinary habits, evidence forms, planning questions, and cautions.', effect: 'read-only' },
      { id: 'workspace-stages', label: 'Stage lenses', description: 'Filters age-respectful planning guidance with explicit configured-partial or reference-only status.', effect: 'read-only' },
      { id: 'workspace-inclusive-planning', label: 'Inclusive planning', description: 'Filters non-diagnostic access-planning prompts that preserve objectives and human authority.', effect: 'read-only' },
      { id: 'workspace-assessment-design', label: 'Assessment design', description: 'Filters manual prompt, criteria, and feedback scaffolds.', effect: 'read-only' },
      { id: 'workspace-misconception-response', label: 'Misconception response', description: 'Compares evidence hypotheses for a wrong answer without classifying the learner.', effect: 'read-only' },
      { id: 'workspace-lesson-review', label: 'Lesson review gate', description: 'Filters human evidence questions and explicit stop-use conditions without approving the lesson.', effect: 'read-only' },
      { id: 'workspace-rights', label: 'Resource rights', description: 'Filters conservative provenance, reuse, attribution, and refusal boundaries for teaching material.', effect: 'read-only' },
      { id: 'workspace-worked-examples', label: 'Worked examples', description: 'Filters twelve synthetic cross-subject, cross-stage evidence loops without creating learner or lesson records.', effect: 'read-only' },
    ],
  },
];

export const workspaceEffectLabels: Record<WorkspaceEffect, string> = {
  'read-only': 'READ ONLY',
  'database-write': 'WRITES DATABASE',
  'backup-write': 'CREATES BACKUP',
  'clipboard-optional': 'READ ONLY · OPTIONAL COPY',
};
