export function preferredWorkspaceScrollBehavior(): ScrollBehavior {
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth';
}

export function focusWorkspaceSurface(id: string): boolean {
  const target = document.getElementById(id);
  if (!target) {
    window.dispatchEvent(new CustomEvent('ma-teacher:navigate', { detail: { id } }));
    return true;
  }
  if (!target.hasAttribute('tabindex')) target.tabIndex = -1;
  target.focus({ preventScroll: true });
  target.scrollIntoView({ behavior: preferredWorkspaceScrollBehavior(), block: 'start' });
  return true;
}
