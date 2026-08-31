import { useMemo, useState } from 'react';
import { resourceRightsClasses, type ResourceRightsClassId } from './resource-rights';
import './resource-rights.css';

type RightsFilter = 'all' | ResourceRightsClassId;

export function ResourceRightsPanel() {
  const [filter, setFilter] = useState<RightsFilter>('all');
  const visible = useMemo(() => filter === 'all' ? resourceRightsClasses : resourceRightsClasses.filter((item) => item.id === filter), [filter]);

  return <section id="workspace-rights" className="resource-rights-shell" aria-labelledby="resource-rights-title">
    <header>
      <div>
        <p>PROVENANCE BEFORE COPYING · NOT LEGAL ADVICE</p>
        <h2 id="resource-rights-title">Know what authority travels with a resource.</h2>
        <span>Public access, educational value, and download success do not grant reuse rights.</span>
      </div>
      <label>Resource class<select value={filter} onChange={(event) => setFilter(event.target.value as RightsFilter)}><option value="all">All provenance classes</option>{resourceRightsClasses.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
    </header>

    <p className="resource-rights-count" role="status">Showing {visible.length} of {resourceRightsClasses.length} resource classes.</p>

    <div className="resource-rights-grid">
      {visible.map((item) => <article key={item.id}>
        <div className="resource-rights-heading"><div><p>{item.id.toUpperCase()}</p><h3>{item.label}</h3></div><code>{item.id}</code></div>
        <blockquote>{item.defaultAction}</blockquote>
        <section><h4>Evidence needed</h4><ul>{item.evidenceNeeded.map((entry) => <li key={entry}>{entry}</li>)}</ul></section>
        <div className="resource-rights-decisions">
          <section><h4>Permitted within evidence</h4><ul>{item.permitted.map((entry) => <li key={entry}>{entry}</li>)}</ul></section>
          <section><h4>Do not do</h4><ul>{item.prohibited.map((entry) => <li key={entry}>{entry}</li>)}</ul></section>
        </div>
      </article>)}
    </div>
  </section>;
}
