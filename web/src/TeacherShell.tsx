import { lazy, Suspense, useEffect, useState, type ReactNode } from 'react';
import { InfoTip } from './InfoTip';
import { PrintLessonControl } from './PrintLessonControl';
import { SurfaceErrorBoundary } from './AppErrorBoundary';
import { workspaceEffectLabels, workspaceGroups, type WorkspaceEffect } from './workspace-surfaces';
import tutorIcon from '../../icon-large.png';
import './teacher-shell.css';

const App = lazy(() => import('./App').then((module) => ({ default: module.App })));
const AccessibilityReviewPanel = lazy(() => import('./AccessibilityReviewPanel').then((module) => ({ default: module.AccessibilityReviewPanel })));
const AssessmentPatternsPanel = lazy(() => import('./AssessmentPatternsPanel').then((module) => ({ default: module.AssessmentPatternsPanel })));
const ClassroomPanel = lazy(() => import('./ClassroomPanel'));
const ContinuationSnapshotPanel = lazy(() => import('./ContinuationSnapshotPanel').then((module) => ({ default: module.ContinuationSnapshotPanel })));
const CurriculumCoveragePanel = lazy(() => import('./CurriculumCoveragePanel').then((module) => ({ default: module.CurriculumCoveragePanel })));
const CurriculumReferenceCandidatePanel = lazy(() => import('./CurriculumReferenceCandidatePanel').then((module) => ({ default: module.CurriculumReferenceCandidatePanel })));
const CurriculumReviewGuidePanel = lazy(() => import('./CurriculumReviewGuidePanel').then((module) => ({ default: module.CurriculumReviewGuidePanel })));
const CurriculumSourceAcquisitionPanel = lazy(() => import('./CurriculumSourceAcquisitionPanel').then((module) => ({ default: module.CurriculumSourceAcquisitionPanel })));
const DatabaseBackupPanel = lazy(() => import('./DatabaseBackupPanel').then((module) => ({ default: module.DatabaseBackupPanel })));
const DataStewardshipPanel = lazy(() => import('./DataStewardshipPanel').then((module) => ({ default: module.DataStewardshipPanel })));
const DevelopmentHistoryPanel = lazy(() => import('./DevelopmentHistoryPanel').then((module) => ({ default: module.DevelopmentHistoryPanel })));
const DevelopmentReceiptPanel = lazy(() => import('./DevelopmentReceiptPanel').then((module) => ({ default: module.DevelopmentReceiptPanel })));
const EvidenceLegendPanel = lazy(() => import('./EvidenceLegendPanel').then((module) => ({ default: module.EvidenceLegendPanel })));
const FeedbackHubPanel = lazy(() => import('./FeedbackHubPanel').then((module) => ({ default: module.FeedbackHubPanel })));
const FeedbackPlanningPanel = lazy(() => import('./FeedbackPlanningPanel').then((module) => ({ default: module.FeedbackPlanningPanel })));
const GettingStartedPanel = lazy(() => import('./GettingStartedPanel').then((module) => ({ default: module.GettingStartedPanel })));
const InclusivePlanningPanel = lazy(() => import('./InclusivePlanningPanel').then((module) => ({ default: module.InclusivePlanningPanel })));
const JurisdictionStageGuidePanel = lazy(() => import('./JurisdictionStageGuidePanel').then((module) => ({ default: module.JurisdictionStageGuidePanel })));
const LearningCheckPanel = lazy(() => import('./LearningCheckPanel').then((module) => ({ default: module.LearningCheckPanel })));
const LearningProgressPanel = lazy(() => import('./LearningProgressPanel').then((module) => ({ default: module.LearningProgressPanel })));
const LearningWorkspacePanel = lazy(() => import('./LearningWorkspacePanel').then((module) => ({ default: module.LearningWorkspacePanel })));
const LessonDraftPanel = lazy(() => import('./LessonDraftPanel').then((module) => ({ default: module.LessonDraftPanel })));
const LessonReaderPanel = lazy(() => import('./LessonReaderPanel').then((module) => ({ default: module.LessonReaderPanel })));
const LessonReviewGatePanel = lazy(() => import('./LessonReviewGatePanel').then((module) => ({ default: module.LessonReviewGatePanel })));
const LessonReviewRecordPanel = lazy(() => import('./LessonReviewRecordPanel').then((module) => ({ default: module.LessonReviewRecordPanel })));
const MisconceptionResponsePanel = lazy(() => import('./MisconceptionResponsePanel').then((module) => ({ default: module.MisconceptionResponsePanel })));
const ProjectReadinessPanel = lazy(() => import('./ProjectReadinessPanel').then((module) => ({ default: module.ProjectReadinessPanel })));
const QuestioningPlanningPanel = lazy(() => import('./QuestioningPlanningPanel').then((module) => ({ default: module.QuestioningPlanningPanel })));
const ResourceRightsPanel = lazy(() => import('./ResourceRightsPanel').then((module) => ({ default: module.ResourceRightsPanel })));
const SafeCodeLabPanel = lazy(() => import('./SafeCodeLabPanel'));
const SafetyPrivacyPanel = lazy(() => import('./SafetyPrivacyPanel').then((module) => ({ default: module.SafetyPrivacyPanel })));
const StageLensesPanel = lazy(() => import('./StageLensesPanel').then((module) => ({ default: module.StageLensesPanel })));
const SubjectLensesPanel = lazy(() => import('./SubjectLensesPanel').then((module) => ({ default: module.SubjectLensesPanel })));
const TeachingBankCoveragePanel = lazy(() => import('./TeachingBankCoveragePanel').then((module) => ({ default: module.TeachingBankCoveragePanel })));
const TeachingDataAuthoringQueuePanel = lazy(() => import('./TeachingDataAuthoringQueuePanel').then((module) => ({ default: module.TeachingDataAuthoringQueuePanel })));
const TeachingDataDraftValidatorPanel = lazy(() => import('./TeachingDataDraftValidatorPanel').then((module) => ({ default: module.TeachingDataDraftValidatorPanel })));
const TeachingDataProvenancePanel = lazy(() => import('./TeachingDataProvenancePanel').then((module) => ({ default: module.TeachingDataProvenancePanel })));
const TeachingEvidenceAppraisalPanel = lazy(() => import('./TeachingEvidenceAppraisalPanel').then((module) => ({ default: module.TeachingEvidenceAppraisalPanel })));
const TeachingEvidenceChecklistPanel = lazy(() => import('./TeachingEvidenceChecklistPanel').then((module) => ({ default: module.TeachingEvidenceChecklistPanel })));
const TeachingOperationsPanel = lazy(() => import('./TeachingOperationsPanel').then((module) => ({ default: module.TeachingOperationsPanel })));
const TeachingPatternsPanel = lazy(() => import('./TeachingPatternsPanel').then((module) => ({ default: module.TeachingPatternsPanel })));
const TeachingPlanningPacketPanel = lazy(() => import('./TeachingPlanningPacketPanel').then((module) => ({ default: module.TeachingPlanningPacketPanel })));
const TeachingProposalPanel = lazy(() => import('./TeachingProposalPanel').then((module) => ({ default: module.TeachingProposalPanel })));
const TeachingReferencePanel = lazy(() => import('./TeachingReferencePanel').then((module) => ({ default: module.TeachingReferencePanel })));
const TeachingReferenceReviewPanel = lazy(() => import('./TeachingReferenceReviewPanel').then((module) => ({ default: module.TeachingReferenceReviewPanel })));
const TeachingSessionBriefPanel = lazy(() => import('./TeachingSessionBriefPanel').then((module) => ({ default: module.TeachingSessionBriefPanel })));
const TeachingSessionPanel = lazy(() => import('./TeachingSessionPanel').then((module) => ({ default: module.TeachingSessionPanel })));
const TeachingToolkitPathPanel = lazy(() => import('./TeachingToolkitPathPanel').then((module) => ({ default: module.TeachingToolkitPathPanel })));
const ViewModeControl = lazy(() => import('./ViewModeControl').then((module) => ({ default: module.ViewModeControl })));
const VocabularyPlanningPanel = lazy(() => import('./VocabularyPlanningPanel').then((module) => ({ default: module.VocabularyPlanningPanel })));
const WorkedExamplesPanel = lazy(() => import('./WorkedExamplesPanel').then((module) => ({ default: module.WorkedExamplesPanel })));
const WorkspaceIndexPanel = lazy(() => import('./WorkspaceIndexPanel').then((module) => ({ default: module.WorkspaceIndexPanel })));
const WorkspaceRegistryAuditPanel = lazy(() => import('./WorkspaceRegistryAuditPanel').then((module) => ({ default: module.WorkspaceRegistryAuditPanel })));

type WorkspaceEntry = { id: string; label: string; description: string };
type WorkspaceGroup = { id: string; label: string; eyebrow: string; description: string; entries: WorkspaceEntry[] };

const groups: WorkspaceGroup[] = [
  {
    id: 'teach', label: 'Learn & teach', eyebrow: 'EVERYDAY LEARNING', description: 'Lessons, work, feedback and progress.',
    entries: [
      { id: 'workspace-start', label: 'Guided setup', description: 'See the next missing record in the teaching journey.' },
      { id: 'workspace-learning', label: 'Learners and study plans', description: 'Create a learner, choose a subject and set a goal.' },
      { id: 'workspace-lesson-draft', label: 'Prepare a lesson', description: 'Draft a lesson linked to accepted curriculum evidence.' },
      { id: 'workspace-lesson-reader', label: 'Open a lesson', description: 'Read the selected lesson in a clean teaching view.' },
      { id: 'workspace-learning-checks', label: 'Practice and review', description: 'Create a check, record an answer and review it.' },
      { id: 'workspace-progress', label: 'Progress record', description: 'See recorded learning evidence without invented scores.' },
      { id: 'workspace-feedback-hub', label: 'Share an idea', description: 'Help improve MA-Teacher without sharing private information.' },
      { id: 'workspace-teaching-sessions', label: 'Record a teaching session', description: 'Record what was taught against the exact lesson.' },
      { id: 'workspace-teaching-operations', label: 'Teaching pipeline', description: 'See what evidence is missing from a lesson journey.' },
    ],
  },
  {
    id: 'plan', label: 'Make lessons', eyebrow: 'TEACHER TOOLS', description: 'Plan lessons, questions and helpful feedback.',
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
    id: 'curriculum', label: 'Subjects', eyebrow: 'WHAT TO TEACH', description: 'Subjects, age guidance and trusted sources.',
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
    id: 'library', label: 'Teacher library', eyebrow: 'TEACHER REFERENCE', description: 'Extra planning material and evidence checks.',
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
    id: 'manage', label: 'Keep safe', eyebrow: 'TEACHER AND IT TOOLS', description: 'Backups, privacy, access and readiness.',
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
  'workspace-learning': () => (
    <>
      <LearningWorkspacePanel />
      <ClassroomPanel />
    </>
  ),
  'workspace-lesson-draft': () => <LessonDraftPanel />,
  'workspace-proposals': () => <TeachingProposalPanel />,
  'workspace-lesson-reader': () => <LessonReaderPanel />,
  'workspace-lesson-review-records': () => <LessonReviewRecordPanel />,
  'workspace-teaching-sessions': () => <TeachingSessionPanel />,
  'workspace-teaching-operations': () => <TeachingOperationsPanel />,
  'workspace-learning-checks': () => <LearningCheckPanel />,
  'workspace-misconception-response': () => <MisconceptionResponsePanel />,
  'workspace-progress': () => <LearningProgressPanel />,
  'workspace-feedback-hub': () => <FeedbackHubPanel />,
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
  'workspace-subjects': () => (
    <>
      <SubjectLensesPanel />
      <SafeCodeLabPanel />
    </>
  ),
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
const simpleIds = new Set(['workspace-start', 'workspace-lesson-reader', 'workspace-learning-checks', 'workspace-progress', 'workspace-subjects', 'workspace-feedback-hub']);
const simpleGroup: WorkspaceGroup = { id: 'simple', label: 'Learn', eyebrow: 'YOUR LEARNING SPACE', description: 'Lessons, work, progress and ideas.', entries: allEntries.filter((entry) => simpleIds.has(entry.id)).map(({ groupId: _groupId, ...entry }) => entry) };
type AppView = 'simple' | 'teacher';

function Home({ open, view }: { open: (id: string) => void; view: AppView }) {
  const actions = view === 'simple' ? [
    ['workspace-lesson-reader', 'Open my lesson', 'Read the lesson chosen by your teacher.'],
    ['workspace-learning-checks', 'Send in my work', 'Type an answer or add one file for a person to review.'],
    ['workspace-progress', 'See my progress', 'See saved work and feedback. These are not grades.'],
    ['workspace-subjects', 'Explore subjects', 'See different ways people think and learn.'],
    ['workspace-feedback-hub', 'Share an idea', 'Tell us what is easy, hard, broken or missing.'],
    ['workspace-start', 'Help me start', 'See the next step and what a teacher needs to set up.'],
  ] as const : [
    ['workspace-learning', 'Set up a learner', 'Create a local learner and a clear study plan.'],
    ['workspace-lesson-draft', 'Prepare a lesson', 'Build a lesson from reviewed curriculum evidence.'],
    ['workspace-lesson-reader', 'Teach a saved lesson', 'Open the selected lesson in a clean view.'],
    ['workspace-learning-checks', 'Practice and review', 'Record work and give human feedback.'],
    ['workspace-coverage', 'Check subject coverage', 'See which sources are ready, partial or missing.'],
    ['workspace-feedback-hub', 'Review user feedback', 'Invite safe feedback and use it to improve the app.'],
  ] as const;
  return <section className="teacher-home" aria-labelledby="teacher-home-title">
    <div className="teacher-home__hero"><img src={tutorIcon} alt="MA-Teacher potato-shaped tutor wearing a graduation cap" /><div><p>{view === 'simple' ? 'YOUR LEARNING SPACE' : 'TEACHER WORKSPACE'}</p><h1 id="teacher-home-title">What would you like to do?</h1><span>Pick one thing. We will only show the tools you need.</span></div></div>
    <div className="teacher-home__actions">{actions.map(([id, label, description], index) => <button type="button" key={id} onClick={() => open(id)}><b>{String(index + 1).padStart(2, '0')}</b><span><strong>{label}</strong><small>{description}</small></span><em>OPEN</em></button>)}</div>
    <aside><strong>Your work stays on this computer.</strong><span>A person checks learning and gives feedback. MA-Teacher does not guess grades or ability.</span></aside>
  </section>;
}

function WorkspaceLoading({ label }: { label: string }) {
  return <section className="teacher-workspace-loading" role="status" aria-live="polite" aria-busy="true">
    <span aria-hidden="true">OPENING</span>
    <h2>{label}</h2>
    <p>Getting this page ready. Your saved work is not changing.</p>
  </section>;
}

export function TeacherShell() {
  const [view, setView] = useState<AppView>(() => window.localStorage.getItem('ma-teacher-view') === 'teacher' ? 'teacher' : 'simple');
  const initialHash = window.location.hash.slice(1);
  const [activeId, setActiveId] = useState(validIds.has(initialHash) ? initialHash : 'teacher-home');
  const visibleGroups = view === 'teacher' ? groups : [simpleGroup];
  const activeEntry = allEntries.find((entry) => entry.id === activeId);
  const activeGroup = visibleGroups.find((group) => group.entries.some((entry) => entry.id === activeId)) ?? visibleGroups[0];
  const activeRenderer = surfaceRenderers[activeId];

  function open(id: string, addHistory = true) {
    const next = validIds.has(id) ? id : 'teacher-home';
    setActiveId(next);
    const hash = next === 'teacher-home' ? '' : `#${next}`;
    if (addHistory) window.history.pushState({ workspace: next }, '', `${window.location.pathname}${window.location.search}${hash}`);
  }

  function changeView(next: AppView) {
    setView(next); window.localStorage.setItem('ma-teacher-view', next);
    if (next === 'simple' && activeId !== 'teacher-home' && !simpleIds.has(activeId)) open('teacher-home');
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
      <div className="teacher-topbar__quick"><button type="button" onClick={() => open('workspace-start')}>Help</button><button type="button" onClick={() => open('workspace-feedback-hub')}>Feedback</button><details><summary>Guides</summary><div><a href="https://github.com/neuro-1977/MA-Teacher/blob/main/docs/STUDENT_GUIDE.md" target="_blank" rel="noreferrer">Student guide</a><a href="https://github.com/neuro-1977/MA-Teacher/blob/main/docs/TEACHER_GUIDE.md" target="_blank" rel="noreferrer">Teacher guide</a><a href="https://github.com/neuro-1977/MA-Teacher/blob/main/CHANGELOG.md" target="_blank" rel="noreferrer">What changed</a><a href="https://github.com/neuro-1977/MA-Teacher/blob/main/docs/PRIVACY_AND_SAFETY.md" target="_blank" rel="noreferrer">Privacy and safety</a></div></details></div>
      <div className="teacher-view-switch" role="group" aria-label="Choose simple or teacher view"><button type="button" className={view === 'simple' ? 'is-active' : ''} onClick={() => changeView('simple')}>Simple</button><button type="button" className={view === 'teacher' ? 'is-active' : ''} onClick={() => changeView('teacher')}>Teacher</button><InfoTip label="About these views">Simple view hides planning and technical tools. Teacher view shows them. This is not a password or security lock.</InfoTip></div>
      {view === 'teacher' ? <nav aria-label="Teacher areas">{groups.map((group) => <button type="button" key={group.id} className={activeGroup.id === group.id && activeEntry ? 'is-active' : ''} onClick={() => open(group.entries[0].id)}>{group.label}</button>)}</nav> : null}
    </header>
    <div className="teacher-layout">
      <aside className="teacher-sidebar" aria-label={`${activeGroup.label} workspaces`}>
        <header><p>{activeGroup.eyebrow}</p><h2>{activeGroup.label}</h2><span>{activeGroup.description}</span></header>
        <select aria-label="Choose workspace" value={activeEntry?.id ?? ''} onChange={(event) => open(event.target.value)}><option value="" disabled>Choose a workspace</option>{activeGroup.entries.map((entry) => <option key={entry.id} value={entry.id}>{entry.label}</option>)}</select>
        <div>{activeGroup.entries.map((entry) => <button type="button" key={entry.id} className={activeId === entry.id ? 'is-active' : ''} onClick={() => open(entry.id)}><strong>{entry.label}</strong><small>{entry.description}</small></button>)}</div>
      </aside>
      <main id="teacher-main" className="teacher-main" tabIndex={-1}>
        {activeEntry ? <header className="teacher-workspace-heading"><div><p>{activeGroup.label.toUpperCase()}</p><h1>{activeEntry.label}</h1><span>{activeEntry.description}</span></div>{effect ? <div className="teacher-effect"><strong data-effect={effect}>{workspaceEffectLabels[effect]}</strong><InfoTip label="What does this label mean?">This tells you whether this page can save a record, create a backup, or only read information.</InfoTip></div> : null}</header> : null}
        {activeId === 'workspace-lesson-reader' ? <div className="teacher-workspace-tools"><PrintLessonControl /></div> : null}
        <div className="teacher-workspace">{activeRenderer && activeEntry ? <SurfaceErrorBoundary name={activeEntry.label}><Suspense fallback={<WorkspaceLoading label={activeEntry.label} />}>{activeRenderer()}</Suspense></SurfaceErrorBoundary> : <Home open={open} view={view} />}</div>
      </main>
    </div>
  </div>;
}
