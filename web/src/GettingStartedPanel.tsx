import { useEffect, useMemo, useRef, useState } from 'react';
import './getting-started.css';
import { focusWorkspaceSurface } from './workspace-navigation';

type Step = { id: string; title: string; explanation: string; count: number; anchor: string; label: string };

export function GettingStartedPanel() {
  const [loaded, setLoaded] = useState(false); const [error, setError] = useState('');
  const [refreshing, setRefreshing] = useState(false); const refreshActive = useRef(false);
  const [counts, setCounts] = useState({ learners: 0, plans: 0, candidates: 0, lessons: 0, lessonReviews: 0, checks: 0, attempts: 0, attemptReviews: 0 });
  async function refresh() {
    if (refreshActive.current) return;
    refreshActive.current = true; setRefreshing(true);
    setError('');
    try {
      const [workspaceResponse, candidateResponse, checkResponse, lessonReviewResponse] = await Promise.all([
        fetch('/api/teaching/workspace'), fetch('/api/curriculum/candidates'), fetch('/api/teaching/checks'), fetch('/api/teaching/lesson-reviews'),
      ]);
      const workspace = await workspaceResponse.json(); const candidateData = await candidateResponse.json(); const checkData = await checkResponse.json(); const lessonReviewData = await lessonReviewResponse.json();
      if (!workspaceResponse.ok || !workspace.ok || !candidateResponse.ok || !candidateData.ok || !checkResponse.ok || !checkData.ok || !lessonReviewResponse.ok || !lessonReviewData.ok) throw new Error('One or more local evidence APIs refused the request.');
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
    { id: 'learner', title: 'Create a local learner', explanation: 'A local profile owns plans, lessons and attempts. Keep identifying information minimal.', count: counts.learners, anchor: 'workspace-learning', label: 'Open learner workspace' },
    { id: 'plan', title: 'Create an active study plan', explanation: 'Choose one subject, stage and clear learning goal. A plan is not curriculum evidence.', count: counts.plans, anchor: 'workspace-learning', label: 'Open study plans' },
    { id: 'evidence', title: 'Capture and review official curriculum', explanation: 'Refresh an allowlisted source, extract candidates, then explicitly accept relevant statements.', count: counts.candidates, anchor: 'workspace-learning', label: 'Open curriculum evidence' },
    { id: 'lesson', title: 'Draft an evidence-linked lesson', explanation: 'Write the objective and teaching sequence against accepted subject and stage evidence.', count: counts.lessons, anchor: 'workspace-lesson-draft', label: 'Open lesson drafting' },
    { id: 'lesson-review', title: 'Review the exact saved lesson', explanation: 'Inspect all criteria against the saved fingerprint. Practice stays locked until a current approved-for-use review exists.', count: counts.lessonReviews, anchor: 'workspace-lesson-review-records', label: 'Open lesson review' },
    { id: 'check', title: 'Author a manual practice check', explanation: 'Write a prompt and success criteria linked to evidence already used by the lesson.', count: counts.checks, anchor: 'workspace-learning-checks', label: 'Open practice authoring' },
    { id: 'attempt', title: 'Submit a learner response', explanation: 'The response remains unreviewed and unscored until a human examines it.', count: counts.attempts, anchor: 'workspace-learning-checks', label: 'Open learner practice' },
    { id: 'review', title: 'Record a human response review', explanation: 'Apply one bounded outcome and feedback to one attempt. Do not infer broad mastery.', count: counts.attemptReviews, anchor: 'workspace-learning-checks', label: 'Open human review' },
  ], [counts]);
  const firstEmpty = steps.find(step => step.count === 0);
  function go(anchor: string) { focusWorkspaceSurface(anchor); }
  return <section id="workspace-start" className="getting-started-shell" aria-labelledby="getting-started-title">
    <header><div><p>START HERE · RECORDS, NOT BUTTON CLICKS</p><h2 id="getting-started-title">Follow the evidence path in order.</h2></div><button onClick={() => void refresh()} disabled={refreshing}>{refreshing ? 'Refreshing counts...' : 'Refresh local counts'}</button></header>
    {error && <div className="getting-started-error"><strong>Local journey unavailable</strong>{error}</div>}
    <section className="getting-started-preflight" aria-labelledby="getting-started-preflight-title">
      <header><div><p>PREPARE BEFORE RECORDS</p><h3 id="getting-started-preflight-title">Orient without claiming progress.</h3></div><span>Browser-memory navigation only</span></header>
      <div>
        <article><strong>Choose the right view</strong><p>Use Teacher workspace for everything, Planning focus for preparation references, or Lesson focus for the current lesson and practice.</p><button type="button" onClick={() => go('workspace-view-mode')}>Open view selector</button></article>
        <article><strong>Inspect side effects first</strong><p>Search every registered surface and filter read-only, database-write, backup-write, or clipboard-optional destinations.</p><button type="button" onClick={() => go('workspace-index')}>Open workspace index</button></article>
        <article><strong>Check navigation structure</strong><p>Run the explicit local registry audit when you need evidence that registered IDs have mounted destinations.</p><button type="button" onClick={() => go('workspace-registry-audit')}>Open registry audit</button></article>
      </div>
      <footer>Opening these surfaces does not create a record, satisfy a step, approve content, or prove the workspace works.</footer>
    </section>
    <div className="getting-started-grid">{steps.map((step, index) => <article key={step.id} className={step.count > 0 ? 'present' : firstEmpty?.id === step.id ? 'first-empty' : ''}>
      <span>{index + 1}</span><div><header><strong>{step.title}</strong><em>{step.count > 0 ? `${step.count} ${step.count === 1 ? 'RECORD' : 'RECORDS'} PRESENT` : firstEmpty?.id === step.id ? 'FIRST EMPTY RECORD TYPE' : 'NO RECORDS'}</em></header><p>{step.explanation}</p><button onClick={() => go(step.anchor)}>{step.label}</button></div>
    </article>)}</div>
    <footer>{!loaded ? 'Counts are unavailable; no record type is assumed present.' : firstEmpty ? `First empty record type: ${firstEmpty.title}. Global counts do not prove the visible records belong to one linked learner, plan, evidence, lesson, check, attempt, and review chain.` : 'Every record type has at least one record. This does not prove one linked evidence chain, curriculum quality, lesson quality, retention, accessibility, or project completion.'}</footer>
  </section>;
}
