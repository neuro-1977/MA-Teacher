export const assessmentGroups = ['all', 'knowledge', 'reasoning', 'performance', 'communication'] as const;
export type AssessmentGroup = typeof assessmentGroups[number];

export type AssessmentPattern = {
  id: string;
  title: string;
  group: Exclude<AssessmentGroup, 'all'>;
  useWhen: string;
  promptFrame: string;
  criteria: string[];
  feedbackQuestions: string[];
  caution: string;
};

export const assessmentPatterns: AssessmentPattern[] = [
  {
    id: 'bounded-retrieval',
    title: 'Bounded retrieval',
    group: 'knowledge',
    useWhen: 'A small, clearly taught fact, term, symbol, sequence, or relationship is needed for the next learning step.',
    promptFrame: 'Without reopening the source, state or represent [specific taught knowledge]. Add one example showing what it means.',
    criteria: ['The requested knowledge is present.', 'The response matches the taught scope.', 'The example is relevant rather than decorative.'],
    feedbackQuestions: ['Which part was secure?', 'Which exact item needs another example or cue?', 'Can the learner recognize the same knowledge in a different representation?'],
    caution: 'Recall speed and quantity must not become a hidden grade or broad memory judgement.',
  },
  {
    id: 'explain-a-process',
    title: 'Explain a process or method',
    group: 'reasoning',
    useWhen: 'The learner needs to show why steps, language choices, operations, or program states produce a result.',
    promptFrame: 'Explain how [result/process] works. Name each relevant step and why it is needed.',
    criteria: ['The sequence is coherent.', 'Relevant causes or reasons are stated.', 'The explanation uses appropriate subject vocabulary.', 'The result is linked to the process rather than asserted.'],
    feedbackQuestions: ['Where does the explanation first become unclear?', 'Which connection needs evidence or an example?', 'Could a diagram, trace, or worked line make the reasoning visible?'],
    caution: 'Fluent wording can hide incorrect reasoning; inspect the relationships, not just the style.',
  },
  {
    id: 'apply-new-context',
    title: 'Apply in a related new context',
    group: 'reasoning',
    useWhen: 'A taught principle, structure, or process should transfer beyond the worked example without adding unrelated difficulty.',
    promptFrame: 'Use [taught idea] to solve, interpret, compose, or explain [new but structurally related case]. Show where the idea applies.',
    criteria: ['The relevant taught idea is selected.', 'It is applied to the changed context.', 'The response identifies the connection.', 'Irrelevant surface features do not drive the method.'],
    feedbackQuestions: ['Did the learner recognize the underlying structure?', 'Which changed feature caused difficulty?', 'Would a closer case separate transfer from new knowledge?'],
    caution: 'A radically unfamiliar context may test reading, background knowledge, or confidence more than the intended idea.',
  },
  {
    id: 'compare-with-criteria',
    title: 'Compare using named criteria',
    group: 'reasoning',
    useWhen: 'Two texts, methods, sources, organisms, events, representations, languages, or systems need disciplined comparison.',
    promptFrame: 'Compare [A] and [B] using [named criteria]. Give evidence for one similarity, one difference, and why either matters.',
    criteria: ['The same criteria are applied to both cases.', 'Claims use relevant evidence.', 'Similarity and difference are distinguished.', 'Significance is explained within scope.'],
    feedbackQuestions: ['Was comparison replaced by two separate descriptions?', 'Which criterion needs clearer evidence?', 'Is the claimed significance broader than the evidence?'],
    caution: 'More listed differences do not automatically make a stronger comparison.',
  },
  {
    id: 'error-analysis',
    title: 'Error analysis and correction',
    group: 'reasoning',
    useWhen: 'An existing response, calculation, explanation, translation, method, or program output can be inspected safely.',
    promptFrame: 'Find the first point where this attempt diverges from the intended result. Explain the cause, change one relevant part, and check again.',
    criteria: ['The original attempt remains visible.', 'The first relevant divergence is identified.', 'The correction addresses the cause.', 'The revised result is checked against explicit criteria.'],
    feedbackQuestions: ['Was the cause identified or only the symptom?', 'Did the change introduce another effect?', 'What check would catch this error independently next time?'],
    caution: 'Do not attach the error to learner identity or treat correction as punishment.',
  },
  {
    id: 'source-evaluation',
    title: 'Evaluate a source or evidence claim',
    group: 'knowledge',
    useWhen: 'The learner must reason about provenance, method, audience, context, limitations, or what evidence can support.',
    promptFrame: 'What does this source or result directly show? Who or what produced it, in what context, and what conclusion would go beyond it?',
    criteria: ['Direct evidence is separated from inference.', 'Relevant provenance or method is identified.', 'At least one limitation or missing context is named.', 'The conclusion remains bounded.'],
    feedbackQuestions: ['Which sentence is direct evidence?', 'Which sentence is interpretation?', 'What additional evidence would change confidence?'],
    caution: 'A source is not reliable or unreliable in every possible use; evaluation must answer a specific question.',
  },
  {
    id: 'practical-demonstration',
    title: 'Practical demonstration with observation',
    group: 'performance',
    useWhen: 'A safe physical, technical, spoken, or procedural performance is part of the intended learning.',
    promptFrame: 'Demonstrate [bounded skill/process] while explaining the critical decisions. Record the result and one check.',
    criteria: ['Safety and setup requirements are met.', 'Critical steps are observable.', 'The result or output is recorded.', 'The learner explains at least one decision or check.'],
    feedbackQuestions: ['Which part was observed directly?', 'Did assistance change what the demonstration proves?', 'What should be repeated under the same conditions?'],
    caution: 'Never infer a practical result from a written description when direct observation is required.',
  },
  {
    id: 'communicate-for-purpose',
    title: 'Communicate for a purpose and audience',
    group: 'communication',
    useWhen: 'Meaning must be expressed through speech, writing, presentation, code, diagram, or another deliberate form.',
    promptFrame: 'Create [communication] for [stated audience and purpose] using [required knowledge or language]. Explain one choice you made.',
    criteria: ['The intended meaning is clear.', 'Content suits the stated purpose.', 'Form and vocabulary suit the intended audience.', 'The explained choice is visible in the result.'],
    feedbackQuestions: ['What does the audience need to understand first?', 'Which choice improves clarity or effect?', 'Which correction changes meaning and which is surface editing?'],
    caution: 'Presentation polish must not outweigh the intended subject knowledge or communicative meaning.',
  },
];
