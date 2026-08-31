import type { TeachingBankId } from './teaching-bank-authoring-requirements';

export interface TeachingDataDraftContract {
  id: TeachingBankId;
  label: string;
  requiredStringFields: readonly string[];
  requiredListFields: readonly string[];
  stageField: 'stage' | 'stages';
}

export const teachingDataDraftContracts: readonly TeachingDataDraftContract[] = [
  {
    id: 'vocabulary', label: 'Vocabulary planning', stageField: 'stages', requiredListFields: [],
    requiredStringFields: ['id', 'subject', 'term', 'stageLabel', 'learnerMeaning', 'disciplinaryPrecision', 'modelUse', 'nonExample', 'retrievalPrompt', 'caution'],
  },
  {
    id: 'questioning', label: 'Questioning planning', stageField: 'stages', requiredListFields: [],
    requiredStringFields: ['id', 'subject', 'stageLabel', 'purpose', 'prompt', 'followUp', 'evidenceToNotice', 'caution'],
  },
  {
    id: 'feedback', label: 'Descriptive feedback planning', stageField: 'stages', requiredListFields: [],
    requiredStringFields: ['id', 'subject', 'stageLabel', 'moment', 'observedEvidence', 'feedbackStem', 'learnerAction', 'caution'],
  },
  {
    id: 'worked-example', label: 'Synthetic worked example', stageField: 'stage', requiredListFields: ['successCriteria'],
    requiredStringFields: ['id', 'subject', 'stageLabel', 'title', 'sourceBoundary', 'learningIntention', 'model', 'checkPrompt', 'sampleAttempt', 'humanReview', 'nextEvidence'],
  },
];
