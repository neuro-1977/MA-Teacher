import { FormEvent, useEffect, useMemo, useState } from 'react';
import './lesson-draft.css';

type StudyPlan = { id: string; learnerId: string; subject: string; learningStage: string; goal: string; status: string };
type CurriculumCandidate = { id: string; subject: string; learningStage: string; statementText: string; reviewState: string };
type LessonDraft = { id: string; studyPlanId: string; title: string; learningObjective: string; evidenceState: string; status: string; sectionCount: number; evidenceCount: number };
type SectionDraft = { kind: string; content: string };

const sectionKinds = ['retrieval', 'explanation', 'worked-example', 'guided-practice', 'independent-practice', 'check', 'extension', 'reflection'];
const initialSections: SectionDraft[] = [
  { kind: 'retrieval', content: '' },
  { kind: 'explanation', content: '' },
  { kind: 'guided-practice', content: '' },
  { kind: 'check', content: '' },
];

function asArray<T>(value: unknown): T[] {
  return Array.isArray(value) ? (value as T[]) : [];
}

export function LessonDraftPanel() {
  const [plans, setPlans] = useState<StudyPlan[]>([]);
  const [candidates, setCandidates] = useState<CurriculumCandidate[]>([]);
  const [lessons, setLessons] = useState<LessonDraft[]>([]);
  const [lessonId, setLessonId] = useState('');
  const [planId, setPlanId] = useState('');
  const [title, setTitle] = useState('');
  const [objective, setObjective] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [sections, setSections] = useState<SectionDraft[]>(initialSections);
  const [status, setStatus] = useState('Not loaded');

  async function refresh() {
    setStatus('Loading local evidence workspace...');
    try {
      const [workspaceResponse, candidateResponse] = await Promise.all([
        fetch('/api/teaching/workspace'),
        fetch('/api/curriculum/candidates'),
      ]);
      const workspace = await workspaceResponse.json();
      const candidateData = await candidateResponse.json();
      const nextPlans = asArray<StudyPlan>(workspace.studyPlans).filter(plan => plan.status === 'active');
      setPlans(nextPlans);
      setLessons(asArray<LessonDraft>(workspace.lessonDrafts));
      setCandidates(asArray<CurriculumCandidate>(candidateData.candidates).filter(candidate => candidate.reviewState === 'accepted'));
      setPlanId(current => current || nextPlans[0]?.id || '');
      setStatus('Local evidence workspace loaded');
    } catch (error) {
      setStatus(`Load failed: ${error instanceof Error ? error.message : 'unknown error'}`);
    }
  }

  useEffect(() => { void refresh(); }, []);

  const activePlan = plans.find(plan => plan.id === planId);
  const compatibleCandidates = useMemo(() => {
    if (!activePlan) return candidates;
    const subject = activePlan.subject.toLowerCase();
    return candidates.filter(candidate => {
      const candidateSubject = candidate.subject.toLowerCase();
      return subject === 'cross-curricular' || candidateSubject === 'framework' || candidateSubject === subject;
    });
  }, [activePlan, candidates]);

  function updateSection(index: number, patch: Partial<SectionDraft>) {
    setSections(current => current.map((section, position) => position === index ? { ...section, ...patch } : section));
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setStatus('Saving evidence-linked draft...');
    try {
      const response = await fetch('/api/teaching/lesson-drafts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-MA-Teacher-Intent': 'draft-evidence-linked-lesson' },
        body: JSON.stringify({
          id: lessonId,
          studyPlanId: planId,
          title,
          learningObjective: objective,
          sections: sections.filter(section => section.content.trim()),
          curriculumCandidateIds: selected,
        }),
      });
      const result = await response.json();
      if (!response.ok || !result.ok) throw new Error(result.error || result.state || `HTTP ${response.status}`);
      setStatus(result.state === 'already-present' ? 'Exact draft already exists' : 'Draft saved; subject facts remain unverified');
      await refresh();
    } catch (error) {
      setStatus(`Draft refused: ${error instanceof Error ? error.message : 'unknown error'}`);
    }
  }

  return <section id="workspace-lesson-draft" className="lesson-draft-shell" aria-labelledby="lesson-draft-title">
    <header className="lesson-draft-header">
      <div>
        <p className="lesson-draft-kicker">EVIDENCE-LINKED LESSON WORKBENCH</p>
        <h2 id="lesson-draft-title">Draft from reviewed curriculum evidence</h2>
        <p>Manual drafting only. MA-Teacher will not invent curriculum facts or mark this lesson classroom-ready.</p>
      </div>
      <button type="button" onClick={() => void refresh()}>Refresh local state</button>
    </header>

    <form className="lesson-draft-grid" onSubmit={submit}>
      <div className="lesson-draft-editor">
        <label>Stable lesson ID<input value={lessonId} onChange={event => setLessonId(event.target.value)} placeholder="science-ks3-cells-01" required /></label>
        <label>Active study plan<select value={planId} onChange={event => { setPlanId(event.target.value); setSelected([]); }} required>
          <option value="">Select a study plan</option>
          {plans.map(plan => <option key={plan.id} value={plan.id}>{plan.subject} · {plan.learningStage} · {plan.goal}</option>)}
        </select></label>
        <label>Lesson title<input value={title} onChange={event => setTitle(event.target.value)} required /></label>
        <label>Learning objective<textarea value={objective} onChange={event => setObjective(event.target.value)} rows={3} required /></label>
        <div className="lesson-sections">
          <div className="lesson-section-heading"><h3>Teaching sequence</h3><button type="button" onClick={() => setSections(current => [...current, { kind: 'reflection', content: '' }])} disabled={sections.length >= 12}>Add section</button></div>
          {sections.map((section, index) => <div className="lesson-section-row" key={`${index}-${section.kind}`}>
            <span>{index + 1}</span>
            <select value={section.kind} onChange={event => updateSection(index, { kind: event.target.value })}>{sectionKinds.map(kind => <option key={kind}>{kind}</option>)}</select>
            <textarea value={section.content} onChange={event => updateSection(index, { content: event.target.value })} rows={2} placeholder="Operator-authored teaching content" />
            <button type="button" aria-label={`Remove section ${index + 1}`} onClick={() => setSections(current => current.filter((_, position) => position !== index))} disabled={sections.length <= 1}>×</button>
          </div>)}
        </div>
      </div>

      <aside className="curriculum-evidence-picker">
        <h3>Accepted curriculum evidence</h3>
        <p>Select 1-20 reviewed statements. Stage compatibility is enforced again by the host.</p>
        {compatibleCandidates.length === 0 && <div className="empty-evidence">No accepted subject-compatible candidates yet. Capture, extract, and review official sources first.</div>}
        {compatibleCandidates.map(candidate => <label className="candidate-choice" key={candidate.id}>
          <input type="checkbox" checked={selected.includes(candidate.id)} onChange={event => setSelected(current => event.target.checked ? [...current, candidate.id] : current.filter(id => id !== candidate.id))} disabled={!selected.includes(candidate.id) && selected.length >= 20} />
          <span><strong>{candidate.subject} · {candidate.learningStage}</strong>{candidate.statementText}</span>
        </label>)}
        <button className="save-lesson" type="submit" disabled={!planId || selected.length === 0}>Save draft with {selected.length} evidence link{selected.length === 1 ? '' : 's'}</button>
        <output>{status}</output>
      </aside>
    </form>

    <div className="lesson-ledger">
      <h3>Draft ledger</h3>
      {lessons.length === 0 ? <p>No lesson drafts recorded.</p> : lessons.map(lesson => <article key={lesson.id}>
        <div><strong>{lesson.title}</strong><span>{lesson.id} · {lesson.studyPlanId}</span></div>
        <p>{lesson.learningObjective}</p>
        <footer><span>{lesson.sectionCount} sections</span><span>{lesson.evidenceCount} evidence links</span><span>{lesson.status}</span><span>{lesson.evidenceState}</span></footer>
      </article>)}
    </div>
  </section>;
}
