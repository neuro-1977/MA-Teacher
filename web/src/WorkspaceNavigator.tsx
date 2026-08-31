import { focusWorkspaceSurface, preferredWorkspaceScrollBehavior } from './workspace-navigation';
import { workspaceEffectLabels, workspaceGroups } from './workspace-surfaces';
import './workspace-navigator.css';

export function WorkspaceNavigator() {
  function navigate(id: string, button?: HTMLButtonElement) {
    focusWorkspaceSurface(id);
    button?.closest('details')?.removeAttribute('open');
  }

  return <nav className="workspace-navigator" aria-label="MA-Teacher workspace sections">
    <button type="button" className="workspace-home" onClick={() => window.scrollTo({ top: 0, behavior: preferredWorkspaceScrollBehavior() })}><strong>MA-TEACHER</strong><span>LOCAL EVIDENCE WORKSPACE</span></button>
    <button type="button" className="workspace-navigator__index" onClick={(event) => navigate('workspace-index', event.currentTarget)}>Workspace index</button>
    <div className="workspace-navigator__groups">
      {workspaceGroups.map((group) => <details key={group.id}>
        <summary><span>{group.label}</span><small>{group.surfaces.length}</small></summary>
        <div className="workspace-navigator__menu" aria-label={`${group.label} destinations`}>
          <header><strong>{group.label}</strong><span>{group.purpose}</span></header>
          {group.surfaces.map((surface) => <button type="button" key={surface.id} onClick={(event) => navigate(surface.id, event.currentTarget)}>
            <span><b>{surface.label}</b><small>{surface.description}</small></span>
            <em data-effect={surface.effect}>{workspaceEffectLabels[surface.effect]}</em>
          </button>)}
        </div>
      </details>)}
    </div>
  </nav>;
}
