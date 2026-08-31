import { useState } from 'react';
import { workspaceGroups } from './workspace-surfaces';
import './workspace-registry-audit.css';

type RegistryAudit = {
  registeredCount: number;
  mountedWorkspaceCount: number;
  duplicateRegistryIds: string[];
  missingDestinations: string[];
  duplicateDomIds: string[];
  unregisteredWorkspaceIds: string[];
};

const infrastructureWorkspaceIds = new Set(['workspace-index']);

export function WorkspaceRegistryAuditPanel() {
  const [audit, setAudit] = useState<RegistryAudit | null>(null);
  const registeredIds = workspaceGroups.flatMap((group) => group.surfaces.map((surface) => surface.id));

  function runAudit() {
    const mountedIds = Array.from(document.querySelectorAll<HTMLElement>('[id^="workspace-"]')).map((element) => element.id);
    const registeredSet = new Set(registeredIds);
    setAudit({
      registeredCount: registeredIds.length,
      mountedWorkspaceCount: mountedIds.length,
      duplicateRegistryIds: duplicates(registeredIds),
      missingDestinations: [...registeredSet].filter((id) => document.querySelectorAll(`[id="${id}"]`).length === 0),
      duplicateDomIds: duplicates(mountedIds),
      unregisteredWorkspaceIds: [...new Set(mountedIds)].filter((id) => !registeredSet.has(id) && !infrastructureWorkspaceIds.has(id)),
    });
  }

  const issueCount = audit ? audit.duplicateRegistryIds.length + audit.missingDestinations.length + audit.duplicateDomIds.length + audit.unregisteredWorkspaceIds.length : null;
  return <section className="registry-audit" id="workspace-registry-audit" aria-labelledby="registry-audit-title">
    <header><div><p>Explicit local diagnostic</p><h2 id="registry-audit-title">Workspace registry audit</h2><span>Compare the mounted page with the same registry used by grouped navigation and the searchable index.</span></div><strong data-state={issueCount === 0 ? 'clean' : issueCount === null ? 'not-run' : 'issues'}>{issueCount === null ? 'NOT RUN' : issueCount === 0 ? 'STRUCTURE MATCHED' : `${issueCount} ISSUE${issueCount === 1 ? '' : 'S'}`}</strong></header>
    <aside className="registry-audit__boundary" role="note"><b>Structural evidence only.</b> A matched ID proves neither visibility nor correct rendering, focus, accessibility, behavior, side effects, persistence, security, or human acceptance.</aside>
    <button type="button" onClick={runAudit}>Run mounted registry audit</button>
    {!audit ? <p className="registry-audit__empty">No audit has run in this browser mount. Nothing is polled, persisted, copied, submitted, or inferred.</p> : <div className="registry-audit__result" aria-live="polite">
      <div className="registry-audit__counts"><span><b>{audit.registeredCount}</b> registered surfaces</span><span><b>{audit.mountedWorkspaceCount}</b> mounted workspace IDs</span></div>
      <AuditList title="Duplicate registry IDs" values={audit.duplicateRegistryIds} empty="No duplicate registry IDs detected." />
      <AuditList title="Registered destinations missing from DOM" values={audit.missingDestinations} empty="Every registered ID had a mounted destination." />
      <AuditList title="Duplicate mounted DOM IDs" values={audit.duplicateDomIds} empty="No duplicate mounted workspace IDs detected." />
      <AuditList title="Mounted but unregistered workspace IDs" values={audit.unregisteredWorkspaceIds} empty="No unexpected mounted workspace IDs detected." />
    </div>}
  </section>;
}

function AuditList({ title, values, empty }: { title: string; values: string[]; empty: string }) {
  return <article data-empty={values.length === 0}><h3>{title}</h3>{values.length === 0 ? <p>{empty}</p> : <ul>{values.map((value) => <li key={value}><code>{value}</code></li>)}</ul>}</article>;
}

function duplicates(values: string[]) {
  const counts = new Map<string, number>();
  for (const value of values) counts.set(value, (counts.get(value) ?? 0) + 1);
  return [...counts.entries()].filter(([, count]) => count > 1).map(([value]) => value).sort();
}
