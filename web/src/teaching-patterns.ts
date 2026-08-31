export const teachingSubjects = ['all', 'english', 'mathematics', 'science', 'history', 'languages', 'computing', 'cross-curricular'] as const;
export type TeachingSubject = typeof teachingSubjects[number];

export type TeachingPattern = {
  id: string;
  title: string;
  subjects: Exclude<TeachingSubject, 'all'>[];
  purpose: string;
  sequence: string[];
  adaptationPrompts: string[];
  caution: string;
};

export const teachingPatternAuthority = {
  evidenceState: 'editorial-scaffold-unreviewed',
  stageBoundary: 'Patterns are not classified as suitable for any age or stage. Review the chosen structure against the selected stage lens, subject evidence, accessibility needs, and qualified human judgement before use.',
} as const;

export const teachingPatterns: TeachingPattern[] = [
  {
    id: 'model-guided-independent',
    title: 'Model, guide, release',
    subjects: ['english', 'mathematics', 'science', 'languages', 'computing', 'cross-curricular'],
    purpose: 'Make an unfamiliar process visible before asking the learner to perform it independently.',
    sequence: ['Name the narrow goal and necessary prior knowledge.', 'Think aloud through one complete worked example.', 'Complete a second example together with prompts.', 'Give an independent example with the same underlying structure.', 'Review the response against observable criteria.'],
    adaptationPrompts: ['Which step needs a visual, concrete object, or short sentence?', 'Can the learner explain why a step is used rather than copy it?', 'What support can be removed without changing the goal?'],
    caution: 'A worked example is not evidence that the learner can transfer the process to a new context.',
  },
  {
    id: 'retrieve-connect-extend',
    title: 'Retrieve, connect, extend',
    subjects: ['english', 'mathematics', 'science', 'history', 'languages', 'computing', 'cross-curricular'],
    purpose: 'Reactivate relevant knowledge and connect it explicitly to one new idea.',
    sequence: ['Ask a small number of low-stakes retrieval questions.', 'Inspect responses before introducing new content.', 'State the connection between known and new material.', 'Teach one bounded extension.', 'Check the connection in a new example.'],
    adaptationPrompts: ['Is the retrieval content genuinely prerequisite?', 'Can the learner respond orally, visually, practically, or in writing?', 'Does the extension add one difficulty rather than several?'],
    caution: 'Retrieval should inform teaching, not become a hidden grade or speed competition.',
  },
  {
    id: 'compare-evidence-claim',
    title: 'Compare evidence, build a claim',
    subjects: ['english', 'science', 'history', 'cross-curricular'],
    purpose: 'Help the learner distinguish an observation or source from the conclusion drawn from it.',
    sequence: ['Present two bounded pieces of evidence with provenance.', 'Identify what each directly shows.', 'Identify missing context or limitations.', 'Draft a claim no broader than the evidence.', 'Revise the claim after a counterexample or additional source.'],
    adaptationPrompts: ['Is each source readable and contextualized?', 'Can evidence and inference be marked in different columns?', 'What wording signals uncertainty accurately?'],
    caution: 'A persuasive statement is not automatically an evidence-supported statement.',
  },
  {
    id: 'predict-observe-explain',
    title: 'Predict, observe, explain',
    subjects: ['science', 'mathematics', 'computing', 'cross-curricular'],
    purpose: 'Make prior reasoning visible, gather bounded evidence, and revise an explanation.',
    sequence: ['State the phenomenon, data set, or program behavior precisely.', 'Record a prediction and its reason.', 'Observe or run one controlled example.', 'Compare prediction and observation.', 'Revise the explanation and name remaining uncertainty.'],
    adaptationPrompts: ['Can the observation be made safely and repeatedly?', 'Which variable or program input changes?', 'Could a diagram, trace table, or physical model reduce language load?'],
    caution: 'A surprising observation should trigger investigation, not a judgement about the learner.',
  },
  {
    id: 'example-nonexample-boundary',
    title: 'Example, non-example, boundary',
    subjects: ['english', 'mathematics', 'science', 'history', 'languages', 'computing', 'cross-curricular'],
    purpose: 'Clarify a concept by testing where it applies and where it does not.',
    sequence: ['Give one clear example and name the defining features.', 'Give a close non-example.', 'Ask which feature changes the classification.', 'Test an ambiguous boundary case.', 'Ask the learner to create and justify a new pair.'],
    adaptationPrompts: ['Are irrelevant surface features distracting from the concept?', 'Can examples use familiar contexts without becoming childish?', 'Is the boundary case genuinely resolvable from taught criteria?'],
    caution: 'Do not treat one culturally familiar example as the definition of a concept.',
  },
  {
    id: 'language-notice-rehearse-use',
    title: 'Notice, rehearse, use language',
    subjects: ['english', 'languages', 'science', 'history', 'mathematics', 'computing'],
    purpose: 'Teach vocabulary or a language structure in meaningful context before independent use.',
    sequence: ['Introduce the word or structure in a short meaningful example.', 'Clarify meaning, form, pronunciation, or notation as relevant.', 'Contrast it with a likely confusion.', 'Rehearse through a bounded supported response.', 'Use it independently in a new sentence, explanation, or problem.'],
    adaptationPrompts: ['Which meaning is required in this subject context?', 'Can pronunciation, spelling, symbol, and meaning be separated?', 'Does the learner need a sentence frame temporarily?'],
    caution: 'Vocabulary recall alone does not prove conceptual understanding or fluent language use.',
  },
  {
    id: 'debug-explain-improve',
    title: 'Debug, explain, improve',
    subjects: ['computing', 'mathematics', 'science', 'languages', 'english', 'cross-curricular'],
    purpose: 'Use an error as inspectable evidence and improve a process without attaching the error to learner identity.',
    sequence: ['Preserve the original attempt or output.', 'Locate the first point where evidence diverges from the intended result.', 'Explain the cause using taught language.', 'Change one relevant part.', 'Rerun or reread and compare the result.'],
    adaptationPrompts: ['Can the process be traced one step at a time?', 'Is the success criterion visible before correction?', 'Would a smaller failing example isolate the cause?'],
    caution: 'Correct output without an explanation may hide guessing or an unrelated change.',
  },
];
