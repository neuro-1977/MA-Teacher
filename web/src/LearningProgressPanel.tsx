import { useEffect, useMemo, useState } from 'react';
import { InfoTip } from './InfoTip';
import './learning-progress.css';

type Summary = { learnerId: string; learnerDisplayName: string; subject: string; learningStage: string; attempts: number; unreviewed: number; met: number; partiallyMet: number; notYet: number; invalid: number; lastSubmittedUtc?: string };
type Entry = { attemptId: string; checkId: string; lessonId: string; lessonTitle: string; subject: string; learningStage: string; learnerId: string; learnerDisplayName: string; prompt: string; successCriteria: string; responseText: string; submittedUtc: string; reviewState: string; outcome?: string; feedback?: string; reviewedUtc?: string; evidenceCount: number; evidenceNeed: string };
type Overview = { ok: boolean; interpretationState: string; summaries: Summary[]; entries: Entry[]; boundaries: string[]; error?: string };

function nextStep(attempts: number, unreviewed: number, reviewed: number, subjects: number) {
  if (attempts === 0) return { title: 'Save your first piece of work', detail: 'Open Practice and review. Answer one question or add one file for a person to read.' };
  if (unreviewed > 0) return { title: 'Your work is waiting safely', detail: `${unreviewed} ${unreviewed === 1 ? 'piece is' : 'pieces are'} ready for a teacher to review. You do not need to send ${unreviewed === 1 ? 'it' : 'them'} again.` };
  if (reviewed === 0) return { title: 'Ask a teacher to review your work', detail: 'A person checks work against the lesson goal. MA-Teacher does not guess a grade.' };
  if (subjects < 2) return { title: 'Try another subject when you are ready', detail: 'Exploring a different subject can show another way you learn and think.' };
  return { title: 'Read your feedback and choose one small step', detail: 'Use the teacher comments below. Progress means learning what to try next, not collecting points.' };
}

export function LearningProgressPanel({ showTeacherDetails = false }: { showTeacherDetails?: boolean }) {
  const [overview, setOverview] = useState<Overview | null>(null);
  const [learner, setLearner] = useState('all');
  const [subject, setSubject] = useState('all');
  const [state, setState] = useState('Loading saved work...');
  const [isLoading, setIsLoading] = useState(true);

  async function refresh() {
    setIsLoading(true);
    setState('Checking saved work...');
    try {
      const response = await fetch('/api/learning/progress');
      const body = await response.text();
      if (!body.trim()) throw new Error(`The local service returned an empty reply (HTTP ${response.status}). Try again. If it keeps happening, ask a teacher to restart MA-Teacher.`);
      let payload: Overview;
      try { payload = JSON.parse(body) as Overview; }
      catch { throw new Error(`The local service returned a reply MA-Teacher could not read (HTTP ${response.status}). No learning work was changed.`); }
      if (!response.ok || !payload.ok) throw new Error(payload.error || `The local service returned HTTP ${response.status}.`);
      setOverview(payload);
      setState('Saved work is up to date.');
    } catch (error) {
      setState(error instanceof Error ? error.message : 'Saved work is unavailable. No learning work was changed.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => { void refresh(); }, []);
  const learners = useMemo(() => ['all', ...Array.from(new Set((overview?.summaries ?? []).map(value => value.learnerId)))], [overview]);
  const subjects = useMemo(() => ['all', ...Array.from(new Set((overview?.summaries ?? []).filter(value => learner === 'all' || value.learnerId === learner).map(value => value.subject)))], [overview, learner]);
  const summaries = (overview?.summaries ?? []).filter(value => (learner === 'all' || value.learnerId === learner) && (subject === 'all' || value.subject === subject));
  const entries = (overview?.entries ?? []).filter(value => (learner === 'all' || value.learnerId === learner) && (subject === 'all' || value.subject === subject));
  const totals = summaries.reduce((sum, value) => ({ attempts: sum.attempts + value.attempts, unreviewed: sum.unreviewed + value.unreviewed, met: sum.met + value.met, partiallyMet: sum.partiallyMet + value.partiallyMet, notYet: sum.notYet + value.notYet, invalid: sum.invalid + value.invalid }), { attempts: 0, unreviewed: 0, met: 0, partiallyMet: 0, notYet: 0, invalid: 0 });
  const reviewed = totals.met + totals.partiallyMet + totals.notYet + totals.invalid;
  const distinctSubjects = new Set(summaries.map(value => value.subject)).size;
  const guidance = nextStep(totals.attempts, totals.unreviewed, reviewed, distinctSubjects);
  const markers = [
    ['First step', totals.attempts >= 1, 'Saved your first piece of work.'],
    ['Feedback found', reviewed >= 1, 'A person reviewed some work.'],
    ['Kept practising', totals.attempts >= 5, 'Saved five pieces of work.'],
    ['Subject explorer', distinctSubjects >= 2, 'Tried work in two subjects.'],
  ] as const;

  return <section id="workspace-progress" className="progress-shell" aria-labelledby="progress-title">
    <header><div><p>YOUR LEARNING JOURNEY</p><h2 id="progress-title">See your work and feedback.</h2><span>These numbers show saved activity. They are not grades.</span></div><button onClick={() => void refresh()} disabled={isLoading}>{isLoading ? 'Checking...' : 'Check again'}</button></header>
    <output className={`progress-state${overview ? '' : ' has-no-data'}`} aria-live="polite">{state}</output>
    <div className="progress-boundary"><strong>{overview?.interpretationState ?? 'Evidence only'}</strong> We never use these counts to guess your ability, rank, or final result. <InfoTip label="Why are there no scores?">A person needs to read the work and use the lesson goal. Counting attempts alone cannot show what someone understands.</InfoTip></div>
    <div className="progress-filters"><label>Learner<select value={learner} onChange={event => { setLearner(event.target.value); setSubject('all'); }}>{learners.map(value => <option key={value} value={value}>{value === 'all' ? 'All learners' : overview?.summaries.find(item => item.learnerId === value)?.learnerDisplayName}</option>)}</select></label><label>Subject<select value={subject} onChange={event => setSubject(event.target.value)}>{subjects.map(value => <option key={value} value={value}>{value === 'all' ? 'All subjects' : value}</option>)}</select></label></div>
    <section className="progress-next" aria-labelledby="progress-next-title"><span aria-hidden="true">→</span><div><p>WHAT HAPPENS NEXT</p><h3 id="progress-next-title">{guidance.title}</h3><small>{guidance.detail}</small></div></section>
    <div className="progress-at-a-glance" aria-label="Saved learning activity"><article><b>{totals.attempts}</b><span>Work saved</span></article><article><b>{totals.unreviewed}</b><span>Waiting for review</span></article><article><b>{reviewed}</b><span>Reviewed by a person</span></article><article><b>{distinctSubjects}</b><span>Subjects explored</span></article></div>
    <section className="progress-markers" aria-labelledby="progress-markers-title"><header><div><p>TRAIL MARKERS</p><h3 id="progress-markers-title">Small steps worth noticing</h3></div><InfoTip label="About trail markers">Trail markers celebrate saved activity only. They do not unlock content or measure learning.</InfoTip></header><div>{markers.map(([label, earned, detail]) => <article key={label} className={earned ? 'is-earned' : ''} aria-label={`${label}: ${earned ? 'reached' : 'not reached yet'}`}><span aria-hidden="true">{earned ? '★' : '○'}</span><strong>{label}</strong><small>{detail}</small><em>{earned ? 'Reached' : 'Not yet'}</em></article>)}</div></section>
    {summaries.length === 0 ? <section className="progress-empty"><span aria-hidden="true">◇</span><div><h3>No saved work here yet</h3><p>That is okay. Choose <strong>Practice and review</strong> when you are ready to save your first answer or file.</p></div></section> : <div className="progress-summary-grid">{summaries.map(value => <article key={`${value.learnerId}-${value.subject}-${value.learningStage}`}><header><strong>{value.learnerDisplayName}</strong><span>{value.subject} · {value.learningStage}</span></header><div><span><b>{value.attempts}</b>tried</span><span><b>{value.unreviewed}</b>waiting</span><span><b>{value.met}</b>met</span><span><b>{value.partiallyMet}</b>nearly</span><span><b>{value.notYet}</b>try next</span><span><b>{value.invalid}</b>could not check</span></div><footer>{value.lastSubmittedUtc ? `Last work saved ${new Date(value.lastSubmittedUtc).toLocaleString()}` : 'No work saved yet'}</footer></article>)}</div>}
    {showTeacherDetails ? <details className="progress-ledger"><summary>Teacher details: open the full work record</summary><div><h3>Saved work and review evidence</h3>{entries.length === 0 ? <p>No matching work.</p> : entries.map(value => <article key={value.attemptId}><header><div><strong>{value.learnerDisplayName} · {value.subject}</strong><span>{value.lessonTitle} · {value.learningStage}</span></div><em>{value.reviewState}{value.outcome ? ` · ${value.outcome}` : ''}</em></header><dl><dt>Question</dt><dd>{value.prompt}</dd><dt>What to look for</dt><dd>{value.successCriteria}</dd><dt>Answer</dt><dd>{value.responseText}</dd>{value.feedback && <><dt>Feedback</dt><dd>{value.feedback}</dd></>}</dl><footer><span>{value.evidenceCount} curriculum link{value.evidenceCount === 1 ? '' : 's'}</span><strong>{value.evidenceNeed}</strong></footer></article>)}</div></details> : <p className="progress-ledger-note"><strong>Full work stays in Teacher view.</strong> Ask a teacher when you want to read the saved answer and detailed feedback together.</p>}
  </section>;
}
