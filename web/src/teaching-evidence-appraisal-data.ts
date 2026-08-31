export type TeachingClaimKind =
  | 'Curriculum requirement'
  | 'Factual explanation'
  | 'Teaching effectiveness'
  | 'Assessment interpretation'
  | 'Resource reuse';

export type EvidenceAppraisalDimension = {
  id: string;
  title: string;
  appliesTo: readonly TeachingClaimKind[];
  questions: readonly string[];
  record: readonly string[];
  stopWhen: string;
};

export const teachingClaimKinds: readonly TeachingClaimKind[] = [
  'Curriculum requirement',
  'Factual explanation',
  'Teaching effectiveness',
  'Assessment interpretation',
  'Resource reuse',
];

export const evidenceAppraisalDimensions: readonly EvidenceAppraisalDimension[] = [
  {
    id: 'claim-source-fit',
    title: 'Claim and source fit',
    appliesTo: teachingClaimKinds,
    questions: [
      'What exact sentence or decision is this evidence intended to support?',
      'Is the source authoritative for that claim type, jurisdiction, subject, stage, and audience?',
      'Is the source primary evidence, a synthesis, guidance, commentary, or a teaching resource?',
    ],
    record: ['Exact claim', 'Source class', 'Authority and jurisdiction', 'Direct quotation location or bounded evidence reference'],
    stopWhen: 'The source is useful background but does not directly support the claim being attached to it.',
  },
  {
    id: 'version-currency',
    title: 'Version, currency, and effective period',
    appliesTo: ['Curriculum requirement', 'Factual explanation', 'Assessment interpretation', 'Resource reuse'],
    questions: [
      'Which captured version, publication date, update date, and effective period apply?',
      'Could a stable URL now serve changed content?',
      'Has the source been superseded, corrected, withdrawn, or replaced for the intended date?',
    ],
    record: ['Retrieval UTC', 'Raw-content hash', 'Effective-from and effective-until dates', 'Superseding or superseded relationship'],
    stopWhen: 'The applicable version or effective period cannot be established.',
  },
  {
    id: 'factual-corroboration',
    title: 'Factual corroboration and contradiction',
    appliesTo: ['Factual explanation', 'Curriculum requirement', 'Assessment interpretation'],
    questions: [
      'Can the factual statement be checked against an independent primary or authoritative source?',
      'Do current sources disagree in wording, scope, date, or underlying fact?',
      'Would a learner reasonably interpret the statement more broadly than the evidence permits?',
    ],
    record: ['Supporting sources', 'Contradicting sources', 'Scope limits', 'Unresolved uncertainty'],
    stopWhen: 'A material contradiction remains unresolved or the explanation would overstate the available evidence.',
  },
  {
    id: 'population-context',
    title: 'Population, setting, and transfer',
    appliesTo: ['Teaching effectiveness', 'Assessment interpretation'],
    questions: [
      'Who participated, at what ages or stages, in which subject and setting?',
      'How similar are the learners, prior knowledge, curriculum, resources, and implementation conditions?',
      'Is transfer beyond the studied population or task being assumed?',
    ],
    record: ['Population and sample', 'Setting and subject', 'Relevant differences', 'Transfer assumptions'],
    stopWhen: 'The intended application depends on an unexamined transfer from a materially different population or setting.',
  },
  {
    id: 'method-comparator',
    title: 'Method, comparator, and alternative explanations',
    appliesTo: ['Teaching effectiveness', 'Assessment interpretation'],
    questions: [
      'What design produced the evidence and what was it compared with?',
      'Were allocation, attrition, baseline differences, missing data, and implementation measured?',
      'Could novelty, additional time, teacher expertise, selection, or measurement explain the result?',
    ],
    record: ['Study or review design', 'Comparator', 'Known limitations', 'Plausible alternative explanations'],
    stopWhen: 'The evidence cannot distinguish the claimed effect from a major alternative explanation.',
  },
  {
    id: 'outcome-alignment',
    title: 'Outcome and objective alignment',
    appliesTo: ['Teaching effectiveness', 'Assessment interpretation', 'Factual explanation'],
    questions: [
      'What was actually measured, when, and through which task or instrument?',
      'Does that outcome match the lesson objective or decision being justified?',
      'Was durable learning, transfer, confidence, preference, completion, or only immediate performance observed?',
    ],
    record: ['Measured outcome', 'Measurement time', 'Instrument or task', 'What the outcome cannot establish'],
    stopWhen: 'The claimed learning or decision is broader than the measured outcome.',
  },
  {
    id: 'magnitude-uncertainty',
    title: 'Magnitude, uncertainty, and practical meaning',
    appliesTo: ['Teaching effectiveness', 'Assessment interpretation'],
    questions: [
      'How large and uncertain is the observed difference?',
      'Is the result consistent across studies, measures, groups, and follow-up periods?',
      'What implementation cost, burden, risk, or opportunity cost accompanies it?',
    ],
    record: ['Magnitude in the source’s own terms', 'Uncertainty or range', 'Consistency', 'Practical constraints'],
    stopWhen: 'A direction-only or statistically notable result is being converted into a precise or universally important claim.',
  },
  {
    id: 'implementation-fidelity',
    title: 'Implementation and dependency conditions',
    appliesTo: ['Teaching effectiveness', 'Assessment interpretation', 'Resource reuse'],
    questions: [
      'What training, time, sequencing, materials, expertise, grouping, or technology did the original implementation require?',
      'Which elements are essential and which can be adapted?',
      'How will implementation and unintended effects be observed locally?',
    ],
    record: ['Essential components', 'Local adaptations', 'Dependencies', 'Monitoring and rollback evidence'],
    stopWhen: 'The claimed result depends on components that are absent or silently changed in the intended use.',
  },
  {
    id: 'rights-provenance',
    title: 'Rights, attribution, and material provenance',
    appliesTo: ['Resource reuse'],
    questions: [
      'Who owns the item and which exact licence applies to this version and component?',
      'Are attribution, share-alike, non-commercial, no-derivatives, account, media, or territorial restrictions present?',
      'Do images, extracts, recordings, fonts, or other third-party assets have separate terms?',
    ],
    record: ['Rights holder', 'Licence and version', 'Required attribution', 'Third-party exclusions and permitted use'],
    stopWhen: 'Permission, provenance, attribution, or third-party rights remain unclear for the intended use.',
  },
] as const;
