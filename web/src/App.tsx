import tutorIcon from '../../icon-large.png';

const subjects = ['Science', 'English', 'Maths', 'History', 'Languages', 'Information technology'];
const boundaries = [
  ['Desktop shell', 'Packaged now'],
  ['Curriculum research', 'Evidence required'],
  ['Learner records', 'Not implemented'],
  ['Assessment and tutoring', 'Not implemented'],
];

export function App() {
  return (
    <main className="shell">
      <header className="masthead">
        <div><span className="eyebrow">MOSTLY ARMLESS / EDUCATION LAB</span><h1>MA-Teacher <em>0.1.0</em></h1></div>
        <span className="status">INSTALLABLE FOUNDATION</span>
      </header>
      <section className="hero" aria-labelledby="purpose-heading">
        <img className="tutor-icon" src={tutorIcon} alt="MA-Teacher potato-shaped tutor wearing a graduation cap" />
        <div><p className="eyebrow">ALL AGES / EVIDENCE-FIRST LEARNING</p>
        <h2 id="purpose-heading">A warm learning companion with the honesty to check its homework.</h2>
        <p>The desktop foundation is real and installable. Curriculum research, lesson planning, learner records, assessment, and AI tutoring remain unimplemented until their sources, safeguarding, and proof contracts are agreed.</p></div>
      </section>
      <section className="subjects" aria-label="Planned subject breadth">
        {subjects.map((subject, index) => <span key={subject}><b>{String(index + 1).padStart(2, '0')}</b>{subject}</span>)}
      </section>
      <section className="decisions" aria-labelledby="decisions-heading">
        <div><p className="eyebrow">TRUTHFUL RELEASE BOUNDARY</p><h2 id="decisions-heading">What exists, and what still needs evidence</h2></div>
        <div className="decision-grid">{boundaries.map(([label, state], index) => <article key={label}><span>{String(index + 1).padStart(2, '0')}</span><strong>{label}</strong><small>{state}</small></article>)}</div>
      </section>
      <footer>PRIVATE DESKTOP FOUNDATION / LOCAL PORT 5201 / CAPTAINNEURO</footer>
    </main>
  );
}
