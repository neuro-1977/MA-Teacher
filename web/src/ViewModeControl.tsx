import { useEffect, useState } from 'react';
import './view-mode.css';

type ViewMode = 'teacher' | 'planning' | 'lesson';

export function ViewModeControl() {
  const [mode, setMode] = useState<ViewMode>('teacher');

  useEffect(() => {
    document.documentElement.dataset.maTeacherView = mode;

    if (mode === 'planning') {
      document.getElementById('workspace-session-brief')?.scrollIntoView({ block: 'start' });
    } else if (mode === 'lesson') {
      document.getElementById('workspace-lesson-reader')?.scrollIntoView({ block: 'start' });
    }

    return () => {
      delete document.documentElement.dataset.maTeacherView;
    };
  }, [mode]);

  const selectMode = (nextMode: ViewMode) => {
    setMode(nextMode);
  };

  return (
    <section id="workspace-view-mode" className="view-mode-control" aria-labelledby="view-mode-title">
      <div>
        <p className="view-mode-kicker">Workspace view</p>
        <h2 id="view-mode-title">Choose what needs attention</h2>
        <p>
          Teacher workspace shows every surface, Planning focus keeps teaching preparation in view, and Lesson focus keeps lesson and practice surfaces in view. Each mode changes presentation only; none changes, deletes, or authorizes data.
        </p>
      </div>
      <div className="view-mode-actions" role="group" aria-label="Workspace view">
        <button
          type="button"
          className={mode === 'teacher' ? 'is-active' : ''}
          aria-pressed={mode === 'teacher'}
          onClick={() => selectMode('teacher')}
        >
          Teacher workspace
        </button>
        <button
          type="button"
          className={mode === 'planning' ? 'is-active' : ''}
          aria-pressed={mode === 'planning'}
          onClick={() => selectMode('planning')}
        >
          Planning focus
        </button>
        <button
          type="button"
          className={mode === 'lesson' ? 'is-active' : ''}
          aria-pressed={mode === 'lesson'}
          onClick={() => selectMode('lesson')}
        >
          Lesson focus
        </button>
      </div>
      {mode !== 'teacher' ? (
        <p className="view-mode-warning" role="status">
          {mode === 'planning' ? 'Planning focus hides operational and development surfaces while keeping non-canonical teaching preparation references.' : 'Lesson focus keeps the current lesson and practice surfaces visible.'} Focus view is presentation only, not a learner login, role, permission, or security boundary. Use Teacher workspace to restore every surface.
        </p>
      ) : null}
    </section>
  );
}
