import './safety-privacy.css';

const boundaries = [
  {
    eyebrow: 'LOCAL DATA',
    title: 'Keep learner records minimal.',
    body: 'Use the smallest useful display name and teaching context. Do not store addresses, health records, safeguarding disclosures, credentials, or school-management data in a learner profile.',
  },
  {
    eyebrow: 'HUMAN JUDGEMENT',
    title: 'A recorded response is not a learner label.',
    body: 'Attempts remain learner-owned evidence. Only a human records a bounded review for that attempt. MA-Teacher does not infer intelligence, disability, effort, emotion, ability, rank, grade, or mastery.',
  },
  {
    eyebrow: 'CURRICULUM EVIDENCE',
    title: 'Review before teaching.',
    body: 'Captured text is not automatically curriculum truth. Check its exact source, version, subject, stage, and context before accepting it or linking it to a lesson.',
  },
  {
    eyebrow: 'BACKUPS',
    title: 'A backup contains the same sensitive records.',
    body: 'Manual database snapshots stay inside the install root. Protect them like the live database. Backup creation and verification exist in source; restore behavior is not implemented or proven.',
  },
  {
    eyebrow: 'SAFEGUARDING',
    title: 'The application is not an emergency or reporting channel.',
    body: 'Do not ask learners to place urgent, harmful, or identifying disclosures into lesson answers. Follow the responsible adult, school, service, or emergency process that applies outside this application.',
  },
  {
    eyebrow: 'ACCESSIBILITY',
    title: 'Adapt presentation without diagnosing the learner.',
    body: 'Use clear language, manageable steps, and appropriate examples. Keyboard, screen-reader, contrast, zoom, reading-demand, motion, and error-recovery acceptance remain unverified.',
  },
];

export function SafetyPrivacyPanel() {
  return <section id="workspace-safety" className="safety-shell" aria-labelledby="safety-title">
    <header className="safety-hero">
      <div>
        <p>BEFORE YOU TEACH · OPERATOR REVIEW REQUIRED</p>
        <h2 id="safety-title">Local-first does not mean consequence-free.</h2>
        <span>Read these boundaries before entering learner information or publishing a lesson.</span>
      </div>
      <strong>NOT ACCEPTANCE PROOF</strong>
    </header>

    <div className="safety-grid">
      {boundaries.map((boundary) => <article key={boundary.eyebrow}>
        <p>{boundary.eyebrow}</p>
        <h3>{boundary.title}</h3>
        <span>{boundary.body}</span>
      </article>)}
    </div>

    <footer className="safety-footer">
      <div><b>Safe default</b><span>Stop and ask a responsible human when evidence, age suitability, accessibility, or safeguarding is uncertain.</span></div>
      <div><b>Data boundary</b><span>Single install-root SQLite storage. No cloud sync, hidden profile, or automatic learner scoring.</span></div>
    </footer>
  </section>;
}
