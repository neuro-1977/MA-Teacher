import './accessibility.css';

const destinations = [
  ['workspace-start', 'Skip to start guidance'],
  ['workspace-session-brief', 'Skip to teaching-session brief'],
  ['workspace-learning', 'Skip to learner workspace'],
  ['workspace-lesson-reader', 'Skip to current lesson'],
  ['workspace-learning-checks', 'Skip to practice checks'],
] as const;

export function SkipLinks() {
  return <nav className="skip-links" aria-label="Skip navigation">
    {destinations.map(([id, label]) => <a key={id} href={`#${id}`}>{label}</a>)}
  </nav>;
}
