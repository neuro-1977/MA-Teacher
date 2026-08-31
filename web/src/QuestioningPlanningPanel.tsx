import { useMemo, useState } from 'react';
import { questioningPlanningEntries, questioningStageLabels, type QuestioningPurpose, type QuestioningStage, type QuestioningSubject } from './questioning-planning-data';
import './questioning-planning.css';

const subjects: Array<'All subjects' | QuestioningSubject> = ['All subjects', 'English', 'Mathematics', 'Science', 'History and histories', 'Languages', 'Computing and IT'];
const purposes: Array<'All purposes' | QuestioningPurpose> = ['All purposes', 'Activate prior knowledge', 'Elicit reasoning', 'Surface a misconception', 'Support transfer', 'Prompt reflection'];

export function QuestioningPlanningPanel() {
  const [subject, setSubject] = useState<(typeof subjects)[number]>('All subjects');
  const [purpose, setPurpose] = useState<(typeof purposes)[number]>('All purposes');
  const [stage, setStage] = useState<'all' | QuestioningStage>('all');
  const [query, setQuery] = useState('');

  const entries = useMemo(() => {
    const needle = query.trim().toLocaleLowerCase();
    return questioningPlanningEntries.filter((entry) => {
      if (subject !== 'All subjects' && entry.subject !== subject) return false;
      if (purpose !== 'All purposes' && entry.purpose !== purpose) return false;
      if (stage !== 'all' && !entry.stages.includes(stage)) return false;
      return !needle || [entry.subject, entry.stageLabel, entry.purpose, entry.prompt, entry.followUp, entry.evidenceToNotice, entry.caution]
        .some((value) => value.toLocaleLowerCase().includes(needle));
    });
  }, [purpose, query, stage, subject]);

  return (
    <section className="questioning-planning" id="workspace-questioning-planning" aria-labelledby="questioning-planning-title">
      <header className="questioning-planning__header">
        <div>
          <p className="questioning-planning__eyebrow">Teaching memory / read only</p>
          <h2 id="questioning-planning-title">Questioning that gathers evidence</h2>
          <p>Plan the next useful question, the evidence worth noticing, and the caution that prevents a confident guess becoming a learner judgement.</p>
        </div>
        <span className="questioning-planning__count" aria-live="polite">{entries.length} of {questioningPlanningEntries.length}</span>
      </header>
      <div className="questioning-planning__boundary" role="note">Planning prompts only. No response is scored, stored, diagnosed, or used to classify a learner.</div>
      <div className="questioning-planning__filters" aria-label="Questioning bank filters">
        <label>Subject<select value={subject} onChange={(event) => setSubject(event.target.value as (typeof subjects)[number])}>{subjects.map((value) => <option key={value}>{value}</option>)}</select></label>
        <label>Purpose<select value={purpose} onChange={(event) => setPurpose(event.target.value as (typeof purposes)[number])}>{purposes.map((value) => <option key={value}>{value}</option>)}</select></label>
        <label>Stage lens<select value={stage} onChange={(event) => setStage(event.target.value as 'all' | QuestioningStage)}><option value="all">All stage lenses</option>{Object.entries(questioningStageLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        <label>Search<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="reasoning, source, state..." /></label>
      </div>
      {entries.length === 0 ? <p className="questioning-planning__empty">No prompts match those planning filters. Widen the search rather than inventing a learner conclusion.</p> : (
        <div className="questioning-planning__grid">{entries.map((entry) => (
          <article className="questioning-card" key={entry.id}>
            <div className="questioning-card__meta"><span>{entry.subject}</span><span>{entry.stageLabel}</span><strong>{entry.purpose}</strong></div>
            <h3>{entry.prompt}</h3>
            <dl><div><dt>Follow-up</dt><dd>{entry.followUp}</dd></div><div><dt>Evidence to notice</dt><dd>{entry.evidenceToNotice}</dd></div><div className="questioning-card__caution"><dt>Caution</dt><dd>{entry.caution}</dd></div></dl>
          </article>
        ))}</div>
      )}
    </section>
  );
}
