import { useEffect, useMemo, useRef, useState } from 'react';
import './getting-started.css';
import { focusWorkspaceSurface } from './workspace-navigation';

type Step = { id: string; title: string; explanation: string; count: number; anchor: string; label: string };

type LocalEvidencePayload = { ok?: boolean; error?: string; [key: string]: unknown };
type WorkspacePayload = LocalEvidencePayload & {
  learners?: object[];
  studyPlans?: { status: string }[];
  lessonDrafts?: object[];
};
type CandidatePayload = LocalEvidencePayload & { candidates?: { reviewState: string }[] };
type CheckPayload = LocalEvidencePayload & { checks?: object[]; attempts?: { reviewState: string }[] };
type LessonReviewPayload = LocalEvidencePayload & {
  lessons?: { latestDecision?: string; latestReviewCurrent: boolean }[];
};

async function readLocalEvidence<T extends LocalEvidencePayload>(endpoint: string): Promise<T> {
  const response = await fetch(endpoint, { cache: 'no-store', headers: { Accept: 'application/json' } });
  const body = await response.text();
  if (!body.trim()) {
    throw new Error('MA-Teacher could not read its saved steps. Press Check again. If this happens again, close MA-Teacher and open it again.');
  }

  let payload: T;
  try {
    payload = JSON.parse(body) as T;
  } catch {
    throw new Error('MA-Teacher received an incomplete reply while checking saved steps. Press Check again.');
  }

  if (!response.ok || payload.ok === false) {
    throw new Error(typeof payload.error === 'string' && payload.error.trim()
      ? payload.error
      : 'MA-Teacher could not check one of the saved steps. Press Check again.');
  }
  return payload;
}

export function GettingStartedPanel({ showTeacherHelp = false }: { showTeacherHelp?: boolean }) {
  const [loaded, setLoaded] = useState(false); const [error, setError] = useState('');
  const [refreshing, setRefreshing] = useState(false); const refreshActive = useRef(false);
  const [counts, setCounts] = useState({ learners: 0, plans: 0, candidates: 0, lessons: 0, lessonReviews: 0, checks: 0, attempts: 0, attemptReviews: 0 });
  async function refresh() {
    if (refreshActive.current) return;
    refreshActive.current = true; setRefreshing(true);
    setError('');
    try {
      const [workspace, candidateData, checkData, lessonReviewData] = await Promise.all([
        readLocalEvidence<WorkspacePayload>('/api/teaching/workspace'),
        readLocalEvidence<CandidatePayload>('/api/curriculum/candidates'),
        readLocalEvidence<CheckPayload>('/api/teaching/checks'),
        readLocalEvidence<LessonReviewPayload>('/api/teaching/lesson-reviews'),
      ]);
      const learners = Array.isArray(workspace.learners) ? workspace.learners : []; const plans = Array.isArray(workspace.studyPlans) ? workspace.studyPlans : [];
      const candidates = Array.isArray(candidateData.candidates) ? candidateData.candidates : []; const lessons = Array.isArray(workspace.lessonDrafts) ? workspace.lessonDrafts : [];
      const checks = Array.isArray(checkData.checks) ? checkData.checks : []; const attempts = Array.isArray(checkData.attempts) ? checkData.attempts : [];
      setCounts({ learners: learners.length, plans: plans.filter((value: { status: string }) => value.status === 'active').length,
        candidates: candidates.filter((value: { reviewState: string }) => value.reviewState === 'accepted').length,
        lessons: lessons.length, lessonReviews: (lessonReviewData.lessons ?? []).filter((value: { latestDecision?: string; latestReviewCurrent: boolean }) => value.latestDecision === 'approved-for-use' && value.latestReviewCurrent).length,
        checks: checks.length, attempts: attempts.length, attemptReviews: attempts.filter((value: { reviewState: string }) => value.reviewState === 'human-reviewed').length }); setLoaded(true);
    } catch (reason) { setLoaded(false); setError(reason instanceof Error ? reason.message : 'Unknown local API error'); }
    finally { refreshActive.current = false; setRefreshing(false); }
  }
  useEffect(() => { void refresh(); }, []);
  const steps = useMemo<Step[]>(() => [
    { id: 'learner', title: 'Add a learner', explanation: 'Use as little personal information as possible.', count: counts.learners, anchor: 'workspace-learning', label: 'Add a learner' },
    { id: 'plan', title: 'Make a learning plan', explanation: 'Pick a subject, an age stage, and one clear goal.', count: counts.plans, anchor: 'workspace-learning', label: 'Make a plan' },
    { id: 'evidence', title: 'Check the subject guide', explanation: 'A teacher checks a trusted curriculum source before making a lesson.', count: counts.candidates, anchor: 'workspace-learning', label: 'Check sources' },
    { id: 'lesson', title: 'Make a lesson', explanation: 'Write a lesson that matches the goal and checked source.', count: counts.lessons, anchor: 'workspace-lesson-draft', label: 'Make a lesson' },
    { id: 'lesson-review', title: 'Check the lesson', explanation: 'A teacher checks the saved lesson before anyone uses it.', count: counts.lessonReviews, anchor: 'workspace-lesson-review-records', label: 'Check a lesson' },
    { id: 'check', title: 'Add a practice question', explanation: 'Say what the learner should try and what good work may show.', count: counts.checks, anchor: 'workspace-learning-checks', label: 'Add practice' },
    { id: 'attempt', title: 'Send in work', explanation: 'Type an answer, add one file, or do both. A person will review it.', count: counts.attempts, anchor: 'workspace-learning-checks', label: 'Send in work' },
    { id: 'review', title: 'Give helpful feedback', explanation: 'A teacher reads the work and writes what to try next.', count: counts.attemptReviews, anchor: 'workspace-learning-checks', label: 'Review work' },
  ], [counts]);
  const firstEmpty = steps.find(step => step.count === 0);
  function go(anchor: string) { focusWorkspaceSurface(anchor); }
  return <section id="workspace-start" className="getting-started-shell" aria-labelledby="getting-started-title">
    <header><div><p>START HERE</p><h2 id="getting-started-title">Let us get ready to learn.</h2><span>Do these steps in order. A teacher does the setup and reviews the work.</span></div><button onClick={() => void refresh()} disabled={refreshing}>{refreshing ? 'Checking...' : 'Check again'}</button></header>
    {error && <div className="getting-started-error"><strong>We could not check your steps.</strong>{error}</div>}
    {showTeacherHelp && <section className="getting-started-preflight" aria-labelledby="getting-started-preflight-title">
      <header><div><p>TEACHER HELP</p><h3 id="getting-started-preflight-title">Need to check how the app is set up?</h3></div><span>These buttons do not change records</span></header>
      <div>
        <article><strong>Choose a view</strong><p>Simple view is calm and learner-friendly. Teacher view shows planning and record tools.</p><button type="button" onClick={() => go('workspace-view-mode')}>View options</button></article>
        <article><strong>Find every tool</strong><p>Search the full list and see which tools can save or change a record.</p><button type="button" onClick={() => go('workspace-index')}>Full tool list</button></article>
        <article><strong>Check the menu</strong><p>Use this technical check only when a page seems to be missing.</p><button type="button" onClick={() => go('workspace-registry-audit')}>Check the menu</button></article>
      </div>
      <footer>Opening these help pages does not save learning work or finish a step.</footer>
    </section>}
    <div className="getting-started-grid">{steps.map((step, index) => <article key={step.id} className={step.count > 0 ? 'present' : firstEmpty?.id === step.id ? 'first-empty' : ''}>
      <span>{index + 1}</span><div><header><strong>{step.title}</strong><em>{step.count > 0 ? `${step.count} SAVED` : firstEmpty?.id === step.id ? 'DO THIS NEXT' : 'NOT READY YET'}</em></header><p>{step.explanation}</p><button onClick={() => go(step.anchor)}>{step.label}</button></div>
    </article>)}</div>
    <footer>{!loaded ? 'We cannot see the saved steps yet.' : firstEmpty ? `Next step: ${firstEmpty.title}. A teacher should check that it belongs to the right learner and lesson.` : 'Every step has something saved. A teacher still checks that the records belong together and are ready to use.'}</footer>
  </section>;
}
