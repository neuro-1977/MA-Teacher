import { useMemo, useState } from 'react';
import { workspaceEffectLabels, workspaceGroups, type WorkspaceEffect } from './workspace-surfaces';
import './workspace-index.css';

type EffectFilter = 'all-effects' | WorkspaceEffect;

export function WorkspaceIndexPanel() {
  const [query, setQuery] = useState('');
  const [effect, setEffect] = useState<EffectFilter>('all-effects');
  const visibleGroups = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    return workspaceGroups.map((group) => {
      const groupMatch = `${group.label} ${group.purpose}`.toLowerCase().includes(normalized);
      const effectMatches = group.surfaces.filter((surface) => effect === 'all-effects' || surface.effect === effect);
      const surfaces = !normalized || groupMatch ? effectMatches : effectMatches.filter((surface) => `${surface.label} ${surface.description} ${workspaceEffectLabels[surface.effect]}`.toLowerCase().includes(normalized));
      return { ...group, surfaces };
    }).filter((group) => group.surfaces.length > 0);
  }, [effect, query]);
  const totalSurfaces = workspaceGroups.reduce((total, group) => total + group.surfaces.length, 0);
  const visibleSurfaces = visibleGroups.reduce((total, group) => total + group.surfaces.length, 0);

  return <section id="workspace-index" className="workspace-index-shell" aria-labelledby="workspace-index-title">
    <header>
      <div>
        <p>WORKSPACE MAP · SIDE EFFECTS FIRST</p>
        <h2 id="workspace-index-title">Know what a surface owns before opening it.</h2>
        <span>Navigation does not perform the listed action. Writes still require an explicit control inside the destination.</span>
      </div>
      <div className="workspace-index-search"><label htmlFor="workspace-index-query">Find a surface<input id="workspace-index-query" type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="lesson, backup, read only..." autoComplete="off" /></label><label htmlFor="workspace-index-effect">Side effect<select id="workspace-index-effect" value={effect} onChange={(event) => setEffect(event.target.value as EffectFilter)}><option value="all-effects">All effects</option>{Object.entries(workspaceEffectLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><strong>{visibleSurfaces} / {totalSurfaces} SURFACES</strong></div>
    </header>

    <p className="workspace-index-result" role="status">{query.trim() || effect !== 'all-effects' ? `${visibleSurfaces} matching ${visibleSurfaces === 1 ? 'surface' : 'surfaces'}${effect === 'all-effects' ? '' : ` declared ${workspaceEffectLabels[effect].toLowerCase()}`}.` : 'All workspace surfaces are shown.'}</p>

    {visibleGroups.length === 0 ? <div className="workspace-index-empty">No surface matches this local filter. Clear or change the search; no application state was changed.</div> : null}

    <div className="workspace-index-groups">
      {visibleGroups.map((group) => <article key={group.id}>
        <div className="workspace-index-group-heading"><div><p>{group.id.toUpperCase()}</p><h3>{group.label}</h3></div><span>{group.purpose}</span></div>
        <div className="workspace-index-links">
          {group.surfaces.map((surface) => <a key={surface.id} href={`#${surface.id}`} className={`effect-${surface.effect}`}>
            <div><b>{surface.label}</b><span>{surface.description}</span></div>
            <em>{workspaceEffectLabels[surface.effect]}</em>
          </a>)}
        </div>
      </article>)}
    </div>
  </section>;
}
