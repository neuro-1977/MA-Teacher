import { useState } from 'react';
import {
  learningErrorHypotheses,
  misconceptionInvestigationSequence,
} from './misconception-response-data';
import './misconception-response.css';

export function MisconceptionResponsePanel() {
  const [selectedId, setSelectedId] = useState(learningErrorHypotheses[0].id);
  const selected = learningErrorHypotheses.find((item) => item.id === selectedId) ?? learningErrorHypotheses[0];

  return (
    <section id="workspace-misconception-response" className="misconception-response-panel" aria-labelledby="misconception-response-title">
      <header>
        <p className="misconception-response-kicker">Evidence before labels</p>
        <h2 id="misconception-response-title">Investigate a wrong answer before teaching against it</h2>
        <p>
          A wrong answer is an observation, not a diagnosis. Test competing explanations before deciding what instruction or access change is warranted.
        </p>
      </header>

      <ol className="misconception-sequence" aria-label="Investigation sequence">
        {misconceptionInvestigationSequence.map((step) => <li key={step}>{step}</li>)}
      </ol>

      <div className="misconception-selector">
        <label htmlFor="misconception-hypothesis">Explore a possible explanation</label>
        <select
          id="misconception-hypothesis"
          value={selectedId}
          onChange={(event) => setSelectedId(event.target.value)}
        >
          {learningErrorHypotheses.map((item) => (
            <option key={item.id} value={item.id}>{item.label}</option>
          ))}
        </select>
      </div>

      <article className="misconception-hypothesis" aria-live="polite">
        <h3>{selected.label}</h3>
        <p>{selected.description}</p>
        <div className="misconception-columns">
          <section>
            <h4>Gather distinguishing evidence</h4>
            <ul>{selected.evidenceQuestions.map((item) => <li key={item}>{item}</li>)}</ul>
          </section>
          <section>
            <h4>Possible bounded responses</h4>
            <ul>{selected.usefulResponses.map((item) => <li key={item}>{item}</li>)}</ul>
          </section>
          <section>
            <h4>Evidence against this hypothesis</h4>
            <ul>{selected.disconfirmingEvidence.map((item) => <li key={item}>{item}</li>)}</ul>
          </section>
        </div>
      </article>

      <p className="misconception-boundary" role="note">
        <strong>No learner classification:</strong> this reference surface stores nothing, scores nothing, and cannot establish ability, diagnosis, motivation, or a permanent misconception.
      </p>
    </section>
  );
}
