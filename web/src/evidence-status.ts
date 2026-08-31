export const evidenceStates = [
  {
    id: 'source-present',
    label: 'Source present',
    tone: 'neutral',
    meaning: 'The implementation or reference exists in the current source boundary. It has not been executed by this state alone.',
  },
  {
    id: 'not-run',
    label: 'Not run',
    tone: 'pending',
    meaning: 'The named verification has no accepted execution evidence for the current artifact or source state.',
  },
  {
    id: 'observed',
    label: 'Observed',
    tone: 'observed',
    meaning: 'A named behavior was directly observed in a stated environment. The claim is limited to that action and evidence.',
  },
  {
    id: 'human-reviewed',
    label: 'Human reviewed',
    tone: 'reviewed',
    meaning: 'A human made the recorded bounded judgement. It does not establish broad learner mastery or universal correctness.',
  },
  {
    id: 'accepted',
    label: 'Accepted for use',
    tone: 'accepted',
    meaning: 'The named evidence or record passed its explicit review gate for the stated purpose. Other gates remain independent.',
  },
  {
    id: 'failed',
    label: 'Failed',
    tone: 'failed',
    meaning: 'The named action or check produced contrary evidence. Preserve the failure and its exact scope until corrected and rerun.',
  },
  {
    id: 'unsupported',
    label: 'Unsupported',
    tone: 'unsupported',
    meaning: 'The product does not currently claim this curriculum, workflow, environment, or capability.',
  },
  {
    id: 'not-applicable',
    label: 'Not applicable',
    tone: 'muted',
    meaning: 'The gate does not apply to the named scope. This must include a reason and cannot be used to hide missing evidence.',
  },
] as const;

export type EvidenceState = typeof evidenceStates[number];
export type EvidenceStateId = EvidenceState['id'];

export function getEvidenceState(id: EvidenceStateId) {
  return evidenceStates.find((state) => state.id === id)!;
}
