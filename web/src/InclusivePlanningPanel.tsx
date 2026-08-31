import { useMemo, useState } from 'react';
import {
  inclusivePlanningLenses,
  type InclusivePlanningCategory,
} from './inclusive-planning-data';
import './inclusive-planning.css';

type PlanningFilter = 'All' | InclusivePlanningCategory;

const filters: readonly PlanningFilter[] = [
  'All',
  'Language',
  'Reading',
  'Sensory access',
  'Attention and structure',
  'Response access',
  'Prior knowledge',
  'Challenge',
  'Safety',
];

export function InclusivePlanningPanel() {
  const [filter, setFilter] = useState<PlanningFilter>('All');

  const visibleLenses = useMemo(
    () => filter === 'All'
      ? inclusivePlanningLenses
      : inclusivePlanningLenses.filter((lens) => lens.category === filter),
    [filter],
  );

  return (
    <section id="workspace-inclusive-planning" className="inclusive-planning-panel" aria-labelledby="inclusive-planning-title">
      <header>
        <p className="inclusive-planning-kicker">Access planning, not diagnosis</p>
        <h2 id="inclusive-planning-title">Preserve the objective while changing the route</h2>
        <p>
          Use these prompts to investigate barriers with the learner and responsible humans. They do not identify a condition, prescribe support, or authorize collection of sensitive information.
        </p>
      </header>

      <div className="inclusive-planning-filters" role="group" aria-label="Filter planning lenses">
        {filters.map((item) => (
          <button
            key={item}
            type="button"
            aria-pressed={filter === item}
            className={filter === item ? 'is-active' : ''}
            onClick={() => setFilter(item)}
          >
            {item}
          </button>
        ))}
      </div>

      <div className="inclusive-planning-boundary" role="note">
        <strong>Human authority remains required.</strong> Ask, observe, trial, and review. Never infer a diagnosis, lower an objective silently, or store personal detail merely because it might be useful later.
      </div>

      <div className="inclusive-planning-grid">
        {visibleLenses.map((lens) => (
          <article key={lens.id} className="inclusive-planning-card">
            <p className="inclusive-planning-category">{lens.category}</p>
            <h3>{lens.title}</h3>
            <h4>Ask first</h4>
            <ul>{lens.askFirst.map((item) => <li key={item}>{item}</li>)}</ul>
            <h4>Planning moves</h4>
            <ul>{lens.planningMoves.map((item) => <li key={item}>{item}</li>)}</ul>
            <h4>Evidence worth collecting</h4>
            <ul>{lens.evidenceToCollect.map((item) => <li key={item}>{item}</li>)}</ul>
            <p className="inclusive-planning-never"><strong>Never infer:</strong> {lens.neverInfer}</p>
          </article>
        ))}
      </div>
    </section>
  );
}
