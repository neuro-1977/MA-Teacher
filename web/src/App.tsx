const decisions = [
  ['Product name', 'Open'],
  ['First key stage and subject', 'Open'],
  ['Student and teacher first workflow', 'Open'],
  ['Curriculum source and safeguarding model', 'Required before build'],
];

export function App() {
  return (
    <main className="shell">
      <header className="masthead">
        <div><span className="eyebrow">MOSTLY ARMLESS / PRIVATE MODULE</span><h1>MA-Teacher <em>working title</em></h1></div>
        <span className="status">ROADMAP ONLY</span>
      </header>
      <section className="hero" aria-labelledby="purpose-heading">
        <p className="eyebrow">ENGLISH NATIONAL CURRICULUM</p>
        <h2 id="purpose-heading">A student and teacher companion, before it becomes a product.</h2>
        <p>This skeleton preserves the direction: clear curriculum context for students and practical planning context for teachers. No accounts, learner records, curriculum import, or automated decisions exist here.</p>
      </section>
      <section className="roles" aria-label="Planned roles">
        <article><span>01</span><h3>Student view</h3><p>Learning goals, current topics, progress, and safe next actions.</p></article>
        <article><span>02</span><h3>Teacher view</h3><p>Curriculum intent, planning context, and future progress visibility.</p></article>
      </section>
      <section className="decisions" aria-labelledby="decisions-heading">
        <div><p className="eyebrow">NEXT GATES</p><h2 id="decisions-heading">Captain decisions before build</h2></div>
        <div className="decision-grid">{decisions.map(([label, state], index) => <article key={label}><span>{String(index + 1).padStart(2, '0')}</span><strong>{label}</strong><small>{state}</small></article>)}</div>
      </section>
      <footer>PRIVATE VITE SKELETON / 0 BUILD FEATURES / PORT 5201</footer>
    </main>
  );
}
