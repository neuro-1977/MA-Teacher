import './print-lesson-control.css';

export function PrintLessonControl() {
  const printLesson = () => {
    const reader = document.getElementById('workspace-lesson-reader');
    if (!reader) return;
    reader.scrollIntoView({ block: 'start' });
    window.print();
  };

  return <aside className="print-lesson-control" aria-label="Lesson output controls">
    <div>
      <p>LESSON OUTPUT</p>
      <span>Review the loaded lesson, then print it or use the system PDF destination.</span>
    </div>
    <a href="#workspace-lesson-reader">Review lesson</a>
    <button type="button" onClick={printLesson}>Print current lesson</button>
  </aside>;
}
