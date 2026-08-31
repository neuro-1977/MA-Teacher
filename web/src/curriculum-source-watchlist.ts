export type CurriculumSourceWatchState = 'Manual evidence watch' | 'Blocked detail' | 'Research unresolved';

export interface CurriculumSourceWatchItem {
  id: string;
  candidateId: string | null;
  label: string;
  state: CurriculumSourceWatchState;
  trigger: string;
  evidence: readonly string[];
  preserve: string;
  forbidden: string;
}

export const curriculumSourceWatchItems: readonly CurriculumSourceWatchItem[] = [
  {
    id: 'MACW-001',
    candidateId: 'curriculum-england-eyfs-2026-transition',
    label: 'England EYFS effective-date transition',
    state: 'Manual evidence watch',
    trigger: 'First governed review on or after 1 September 2026, or any earlier intended use for that period.',
    evidence: ['Exact provider document', 'Effective-from date', 'Retrieved bytes and hash', 'Reviewer disposition'],
    preserve: 'Keep childminder and group/school documents, plus before and from 1 September revisions, as separate evidence.',
    forbidden: 'Do not assume the newest document applies to every provider or date.',
  },
  {
    id: 'MACW-002',
    candidateId: 'curriculum-wales-14-16-guidance-2026',
    label: 'Wales 14-to-16 guidance revision',
    state: 'Manual evidence watch',
    trigger: 'Before governed use and whenever Hwb publishes a revised 14-to-16 guidance notice or document.',
    evidence: ['Exact Hwb revision', 'Publication or update date', 'Content hash', 'Scope and reviewer disposition'],
    preserve: 'Keep the broad 3-to-16 framework and every 14-to-16 revision as distinct records.',
    forbidden: 'Do not replace the whole framework or map the guidance directly to another jurisdiction\'s Key Stage 4.',
  },
  {
    id: 'MACW-003',
    candidateId: 'curriculum-northern-ireland-2026-consultation',
    label: 'Northern Ireland consultation outcome',
    state: 'Manual evidence watch',
    trigger: 'An official outcome, enacted framework, implementation timetable, replacement publication, or withdrawal notice.',
    evidence: ['Official outcome status', 'Effective timetable', 'Exact publication revision', 'Reviewer comparison with current statute'],
    preserve: 'Keep the current statutory curriculum as last-good authority until a reviewed effective replacement exists.',
    forbidden: 'Do not present consultation material as the current curriculum.',
  },
  {
    id: 'MACW-004',
    candidateId: 'curriculum-northern-ireland-current-statutory',
    label: 'CCEA subject detail unavailable',
    state: 'Blocked detail',
    trigger: 'A later permitted direct retrieval of the exact CCEA subject or stage publication.',
    evidence: ['Direct official document', 'Access receipt', 'Document hash and locator', 'Scope review'],
    preserve: 'Keep the blocked-access receipt and Department of Education high-level structure separately.',
    forbidden: 'Do not substitute search snippets, mirrors, summaries, or another jurisdiction for unavailable CCEA detail.',
  },
  {
    id: 'MACW-005',
    candidateId: 'curriculum-wales-adult-learning-intake-unresolved',
    label: 'Wales adult curriculum authority unresolved',
    state: 'Research unresolved',
    trigger: 'A product feature needs Welsh adult-learning authority or an official canonical curriculum is identified.',
    evidence: ['Named official authority', 'Exact governed scope', 'Current revision', 'Reviewer rationale'],
    preserve: 'Keep the candidate in Research incomplete until the authority question is resolved.',
    forbidden: 'Do not substitute policy, apprenticeship material, or England or Scotland frameworks as Welsh curriculum authority.',
  },
  {
    id: 'MACW-006',
    candidateId: 'curriculum-scotland-adult-literacies',
    label: 'Scotland adult-literacies currency and accessibility',
    state: 'Manual evidence watch',
    trigger: 'Before statement extraction, governed authority, teaching linkage, or redistribution.',
    evidence: ['Official landing page', 'PDF hash and publication history', 'Supersession state', 'Accessibility and licence review'],
    preserve: 'Keep each reviewed PDF revision and its accessibility or licence receipt independently.',
    forbidden: 'Do not infer that an official PDF is current, accessible, reusable, or suitable for extraction.',
  },
  {
    id: 'MACW-007',
    candidateId: null,
    label: 'All candidate URL and content drift',
    state: 'Manual evidence watch',
    trigger: 'Explicit operator recheck, redirect, retrieval failure, source notice, or a future persisted due-state.',
    evidence: ['Final resolved URL', 'HTTP and retrieval receipt', 'Captured bytes and hash', 'Comparison and human disposition'],
    preserve: 'Keep last-good evidence and every immutable candidate revision while review is unresolved.',
    forbidden: 'Do not treat an unchanged URL as unchanged content or inherit an earlier review onto new bytes.',
  },
];

export const curriculumSourceWatchByCandidateId = new Map(
  curriculumSourceWatchItems
    .filter((item): item is CurriculumSourceWatchItem & { candidateId: string } => item.candidateId !== null)
    .map((item) => [item.candidateId, item]),
);

export const curriculumGlobalDriftWatch = curriculumSourceWatchItems.find((item) => item.candidateId === null)!;
