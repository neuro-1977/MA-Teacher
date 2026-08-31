import { useMemo, useState } from 'react';
import { workedExamples, type WorkedExampleId } from './worked-examples';
import './worked-examples.css';

type ExampleFilter = 'all' | WorkedExampleId;
type StageFilter = 'all' | typeof workedExamples[number]['stage'];

export function WorkedExamplesPanel() {
  const [filter, setFilter] = useState<ExampleFilter>('all');
  const [stageFilter, setStageFilter] = useState<StageFilter>('all');
  const stages = useMemo(() => Array.from(new Map(workedExamples.map(example => [example.stage, example.stageLabel])).entries()), []);
  const visible = useMemo(() => workedExamples.filter(example => (filter === 'all' || example.id === filter) && (stageFilter === 'all' || example.stage === stageFilter)), [filter, stageFilter]);

  return <section id="workspace-worked-examples" className="worked-examples-shell" aria-labelledby="worked-examples-title">
    <header>
      <div>
        <p>SYNTHETIC DATA · NOTHING SAVED</p>
        <h2 id="worked-examples-title">See the human evidence loop end to end.</h2>
        <span>These original examples demonstrate structure. Replace every source boundary with reviewed real curriculum evidence before use.</span>
      </div>
      <div className="worked-example-filters"><label>Example<select value={filter} onChange={(event) => setFilter(event.target.value as ExampleFilter)}><option value="all">All twelve examples</option>{workedExamples.map((example) => <option key={example.id} value={example.id}>{example.subject} · {example.stageLabel}</option>)}</select></label><label>Stage lens<select value={stageFilter} onChange={(event) => setStageFilter(event.target.value as StageFilter)}><option value="all">All stage lenses</option>{stages.map(([id, label]) => <option key={id} value={id}>{label}</option>)}</select></label></div>
    </header>

    <p className="worked-examples-count" role="status">Showing {visible.length} synthetic {visible.length === 1 ? 'example' : 'examples'}.</p>

    <div className="worked-examples-grid">
      {visible.map((example) => <article key={example.id}>
        <div className="worked-example-heading"><div><p>{example.subject.toUpperCase()} · {example.stageLabel.toUpperCase()}</p><h3>{example.title}</h3></div><code>{example.id}</code></div>
        <p className="worked-example-stage-boundary">Stage is a planning lens only. It is not curriculum acceptance, age suitability, accessibility approval, or evidence of effectiveness.</p>
        <aside className="worked-example-source"><b>Source boundary</b><span>{example.sourceBoundary}</span></aside>
        <section><h4>Learning intention</h4><p>{example.learningIntention}</p></section>
        <section><h4>Model</h4><p>{example.model}</p></section>
        <section><h4>Manual check</h4><p>{example.checkPrompt}</p><ul>{example.successCriteria.map((criterion) => <li key={criterion}>{criterion}</li>)}</ul></section>
        <section className="worked-example-attempt"><h4>Synthetic learner attempt</h4><p>{example.sampleAttempt}</p></section>
        <section className="worked-example-review"><h4>Bounded human review</h4><p>{example.humanReview}</p></section>
        <footer><b>Next evidence</b><span>{example.nextEvidence}</span></footer>
      </article>)}
    </div>
  </section>;
}
