import './teaching-toolkit-path.css';

const steps = [
  {
    number: '01',
    title: 'Frame the subject, stage, goal, and evidence',
    description: 'Use the non-canonical session brief to name one subject lens, one stage lens, one narrow goal, and the observable evidence that would matter.',
    question: 'What exactly is being taught, for which stage lens, and what could be observed without assigning a score or learner label?',
    href: '#workspace-session-brief',
    linkLabel: 'Open teaching-session brief',
  },
  {
    number: '02',
    title: 'Review a teaching structure deliberately',
    description: 'Inspect an unreviewed editorial pattern as a possible scaffold, then test every step against the selected subject, stage, accessibility needs, and qualified judgement.',
    question: 'Why might this structure fit the stated goal, and what would make it unsuitable here?',
    href: '#workspace-patterns',
    linkLabel: 'Open teaching patterns',
  },
  {
    number: '03',
    title: 'Name the disciplinary idea',
    description: 'Choose language that preserves the subject distinction rather than replacing it with a vague synonym.',
    question: 'What must the learner be able to distinguish or use?',
    href: '#workspace-vocabulary-planning',
    linkLabel: 'Open vocabulary planning',
  },
  {
    number: '04',
    title: 'Ask for evidence of thinking',
    description: 'Choose a prompt for a genuine pedagogical purpose and prepare a follow-up that can adapt to the response.',
    question: 'What response or action would make the learner\'s current reasoning more visible?',
    href: '#workspace-questioning-planning',
    linkLabel: 'Open questioning planning',
  },
  {
    number: '05',
    title: 'Notice before interpreting',
    description: 'Record what was actually said, written, selected, built or demonstrated before assigning an explanation.',
    question: 'Which part is direct evidence, and which part is still an inference?',
    href: '#workspace-questioning-planning',
    linkLabel: 'Review evidence cautions',
  },
  {
    number: '06',
    title: 'Offer one bounded next action',
    description: 'Respond to the observed feature with language the learner can act on without turning it into a fixed judgement.',
    question: 'What small revision, comparison, trace or explanation can the learner attempt next?',
    href: '#workspace-feedback-planning',
    linkLabel: 'Open feedback planning',
  },
];

export function TeachingToolkitPathPanel() {
  return (
    <section className="teaching-toolkit-path" id="workspace-teaching-toolkit-path" aria-labelledby="teaching-toolkit-path-title">
      <header className="teaching-toolkit-path__header">
        <div>
          <p className="teaching-toolkit-path__eyebrow">Quick start / no automation</p>
          <h2 id="teaching-toolkit-path-title">Plan, ask, notice, respond</h2>
          <p>Use the teaching banks as a thinking route, not a machine that decides what a learner knows.</p>
        </div>
        <span>6 human-led steps</span>
      </header>

      <div className="teaching-toolkit-path__boundary" role="note">
        This guide moves only the reader. It carries no selections, learner data, lesson approval, score, diagnosis, or model instruction between surfaces.
      </div>

      <ol className="teaching-toolkit-path__steps">
        {steps.map((step) => (
          <li key={step.number}>
            <span className="teaching-toolkit-path__number" aria-hidden="true">{step.number}</span>
            <div>
              <h3>{step.title}</h3>
              <p>{step.description}</p>
              <blockquote>{step.question}</blockquote>
              <a href={step.href}>{step.linkLabel}<span aria-hidden="true"> -&gt;</span></a>
            </div>
          </li>
        ))}
      </ol>

      <footer>
        <strong>Stop and check:</strong> if the evidence is missing, ambiguous, inaccessible, or contradicted by the learner, gather better evidence rather than forcing the stored path.
      </footer>
    </section>
  );
}
