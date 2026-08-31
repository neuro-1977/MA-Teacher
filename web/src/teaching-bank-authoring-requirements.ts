export type TeachingBankId = 'vocabulary' | 'questioning' | 'feedback' | 'worked-example';

export interface TeachingBankAuthoringRequirement {
  id: TeachingBankId;
  label: string;
  sourcePath: string;
  boundaryPath: string;
  requiredFields: readonly string[];
  authoringBoundary: string;
  reviewEvidence: readonly string[];
}

export const teachingBankAuthoringRequirements: readonly TeachingBankAuthoringRequirement[] = [
  {
    id: 'vocabulary',
    label: 'Vocabulary planning',
    sourcePath: 'web/src/vocabulary-planning-data.ts',
    boundaryPath: 'docs/VOCABULARY_TEACHING_BOUNDARY.md',
    requiredFields: ['Unique source identifier', 'Source or evidence locator', 'Rights and permitted-use boundary', 'Exact subject and one or more stage lenses', 'Learner-facing meaning', 'Disciplinary precision', 'Model use', 'Non-example', 'Retrieval prompt', 'Interpretation caution'],
    authoringBoundary: 'A term entry supports planning only. It is not curriculum acceptance, a reading-age judgement, proof of acquisition, or a learner profile.',
    reviewEvidence: ['Source identity, currency and locator review', 'Rights and permitted-use review', 'Subject-specialist review', 'Stage and language-demand review', 'Meaning/model/non-example consistency', 'Accessibility and dignity review', 'Source-array count and provenance reconciliation'],
  },
  {
    id: 'questioning',
    label: 'Questioning planning',
    sourcePath: 'web/src/questioning-planning-data.ts',
    boundaryPath: 'docs/QUESTIONING_TEACHING_BOUNDARY.md',
    requiredFields: ['Unique source identifier', 'Source or evidence locator', 'Rights and permitted-use boundary', 'Exact subject and one or more stage lenses', 'Question purpose', 'Primary prompt', 'Adaptive follow-up', 'Evidence to notice', 'Interpretation caution'],
    authoringBoundary: 'A prompt must elicit inspectable reasoning without becoming a score, diagnosis, scripted dialogue loop, or claim about a real learner.',
    reviewEvidence: ['Source identity, currency and locator review', 'Rights and permitted-use review', 'Pedagogical-purpose review', 'Subject-valid response-space review', 'Alternative-answer and ambiguity review', 'Accessibility and communication-route review', 'Source-array count and provenance reconciliation'],
  },
  {
    id: 'feedback',
    label: 'Descriptive feedback planning',
    sourcePath: 'web/src/feedback-planning-data.ts',
    boundaryPath: 'docs/FEEDBACK_TEACHING_BOUNDARY.md',
    requiredFields: ['Unique source identifier', 'Source or evidence locator', 'Rights and permitted-use boundary', 'Exact subject and one or more stage lenses', 'Feedback moment', 'Specific observed evidence condition', 'Bounded feedback stem', 'Learner-owned next action', 'Interpretation caution'],
    authoringBoundary: 'Feedback language may be used only when its named evidence was actually observed. It is not praise scoring, grading, diagnosis, personality judgement, or proof of progress.',
    reviewEvidence: ['Source identity, currency and locator review', 'Rights and permitted-use review', 'Observed-condition/stem consistency', 'Learner-action feasibility', 'No fixed-identity or unsupported inference', 'Subject and stage review', 'Source-array count and provenance reconciliation'],
  },
  {
    id: 'worked-example',
    label: 'Synthetic worked example',
    sourcePath: 'web/src/worked-examples.ts',
    boundaryPath: 'docs/SYNTHETIC_WORKED_WORKFLOW_EXAMPLES.md',
    requiredFields: ['Unique source identifier', 'Source or evidence locator', 'Rights and permitted-use boundary', 'Exact subject and one stage lens', 'Title', 'Source boundary', 'Narrow learning intention', 'Inspectable model', 'Check prompt', 'Success criteria', 'Synthetic sample attempt', 'Bounded human-review example', 'Next evidence'],
    authoringBoundary: 'A worked example demonstrates an evidence loop with synthetic material. It is not curriculum evidence, a learner record, stage suitability proof, or evidence of teaching effectiveness.',
    reviewEvidence: ['Source identity, currency and locator review', 'Rights and permitted-use review', 'Internal model/check/criteria consistency', 'Subject correctness', 'Synthetic-source boundary review', 'Stage/accessibility review', 'No overclaim in human review or next evidence'],
  },
];
