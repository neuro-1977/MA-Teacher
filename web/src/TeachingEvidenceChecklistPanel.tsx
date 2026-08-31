import './teaching-evidence-checklist.css';

const checks = [
  {
    id: 'jurisdiction', title: 'Jurisdiction is explicit',
    question: 'Which curriculum jurisdiction, qualification or non-curriculum purpose applies?',
    evidence: 'A governed source or an explicit statement that the material is original planning data without curriculum authority.',
    stop: 'Stop if a familiar stage label is being used as proof of another jurisdiction\'s framework.',
  },
  {
    id: 'reference-authority', title: 'Reference authority is current and bounded',
    question: 'Which exact source revision, effective date, authority class and accepted purpose support this work?',
    evidence: 'Canonical reference identity, fingerprint, reviewer, review time, applicability and supersession evidence.',
    stop: 'Stop if the evidence is only a URL, search snippet, consultation proposal, future-effective document or earlier review of changed bytes.',
  },
  {
    id: 'lesson-identity', title: 'Lesson identity matches its review',
    question: 'Does the exact current lesson fingerprint have the review being relied upon?',
    evidence: 'Current lesson fingerprint and immutable human-review receipt for that exact content.',
    stop: 'Stop if content changed, the review belongs to a nearby lesson, or approval is inferred from source quality.',
  },
  {
    id: 'observable-purpose', title: 'The learning purpose is observable',
    question: 'What will the learner say, do, compare, trace, revise, build or explain?',
    evidence: 'A bounded purpose using observable disciplinary action rather than a personality or ability label.',
    stop: 'Stop if success can only be described as understand, know, improve, try harder or be more confident.',
  },
  {
    id: 'disciplinary-precision', title: 'Disciplinary precision is preserved',
    question: 'Which distinction must not be blurred by simplified language or a generic activity?',
    evidence: 'Subject-reviewed vocabulary, model/non-example or explanation that retains the relevant disciplinary relationship.',
    stop: 'Stop if the activity can be completed while avoiding the intended subject idea.',
  },
  {
    id: 'accessibility', title: 'Access is designed, not assumed',
    question: 'Can the purpose be reached without relying on one sensory, motor, reading-speed, memory or interaction route?',
    evidence: 'Specific alternatives, named environment, accessible labels/order and later human observation for the exact surface.',
    stop: 'Stop if a source marker or checklist is being treated as product-wide accessibility certification.',
  },
  {
    id: 'question-and-observation', title: 'The question gathers evidence',
    question: 'What response would make current reasoning visible, and what follow-up depends on what is actually observed?',
    evidence: 'A pedagogical purpose, primary prompt, adaptive follow-up and separation of direct observation from inference.',
    stop: 'Stop if the follow-up is mechanical, the desired answer is embedded, or silence/error is converted into diagnosis.',
  },
  {
    id: 'feedback', title: 'Feedback offers one bounded action',
    question: 'Which observed feature can be described, and what small learner-owned action follows?',
    evidence: 'Direct evidence, bounded feedback language, an inspectable next action and an interpretation caution.',
    stop: 'Stop if feedback assigns effort, attitude, honesty, ability, identity or a fixed trait.',
  },
  {
    id: 'privacy-and-retention', title: 'Data use is necessary and governed',
    question: 'Is any real learner, reviewer or source data being collected, and why is each field required?',
    evidence: 'Consent/lawful basis where applicable, minimisation, access, retention, deletion, audit and recovery behavior for the exact data flow.',
    stop: 'Stop if static planning UI is being repurposed to capture learner data without a reviewed lifecycle.',
  },
  {
    id: 'runtime-evidence', title: 'Evidence matches the claim',
    question: 'Is the claim about source, build, render, interaction, persistence, restart, human review or teaching outcome?',
    evidence: 'The exact corresponding artifact, command result, runtime observation, database receipt or named human review.',
    stop: 'Stop if source presence, compilation, a synthetic scenario or one narrow pass is being promoted into broader completion.',
  },
];

export function TeachingEvidenceChecklistPanel() {
  return (
    <section className="teaching-evidence-checklist" id="workspace-teaching-evidence-checklist" aria-labelledby="teaching-evidence-checklist-title">
      <header className="teaching-evidence-checklist__header">
        <div><p className="teaching-evidence-checklist__eyebrow">Human review / no score</p><h2 id="teaching-evidence-checklist-title">Ten checks before claiming teaching evidence</h2><p>Open each check when it is relevant. Nothing here can become complete, approved or ready by clicking it.</p></div>
        <span>{checks.length} review prompts</span>
      </header>
      <div className="teaching-evidence-checklist__boundary" role="note">This surface stores nothing and grants no authority. It is a reading aid, not a gate, rubric, certification or substitute for canonical receipts.</div>
      <div className="teaching-evidence-checklist__items">
        {checks.map((check, index) => (
          <details key={check.id}>
            <summary><span>{String(index + 1).padStart(2, '0')}</span><strong>{check.title}</strong></summary>
            <dl>
              <div><dt>Ask</dt><dd>{check.question}</dd></div>
              <div><dt>Evidence needed</dt><dd>{check.evidence}</dd></div>
              <div className="teaching-evidence-checklist__stop"><dt>Stop condition</dt><dd>{check.stop}</dd></div>
            </dl>
          </details>
        ))}
      </div>
    </section>
  );
}
