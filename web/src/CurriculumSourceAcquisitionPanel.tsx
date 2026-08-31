import { useMemo, useState } from 'react';
import {
  curriculumSourceReferences,
  type CurriculumSourceJurisdiction,
} from './curriculum-source-catalogue';
import './curriculum-source-acquisition.css';

type JurisdictionFilter = 'All' | CurriculumSourceJurisdiction;

const jurisdictionFilters: readonly JurisdictionFilter[] = [
  'All',
  'England',
  'Scotland',
  'Wales',
  'Northern Ireland',
  'Cross-UK reference',
];

export function CurriculumSourceAcquisitionPanel() {
  const [jurisdiction, setJurisdiction] = useState<JurisdictionFilter>('All');
  const [query, setQuery] = useState('');

  const visibleSources = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase();

    return curriculumSourceReferences.filter((source) => {
      if (jurisdiction !== 'All' && source.jurisdiction !== jurisdiction) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return [
        source.title,
        source.authority,
        source.jurisdiction,
        source.sourceClass,
        source.stage,
        source.scope,
        source.caution,
      ].some((value) => value.toLocaleLowerCase().includes(normalizedQuery));
    });
  }, [jurisdiction, query]);

  return (
    <section id="workspace-source-acquisition" className="source-acquisition-panel" aria-labelledby="source-acquisition-title">
      <header className="source-acquisition-header">
        <div>
          <p className="source-acquisition-kicker">Evidence intake map</p>
          <h2 id="source-acquisition-title">Official curriculum and teaching-source guide</h2>
          <p>
            Find the right authority before capturing evidence. These links are reference routes only; every item remains not imported until the database records and reviews an exact version.
          </p>
        </div>
        <div className="source-acquisition-state" aria-label="Catalogue evidence state">
          <strong>{visibleSources.length}</strong>
          <span>reference routes shown</span>
          <span>0 imported by this guide</span>
        </div>
      </header>

      <div className="source-acquisition-controls">
        <label>
          Search source routes
          <input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Subject, authority, stage, or source class"
          />
        </label>
        <div className="source-jurisdiction-filters" role="group" aria-label="Filter by jurisdiction">
          {jurisdictionFilters.map((filter) => (
            <button
              key={filter}
              type="button"
              aria-pressed={jurisdiction === filter}
              className={jurisdiction === filter ? 'is-active' : ''}
              onClick={() => setJurisdiction(filter)}
            >
              {filter}
            </button>
          ))}
        </div>
      </div>

      <div className="source-acquisition-boundary" role="note">
        <strong>Database acceptance boundary:</strong> opening a link does not retrieve, import, parse, approve, or attach evidence to a lesson. Temporary downloads must be captured into the versioned database path and then removed.
      </div>

      {visibleSources.length > 0 ? (
        <div className="source-acquisition-grid">
          {visibleSources.map((source) => (
            <article key={source.id} className="source-acquisition-card">
              <div className="source-acquisition-card-topline">
                <span>{source.jurisdiction}</span>
                <span>{source.sourceClass}</span>
                <strong>{source.importState.replace('_', ' ')}</strong>
              </div>
              <h3>{source.title}</h3>
              <dl>
                <div><dt>Authority</dt><dd>{source.authority}</dd></div>
                <div><dt>Stage</dt><dd>{source.stage}</dd></div>
                <div><dt>Scope</dt><dd>{source.scope}</dd></div>
                <div><dt>Route observed</dt><dd>{source.observedUtc}</dd></div>
              </dl>
              <p className="source-acquisition-caution"><strong>Keep explicit:</strong> {source.caution}</p>
              <a href={source.url} target="_blank" rel="noreferrer">Open official route</a>
            </article>
          ))}
        </div>
      ) : (
        <p className="source-acquisition-empty" role="status">
          No reference route matches this filter. Clear the search or choose another jurisdiction; do not infer a missing source.
        </p>
      )}
    </section>
  );
}
