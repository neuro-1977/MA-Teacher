import { useMemo, useState } from 'react';
import { curriculumReviewPhases, type CurriculumReviewPhaseId } from './curriculum-review-criteria';
import './curriculum-review-guide.css';

type PhaseFilter = 'all' | CurriculumReviewPhaseId;

export function CurriculumReviewGuidePanel() {
  const [filter, setFilter] = useState<PhaseFilter>('all');
  const visible = useMemo(() => filter === 'all' ? curriculumReviewPhases : curriculumReviewPhases.filter((phase) => phase.id === filter), [filter]);

  return <section id="workspace-curriculum-review" className="curriculum-review-shell" aria-labelledby="curriculum-review-title">
    <header>
      <div>
        <p>PROVENANCE BEFORE PROMOTION</p>
        <h2 id="curriculum-review-title">Review every curriculum evidence transition.</h2>
        <span>A successful download or parser result is not an accepted curriculum statement.</span>
      </div>
      <label>Evidence phase<select value={filter} onChange={(event) => setFilter(event.target.value as PhaseFilter)}><option value="all">All transitions</option>{curriculumReviewPhases.map((phase) => <option key={phase.id} value={phase.id}>{phase.label}</option>)}</select></label>
    </header>

    <p className="curriculum-review-count" role="status">Showing {visible.length} of {curriculumReviewPhases.length} evidence transitions.</p>

    <div className="curriculum-review-phases">
      {visible.map((phase, index) => <article key={phase.id}>
        <div className="curriculum-review-heading"><div><p>STEP {curriculumReviewPhases.indexOf(phase) + 1}</p><h3>{phase.label}</h3></div><code>{phase.id}</code></div>
        <b className="curriculum-review-purpose">{phase.purpose}</b>
        <section><h4>Inspect directly</h4><ul>{phase.inspect.map((item) => <li key={item}>{item}</li>)}</ul></section>
        <div className="curriculum-review-decisions">
          <p><b>Progress only when</b><span>{phase.progressWhen}</span></p>
          <p><b>Refuse when</b><span>{phase.refuseWhen}</span></p>
        </div>
        {index < visible.length - 1 ? <div className="curriculum-review-next" aria-hidden="true">NEXT EVIDENCE GATE</div> : null}
      </article>)}
    </div>
  </section>;
}
