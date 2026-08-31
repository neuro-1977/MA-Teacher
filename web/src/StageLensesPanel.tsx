import { useMemo, useState } from 'react';
import { stageLenses, type StageLensId } from './stage-lenses';
import './stage-lenses.css';

type StageFilter = 'all' | StageLensId;

export function StageLensesPanel() {
  const [filter, setFilter] = useState<StageFilter>('all');
  const visible = useMemo(() => filter === 'all' ? stageLenses : stageLenses.filter((stage) => stage.id === filter), [filter]);

  return <section id="workspace-stages" className="stage-lenses-shell" aria-labelledby="stage-lenses-title">
    <header>
      <div>
        <p>AGE-RESPECTFUL PLANNING · STAGE IS NOT ABILITY</p>
        <h2 id="stage-lenses-title">Adapt access without shrinking the learner.</h2>
        <span>Curriculum support status is explicit. Reference-only guidance must not be presented as configured coverage.</span>
      </div>
      <label>Stage view<select value={filter} onChange={(event) => setFilter(event.target.value as StageFilter)}><option value="all">All stages</option>{stageLenses.map((stage) => <option key={stage.id} value={stage.id}>{stage.label}</option>)}</select></label>
    </header>

    <p className="stage-lenses-count" role="status">Showing {visible.length} of {stageLenses.length} planning lenses.</p>

    <div className="stage-lenses-grid">
      {visible.map((stage) => <article key={stage.id}>
        <div className="stage-lens-heading"><div><p>{stage.id.toUpperCase()}</p><h3>{stage.label}</h3></div><strong className={`support-${stage.support}`}>{stage.support === 'reference-only' ? 'REFERENCE ONLY' : 'CONFIGURED · PARTIAL'}</strong></div>
        <blockquote>{stage.curriculumBoundary}</blockquote>
        <div className="stage-lens-columns">
          <section><h4>Planning focus</h4><ul>{stage.planningFocus.map((item) => <li key={item}>{item}</li>)}</ul></section>
          <section><h4>Response options</h4><ul>{stage.responseOptions.map((item) => <li key={item}>{item}</li>)}</ul></section>
        </div>
        <aside><h4>Dignity boundaries</h4><ul>{stage.dignityRules.map((item) => <li key={item}>{item}</li>)}</ul></aside>
      </article>)}
    </div>
  </section>;
}
