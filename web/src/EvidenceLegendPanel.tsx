import { evidenceStates } from './evidence-status';
import './evidence-status.css';

export function EvidenceLegendPanel() {
  return <section id="workspace-evidence" className="evidence-legend" aria-labelledby="evidence-legend-title">
    <header>
      <div>
        <p>CANONICAL BREADCRUMB STATES</p>
        <h2 id="evidence-legend-title">Development receipt evidence language.</h2>
        <span>These selectable receipt states share one React source. Read each against the exact source, artifact, action, environment, and date it describes.</span>
      </div>
      <strong>NO IMPLIED COMPLETION</strong>
    </header>

    <div className="evidence-state-grid">
      {evidenceStates.map((state) => <article key={state.id} className={`evidence-state is-${state.tone}`}>
        <b>{state.label}</b>
        <span>{state.meaning}</span>
        <code>{state.id}</code>
      </article>)}
    </div>

    <footer>
      <b>Promotion rule</b>
      <span>A receipt state changes only through a new immutable record with stronger direct evidence. Readiness gates and domain records may use additional bounded vocabularies; wording, confidence, repetition, or elapsed time cannot promote any state.</span>
    </footer>
  </section>;
}
