import { useEffect, type ReactNode } from 'react';
import { useState } from 'react';
import { App } from './App';
import { AccessibilityReviewPanel } from './AccessibilityReviewPanel';
import { AssessmentPatternsPanel } from './AssessmentPatternsPanel';
import { ContinuationSnapshotPanel } from './ContinuationSnapshotPanel';
import { CurriculumCoveragePanel } from './CurriculumCoveragePanel';
import { CurriculumReferenceCandidatePanel } from './CurriculumReferenceCandidatePanel';
import { CurriculumReviewGuidePanel } from './CurriculumReviewGuidePanel';
import { CurriculumSourceAcquisitionPanel } from './CurriculumSourceAcquisitionPanel';
import { DatabaseBackupPanel } from './DatabaseBackupPanel';
import { DataStewardshipPanel } from './DataStewardshipPanel';
import { DevelopmentHistoryPanel } from './DevelopmentHistoryPanel';
import { DevelopmentReceiptPanel } from './DevelopmentReceiptPanel';
import { EvidenceLegendPanel } from './EvidenceLegendPanel';
import { FeedbackPlanningPanel } from './FeedbackPlanningPanel';
import { GettingStartedPanel } from './GettingStartedPanel';
import { InclusivePlanningPanel } from './InclusivePlanningPanel';
import { JurisdictionStageGuidePanel } from './JurisdictionStageGuidePanel';
import { LearningCheckPanel } from './LearningCheckPanel';
import { LearningProgressPanel } from './LearningProgressPanel';
import { LearningWorkspacePanel } from './LearningWorkspacePanel';
import { LessonDraftPanel } from './LessonDraftPanel';
import { LessonReaderPanel } from './LessonReaderPanel';
import { LessonReviewGatePanel } from './LessonReviewGatePanel';
import { LessonReviewRecordPanel } from './LessonReviewRecordPanel';
import { MisconceptionResponsePanel } from './MisconceptionResponsePanel';
import { PrintLessonControl } from './PrintLessonControl';
import { ProjectReadinessPanel } from './ProjectReadinessPanel';
import { QuestioningPlanningPanel } from './QuestioningPlanningPanel';
import { ResourceRightsPanel } from './ResourceRightsPanel';
import { SafetyPrivacyPanel } from './SafetyPrivacyPanel';
import { StageLensesPanel } from './StageLensesPanel';
import { SubjectLensesPanel } from './SubjectLensesPanel';
import { SurfaceErrorBoundary } from './AppErrorBoundary';
import { TeachingBankCoveragePanel } from './TeachingBankCoveragePanel';
import { TeachingDataAuthoringQueuePanel } from './TeachingDataAuthoringQueuePanel';
import { TeachingDataDraftValidatorPanel } from './TeachingDataDraftValidatorPanel';
import { TeachingDataProvenancePanel } from './TeachingDataProvenancePanel';
import { TeachingEvidenceAppraisalPanel } from './TeachingEvidenceAppraisalPanel';
import { TeachingEvidenceChecklistPanel } from './TeachingEvidenceChecklistPanel';
import { TeachingOperationsPanel } from './TeachingOperationsPanel';
import { TeachingPatternsPanel } from './TeachingPatternsPanel';
import { TeachingPlanningPacketPanel } from './TeachingPlanningPacketPanel';
import { TeachingProposalPanel } from './TeachingProposalPanel';
import { TeachingReferencePanel } from './TeachingReferencePanel';
import { TeachingReferenceReviewPanel } from './TeachingReferenceReviewPanel';
import { TeachingSessionBriefPanel } from './TeachingSessionBriefPanel';
import { TeachingSessionPanel } from './TeachingSessionPanel';
import { TeachingToolkitPathPanel } from './TeachingToolkitPathPanel';
import { ViewModeControl } from './ViewModeControl';
import { VocabularyPlanningPanel } from './VocabularyPlanningPanel';
import { WorkedExamplesPanel } from './WorkedExamplesPanel';
import { WorkspaceIndexPanel } from './WorkspaceIndexPanel';
import { WorkspaceRegistryAuditPanel } from './WorkspaceRegistryAuditPanel';
import { workspaceEffectLabels, workspaceGroups, type WorkspaceEffect } from './workspace-surfaces';
import tutorIcon from '../../icon-large.png';
import './teacher-shell.css';

type WorkspaceEntry = { id: string; label: string; description: string };
type WorkspaceGroup = { id: string; label: string; eyebrow: string; description: string; entries: WorkspaceEntry[] };

const groups: WorkspaceGroup[] = [
  {
    id: 'teach', label: 'Teach', eyebrow: 'THE DAY-TO-DAY WORK', description: 'Learners, lessons, practice and progress.',
    entries: [
      { id: 'workspace-start', label: 'Guided setup', description: 'See the next missing record in the teaching journey.' },
      { id: 'workspace-learning', label: 'Learners and study plans', description: 'Create a learner, choose a subject and set a goal.' },
      { id: 'workspace-lesson-draft', label: 'Prepare a lesson', description: 'Draft a lesson linked to accepted curriculum evidence.' },
      { id: 'workspace-lesson-reader', label: 'Open a lesson', description: 'Read the selected lesson in a clean teaching view.' },
      { id: 'workspace-learning-checks', label: 'Practice and review', description: 'Create a check, record an answer and review it.' },
      { id: 'workspace-progress', label: 'Progress record', description: 'See recorded learning evidence without invented scores.' },
      { id: 'workspace-teaching-sessions', label: 'Record a teaching session', description: 'Record what was taught against the exact lesson.' },
      { id: 'workspace-teaching-operations', label: 'Teaching pipeline', description: 'See what evidence is missing from a lesson journey.' },
    ],
  },
  {
    id: 'plan', label: 'Plan', eyebrow: 'BUILD A STRONGER LESSON', description: 'Practical planning aids, examples and review prompts.',
    entries: [
      { id: 'workspace-session-brief', label: 'Quick session brief', description: 'Combine subject, stage and your teaching intent.' },
      { id: 'workspace-planning-packet', label: 'Planning packet', description: 'Gather matching vocabulary, questions and feedback.' },
      { id: 'workspace-proposals', label: 'Teaching proposals', description: 'Record an idea for human review without applying it.' },
      { id: 'workspace-patterns', label: 'Lesson structures', description: 'Browse subject-aware ways to structure a lesson.' },
      { id: 'workspace-vocabulary-planning', label: 'Vocabulary', description: 'Plan meanings, models, non-examples and retrieval.' },
      { id: 'workspace-questioning-planning', label: 'Questions', description: 'Plan prompts, follow-ups and evidence to notice.' },
      { id: 'workspace-feedback-planning', label: 'Feedback', description: 'Plan feedback that leads to a learner action.' },
      { id: 'workspace-worked-examples', label: 'Worked examples', description: 'Browse examples across subjects and age groups.' },
      { id: 'workspace-inclusive-planning', label: 'Inclusive access', description: 'Plan access without diagnosing or lowering the goal.' },
      { id: 'workspace-assessment-design', label: 'Assessment prompts', description: 'Plan manual checks, criteria and feedback.' },
      { id: 'workspace-misconception-response', label: 'Respond to a wrong answer', description: 'Compare possible explanations before responding.' },
      { id: 'workspace-lesson-review', label: 'Lesson review guide', description: 'Check a lesson against explicit stop-use conditions.' },
      { id: 'workspace-lesson-review-records', label: 'Approve a saved lesson', description: 'Record a review tied to the exact saved lesson.' },
    ],
  },
  {
    id: 'curriculum', label: 'Curriculum', eyebrow: 'WHAT SHOULD BE TAUGHT', description: 'Official sources, subject coverage and teaching references.',
    entries: [
      { id: 'workspace-curriculum-sources', label: 'Curriculum source status', description: 'See registered official sources and capture status.' },
      { id: 'workspace-coverage', label: 'Curriculum coverage', description: 'See supported, partial and unsupported areas.' },
      { id: 'workspace-source-acquisition', label: 'Find official sources', description: 'Use the guide to locate appropriate source material.' },
      { id: 'workspace-curriculum-review', label: 'Review source changes', description: 'Understand the evidence needed before accepting drift.' },
      { id: 'workspace-curriculum-reference-candidates', label: 'Source candidates', description: 'Inspect official-source candidates before import.' },
      { id: 'workspace-references', label: 'Teaching references', description: 'Browse the bounded teaching reference library.' },
      { id: 'workspace-evidence-appraisal', label: 'Appraise evidence', description: 'Ask whether a claim is supported for this use.' },
      { id: 'workspace-subjects', label: 'Subject guidance', description: 'See what evidence and thinking look like by subject.' },
      { id: 'workspace-stages', label: 'Age and stage guidance', description: 'Use age-respectful guidance without false equivalence.' },
      { id: 'workspace-jurisdiction-stage-guidance', label: 'Jurisdiction stages', description: 'Compare native stage structures carefully.' },
      { id: 'workspace-rights', label: 'Resource rights', description: 'Check provenance, reuse and attribution boundaries.' },
    ],
  },
  {
    id: 'library', label: 'Teaching library', eyebrow: 'REFERENCE BANKS', description: 'Inspect and improve the app-owned teaching material.',
    entries: [
      { id: 'workspace-teaching-toolkit-path', label: 'Planning pathway', description: 'Follow the plan, ask, notice and respond sequence.' },
      { id: 'workspace-teaching-data-provenance', label: 'Material provenance', description: 'See where each teaching bank came from.' },
      { id: 'workspace-teaching-evidence-checklist', label: 'Evidence checklist', description: 'Use human review prompts without automatic scoring.' },
      { id: 'workspace-bank-coverage', label: 'Library gaps', description: 'See missing subject and stage combinations.' },
      { id: 'workspace-authoring-queue', label: 'Authoring queue', description: 'Create blank contribution templates for exact gaps.' },
      { id: 'workspace-draft-validator', label: 'Validate a contribution', description: 'Check one JSON draft without applying it.' },
      { id: 'workspace-reference-review', label: 'Review a reference', description: 'Record what was checked against an exact source.' },
    ],
  },
  {
    id: 'manage', label: 'Manage', eyebrow: 'LOCAL RECORDS AND SAFETY', description: 'Backups, privacy, accessibility and readiness.',
    entries: [
      { id: 'workspace-backups', label: 'Database backups', description: 'Create or verify an explicit local snapshot.' },
      { id: 'workspace-data-stewardship', label: 'Data stewardship', description: 'Inspect record counts and record retention policy.' },
      { id: 'workspace-safety', label: 'Safety and privacy', description: 'Review learner-data and safeguarding boundaries.' },
      { id: 'workspace-accessibility-reviews', label: 'Accessibility reviews', description: 'Record an observation for one named surface.' },
      { id: 'workspace-readiness', label: 'Project readiness', description: 'See what is proven, partial or still missing.' },
      { id: 'workspace-evidence', label: 'Evidence language', description: 'Understand what every status label means.' },
    ],
  },
  {
    id: 'advanced', label: 'Advanced', eyebrow: 'DEVELOPMENT AND DIAGNOSTICS', description: 'Technical evidence and maintenance tools, kept out of normal teaching.',
    entries: [
      { id: 'workspace-index', label: 'Complete workspace index', description: 'Search every available surface and side effect.' },
      { id: 'workspace-view-mode', label: 'Legacy view preferences', description: 'Inspect the earlier presentation preference control.' },
      { id: 'workspace-registry-audit', label: 'Navigation audit', description: 'Compare registered destinations with mounted surfaces.' },
      { id: 'workspace-continuation', label: 'Continuation snapshot', description: 'Read a bounded technical handoff snapshot.' },
      { id: 'workspace-development-history', label: 'Development history', description: 'Read immutable database-owned development receipts.' },
      { id: 'workspace-development-receipt', label: 'Append development receipt', description: 'Record one explicitly confirmed technical breadcrumb.' },
    ],
  },
];

const surfaceRenderers: Partial<Record<string, () => ReactNode>> = {
  'workspace-start': () => <GettingStartedPanel />,
  'workspace-view-mode': () => <ViewModeControl />,
  'workspace-index': () => <WorkspaceIndexPanel />,
  'workspace-registry-audit': () => <WorkspaceRegistryAuditPanel />,
  'workspace-continuation': () => <ContinuationSnapshotPanel />,
  'workspace-development-history': () => <DevelopmentHistoryPanel />,
  'workspace-development-receipt': () => <DevelopmentReceiptPanel />,
  'workspace-evidence': () => <EvidenceLegendPanel />,
  'workspace-safety': () => <SafetyPrivacyPanel />,
  'workspace-data-stewardship': () => <DataStewardshipPanel />,
  'workspace-accessibility-reviews': () => <AccessibilityReviewPanel />,
  'workspace-readiness': () => <ProjectReadinessPanel />,
  'workspace-curriculum-sources': () => <App />,
  'workspace-source-acquisition': () => <CurriculumSourceAcquisitionPanel />,
  'workspace-backups': () => <DatabaseBackupPanel />,
  'workspace-coverage': () => <CurriculumCoveragePanel />,
  'workspace-curriculum-review': () => <CurriculumReviewGuidePanel />,
  'workspace-learning': () => <LearningWorkspacePanel />,
  'workspace-lesson-draft': () => <LessonDraftPanel />,
  'workspace-proposals': () => <TeachingProposalPanel />,
  'workspace-lesson-reader': () => <LessonReaderPanel />,
  'workspace-lesson-review-records': () => <LessonReviewRecordPanel />,
  'workspace-teaching-sessions': () => <TeachingSessionPanel />,
  'workspace-teaching-operations': () => <TeachingOperationsPanel />,
  'workspace-learning-checks': () => <LearningCheckPanel />,
  'workspace-misconception-response': () => <MisconceptionResponsePanel />,
  'workspace-progress': () => <LearningProgressPanel />,
  'workspace-references': () => <TeachingReferencePanel />,
  'workspace-reference-review': () => <TeachingReferenceReviewPanel />,
  'workspace-evidence-appraisal': () => <TeachingEvidenceAppraisalPanel />,
  'workspace-patterns': () => <TeachingPatternsPanel />,
  'workspace-teaching-toolkit-path': () => <TeachingToolkitPathPanel />,
  'workspace-teaching-data-provenance': () => <TeachingDataProvenancePanel />,
  'workspace-curriculum-reference-candidates': () => <CurriculumReferenceCandidatePanel />,
  'workspace-jurisdiction-stage-guidance': () => <JurisdictionStageGuidePanel />,
  'workspace-teaching-evidence-checklist': () => <TeachingEvidenceChecklistPanel />,
  'workspace-vocabulary-planning': () => <VocabularyPlanningPanel />,
  'workspace-questioning-planning': () => <QuestioningPlanningPanel />,
  'workspace-feedback-planning': () => <FeedbackPlanningPanel />,
  'workspace-session-brief': () => <TeachingSessionBriefPanel />,
  'workspace-planning-packet': () => <TeachingPlanningPacketPanel />,
  'workspace-bank-coverage': () => <TeachingBankCoveragePanel />,
  'workspace-authoring-queue': () => <TeachingDataAuthoringQueuePanel />,
  'workspace-draft-validator': () => <TeachingDataDraftValidatorPanel />,
  'workspace-subjects': () => <SubjectLensesPanel />,
  'workspace-stages': () => <StageLensesPanel />,
  'workspace-inclusive-planning': () => <InclusivePlanningPanel />,
  'workspace-assessment-design': () => <AssessmentPatternsPanel />,
  'workspace-lesson-review': () => <LessonReviewGatePanel />,
  'workspace-rights': () => <ResourceRightsPanel />,
  'workspace-worked-examples': () => <WorkedExamplesPanel />,
};

const effectById = new Map<string, WorkspaceEffect>(workspaceGroups.flatMap((group) => group.surfaces.map((surface) => [surface.id, surface.effect] as const)));
const allEntries = groups.flatMap((group) => group.entries.map((entry) => ({ ...entry, groupId: group.id })));
const validIds = new Set(allEntries.map((entry) => entry.id));

function Home({ open }: { open: (id: string) => void }) {
  const actions = [
    ['workspace-learning', 'Set up a learner', 'Create a local learner and a clear study plan.'],
    ['workspace-lesson-draft', 'Prepare a lesson', 'Build a lesson from reviewed curriculum evidence.'],
    ['workspace-lesson-reader', 'Teach a saved lesson', 'Open the selected lesson without the development clutter.'],
    ['workspace-learning-checks', 'Practice and review', 'Record a response and a human review.'],
    ['workspace-coverage', 'Explore the curriculum', 'See subjects, stages and current evidence coverage.'],
    ['workspace-start', 'Show me where to start', 'Use the guided record-by-record setup path.'],
  ] as const;
  return <section className="teacher-home" aria-labelledby="teacher-home-title">
    <div className="teacher-home__hero"><img src={tutorIcon} alt="MA-Teacher potato-shaped tutor wearing a graduation cap" /><div><p>LOCAL TEACHING WORKSPACE</p><h1 id="teacher-home-title">What do you want to do?</h1><span>Choose one task. MA-Teacher will show only the workspace needed for it.</span></div></div>
    <div className="teacher-home__actions">{actions.map(([id, label, description], index) => <button type="button" key={id} onClick={() => open(id)}><b>{String(index + 1).padStart(2, '0')}</b><span><strong>{label}</strong><small>{description}</small></span><em>OPEN</em></button>)}</div>
    <aside><strong>Your records stay local.</strong><span>MA-Teacher separates curriculum evidence, lesson decisions and learner records. It does not invent approval, progress or mastery.</span></aside>
  </section>;
}

export function TeacherShell() {
  const initialHash = window.location.hash.slice(1);
  const [activeId, setActiveId] = useState(validIds.has(initialHash) ? initialHash : 'teacher-home');
  const activeEntry = allEntries.find((entry) => entry.id === activeId);
  const activeGroup = groups.find((group) => group.id === activeEntry?.groupId) ?? groups[0];
  const activeRenderer = surfaceRenderers[activeId];

  function open(id: string, addHistory = true) {
    const next = validIds.has(id) ? id : 'teacher-home';
    setActiveId(next);
    const hash = next === 'teacher-home' ? '' : `#${next}`;
    if (addHistory) window.history.pushState({ workspace: next }, '', `${window.location.pathname}${window.location.search}${hash}`);
  }

  useEffect(() => {
    const navigate = (event: Event) => open((event as CustomEvent<{ id: string }>).detail.id);
    const restore = () => { const id = window.location.hash.slice(1); setActiveId(validIds.has(id) ? id : 'teacher-home'); };
    window.addEventListener('ma-teacher:navigate', navigate as EventListener);
    window.addEventListener('popstate', restore);
    return () => { window.removeEventListener('ma-teacher:navigate', navigate as EventListener); window.removeEventListener('popstate', restore); };
  }, []);

  useEffect(() => {
    document.title = activeEntry ? `${activeEntry.label} - MA-Teacher` : 'MA-Teacher';
    window.scrollTo({ top: 0, behavior: 'auto' });
    if (!activeEntry) return;
    window.requestAnimationFrame(() => { const target = document.getElementById(activeEntry.id); if (target) { if (!target.hasAttribute('tabindex')) target.tabIndex = -1; target.focus({ preventScroll: true }); } });
  }, [activeId]);

  const effect = activeEntry ? (effectById.get(activeEntry.id) ?? 'read-only') : null;
  return <div className="teacher-app-shell">
    <a className="teacher-skip-link" href="#teacher-main">Skip to workspace</a>
    <header className="teacher-topbar">
      <button type="button" className="teacher-brand" onClick={() => open('teacher-home')}><img src={tutorIcon} alt="" /><span><strong>MA-TEACHER</strong><small>LEARN · PLAN · REVIEW</small></span></button>
      <nav aria-label="Main areas">{groups.map((group) => <button type="button" key={group.id} className={activeGroup.id === group.id && activeEntry ? 'is-active' : ''} onClick={() => open(group.entries[0].id)}>{group.label}</button>)}</nav>
    </header>
    <div className="teacher-layout">
      <aside className="teacher-sidebar" aria-label={`${activeGroup.label} workspaces`}>
        <header><p>{activeGroup.eyebrow}</p><h2>{activeGroup.label}</h2><span>{activeGroup.description}</span></header>
        <select aria-label="Choose workspace" value={activeEntry?.id ?? ''} onChange={(event) => open(event.target.value)}><option value="" disabled>Choose a workspace</option>{activeGroup.entries.map((entry) => <option key={entry.id} value={entry.id}>{entry.label}</option>)}</select>
        <div>{activeGroup.entries.map((entry) => <button type="button" key={entry.id} className={activeId === entry.id ? 'is-active' : ''} onClick={() => open(entry.id)}><strong>{entry.label}</strong><small>{entry.description}</small></button>)}</div>
      </aside>
      <main id="teacher-main" className="teacher-main" tabIndex={-1}>
        {activeEntry ? <header className="teacher-workspace-heading"><div><p>{activeGroup.label.toUpperCase()}</p><h1>{activeEntry.label}</h1><span>{activeEntry.description}</span></div>{effect ? <strong data-effect={effect}>{workspaceEffectLabels[effect]}</strong> : null}</header> : null}
        {activeId === 'workspace-lesson-reader' ? <div className="teacher-workspace-tools"><PrintLessonControl /></div> : null}
        <div className="teacher-workspace">{activeRenderer && activeEntry ? <SurfaceErrorBoundary name={activeEntry.label}>{activeRenderer()}</SurfaceErrorBoundary> : <Home open={open} />}</div>
      </main>
    </div>
  </div>;
}
