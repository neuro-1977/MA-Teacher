import { useEffect, useState } from 'react';
import './lesson-reader.css';

type Draft = { id: string; title: string; studyPlanId: string; evidenceState: string; status: string };
type Header = { id: string; studyPlanId: string; title: string; learningObjective: string; evidenceState: string; status: string; createdUtc: string; updatedUtc: string; learnerId: string; learnerDisplayName: string; subject: string; learningStage: string; studyGoal: string };
type Section = { sequence: number; kind: string; content: string };
type Evidence = { id: string; sourceRevisionId: number; subject: string; learningStage: string; statementText: string; sourceLocator: string; statementSha256: string; reviewState: string; evidenceRole: string };
type Detail = { ok: boolean; lesson: Header; sections: Section[]; evidence: Evidence[]; error?: string };

export function LessonReaderPanel() {
  const [drafts, setDrafts] = useState<Draft[]>([]);
  const [selected, setSelected] = useState('');
  const [detail, setDetail] = useState<Detail | null>(null);
  const [state, setState] = useState('No lesson selected');

  async function loadDrafts() {
    try {
      const response = await fetch('/api/teaching/workspace'); const payload = await response.json();
      if (!response.ok || !payload.ok) throw new Error(payload.error || `HTTP ${response.status}`);
      const next = Array.isArray(payload.lessonDrafts) ? payload.lessonDrafts : [];
      setDrafts(next); setSelected(current => current || next[0]?.id || '');
    } catch (error) { setState(`Lesson index unavailable: ${error instanceof Error ? error.message : 'unknown error'}`); }
  }

  async function loadDetail(id: string) {
    if (!id) { setDetail(null); setState('No lesson selected'); return; }
    setState('Loading exact saved lesson...');
    try {
      const response = await fetch(`/api/teaching/lessons/${encodeURIComponent(id)}`); const payload = await response.json();
      if (!response.ok || !payload.ok) throw new Error(payload.error || `HTTP ${response.status}`);
      setDetail(payload); setState('Exact saved draft loaded');
    } catch (error) { setDetail(null); setState(`Lesson refused: ${error instanceof Error ? error.message : 'unknown error'}`); }
  }

  useEffect(() => { void loadDrafts(); }, []);
  useEffect(() => { void loadDetail(selected); }, [selected]);

  return <section id="workspace-lesson-reader" className="lesson-reader-shell" aria-labelledby="lesson-reader-title">
    <header><div><p>LESSON READER · EXACT SAVED STATE</p><h2 id="lesson-reader-title">Teach from the draft without losing its evidence.</h2></div>
      <div className="lesson-reader-actions"><select value={selected} onChange={event => setSelected(event.target.value)}><option value="">Select a draft</option>{drafts.map(draft => <option key={draft.id} value={draft.id}>{draft.title} · {draft.id}</option>)}</select><button onClick={() => window.print()} disabled={!detail}>Print view</button></div>
    </header>
    {!detail ? <div className="lesson-reader-empty">{state}</div> : <div className="lesson-reader-paper">
      <div className="lesson-reader-status"><span>{detail.lesson.status}</span><span>{detail.lesson.evidenceState}</span><span>{detail.lesson.subject} · {detail.lesson.learningStage}</span></div>
      <h3>{detail.lesson.title}</h3><p className="lesson-objective"><strong>Learning objective</strong>{detail.lesson.learningObjective}</p>
      <div className="lesson-context"><span><b>Learner</b>{detail.lesson.learnerDisplayName}</span><span><b>Study goal</b>{detail.lesson.studyGoal}</span><span><b>Plan</b>{detail.lesson.studyPlanId}</span></div>
      <ol className="lesson-sequence">{detail.sections.map(section => <li key={`${section.sequence}-${section.kind}`}><span>{section.sequence}</span><article><strong>{section.kind}</strong><p>{section.content}</p></article></li>)}</ol>
      <aside className="lesson-provenance"><h4>Accepted curriculum links</h4>{detail.evidence.map(item => <article key={item.id}><header><strong>{item.subject} · {item.learningStage}</strong><span>{item.reviewState} · {item.evidenceRole}</span></header><p>{item.statementText}</p><footer>revision {item.sourceRevisionId} · {item.sourceLocator} · sha256 {item.statementSha256.slice(0, 16)}...</footer></article>)}</aside>
      <footer className="lesson-reader-warning">This is an operator-authored draft. Curriculum links do not verify every subject fact, activity, explanation, accessibility choice, or learner outcome.</footer>
    </div>}
  </section>;
}
