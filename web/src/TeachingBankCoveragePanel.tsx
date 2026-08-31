import { useMemo, useState } from 'react';
import { feedbackPlanningEntries } from './feedback-planning-data';
import { questioningPlanningEntries } from './questioning-planning-data';
import { stageLenses } from './stage-lenses';
import { subjectLenses } from './subject-lenses';
import { vocabularyEntries } from './vocabulary-planning-data';
import { workedExamples } from './worked-examples';
import './teaching-bank-coverage.css';

type CoverageState = 'Four banks present' | 'Partial source coverage' | 'No source entries';
type CoverageFilter = 'All cells' | 'Show gaps' | 'Empty only';

type CoverageCell = {
  id: string;
  subject: string;
  stage: string;
  stageSupport: string;
  vocabulary: number;
  questioning: number;
  feedback: number;
  examples: number;
  bankCount: number;
  state: CoverageState;
};

export function TeachingBankCoveragePanel() {
  const [filter, setFilter] = useState<CoverageFilter>('Show gaps');
  const cells = useMemo<CoverageCell[]>(() => subjectLenses.flatMap((subject) => stageLenses.map((stage) => {
    const vocabulary = vocabularyEntries.filter((item) => item.subject === subject.label && item.stages.some((value) => value === stage.id)).length;
    const questioning = questioningPlanningEntries.filter((item) => item.subject === subject.label && item.stages.some((value) => value === stage.id)).length;
    const feedback = feedbackPlanningEntries.filter((item) => item.subject === subject.label && item.stages.some((value) => value === stage.id)).length;
    const examples = workedExamples.filter((item) => item.subject === subject.label && item.stage === stage.id).length;
    const bankCount = [vocabulary, questioning, feedback, examples].filter((count) => count > 0).length;
    return {
      id: `${subject.id}-${stage.id}`,
      subject: subject.label,
      stage: stage.label,
      stageSupport: stage.support,
      vocabulary,
      questioning,
      feedback,
      examples,
      bankCount,
      state: bankCount === 4 ? 'Four banks present' : bankCount === 0 ? 'No source entries' : 'Partial source coverage',
    };
  })), []);
  const visible = cells.filter((cell) => filter === 'All cells' || (filter === 'Show gaps' && cell.bankCount < 4) || (filter === 'Empty only' && cell.bankCount === 0));
  const completeCount = cells.filter((cell) => cell.bankCount === 4).length;
  const partialCount = cells.filter((cell) => cell.bankCount > 0 && cell.bankCount < 4).length;
  const emptyCount = cells.filter((cell) => cell.bankCount === 0).length;

  return (
    <section className="bank-coverage" id="workspace-bank-coverage" aria-labelledby="bank-coverage-title">
      <header>
        <div><p>Source-array diagnostic</p><h2 id="bank-coverage-title">Teaching-bank coverage debt</h2><span>See which subject and stage combinations have source entries in each planning bank before authoring more material.</span></div>
        <strong>{cells.length} COMBINATIONS</strong>
      </header>
      <aside className="bank-coverage__boundary" role="note"><b>Presence is not quality.</b> One source entry does not establish completeness, suitability, curriculum alignment, accessibility, effectiveness, or human acceptance. Counts are computed from current arrays; provenance identity, currency, rights, review, and acceptance remain separate evidence.</aside>
      <div className="bank-coverage__summary" aria-label="Teaching bank coverage summary">
        <span><b>{completeCount}</b> four banks present</span><span><b>{partialCount}</b> partial</span><span><b>{emptyCount}</b> empty</span>
        <label>View<select value={filter} onChange={(event) => setFilter(event.target.value as CoverageFilter)}><option>All cells</option><option>Show gaps</option><option>Empty only</option></select></label>
      </div>
      {visible.length === 0 ? <p className="bank-coverage__empty">No combinations match this view. This does not certify the teaching banks.</p> : <div className="bank-coverage__grid">{visible.map((cell) => <article key={cell.id} data-state={cell.state}>
        <div className="bank-coverage__heading"><div><p>{cell.subject}</p><h3>{cell.stage}</h3></div><strong>{cell.bankCount}/4</strong></div>
        <span className="bank-coverage__support">{cell.stageSupport}</span>
        <dl><div><dt>Vocabulary</dt><dd>{cell.vocabulary}</dd></div><div><dt>Questioning</dt><dd>{cell.questioning}</dd></div><div><dt>Feedback</dt><dd>{cell.feedback}</dd></div><div><dt>Worked examples</dt><dd>{cell.examples}</dd></div></dl>
        <footer>{cell.state}</footer>
      </article>)}</div>}
    </section>
  );
}
