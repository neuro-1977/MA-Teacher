export const subjectLenses = [
  {
    id: 'english',
    label: 'English',
    promise: 'Read, discuss, compose, revise, and justify meaning with attention to audience and text.',
    disciplinaryHabits: ['Use precise evidence from the text or language example.', 'Separate comprehension, interpretation, and evaluation.', 'Treat planning, drafting, revising, editing, and publishing as distinct actions.', 'Teach vocabulary, grammar, spelling, and form in meaningful context.'],
    evidenceForms: ['Annotated passage', 'Oral explanation', 'Draft and revision trail', 'Comparative paragraph', 'Performance or reading response'],
    planningQuestions: ['What exact reading, writing, speaking, or listening action is the goal?', 'Which text features or language choices need explicit modelling?', 'What would a successful response visibly contain?', 'Is reading demand obscuring the intended knowledge?'],
    cautions: ['One interpretation is not the only possible interpretation when alternatives are evidenced.', 'Surface accuracy alone does not establish meaning or quality.', 'Do not confuse a learner accent or dialect with a lack of understanding.'],
  },
  {
    id: 'mathematics',
    label: 'Mathematics',
    promise: 'Notice structure, represent relationships, reason, calculate, and test whether an answer makes sense.',
    disciplinaryHabits: ['Move deliberately between concrete, pictorial, symbolic, verbal, and graphical representations.', 'Explain why a method works, not only which steps to copy.', 'Use examples and non-examples to expose mathematical structure.', 'Estimate, check units, and test reasonableness.'],
    evidenceForms: ['Worked solution with reasoning', 'Diagram or model', 'Multiple representations', 'Counterexample', 'Error analysis'],
    planningQuestions: ['What structure should the learner notice?', 'Which prior fact, representation, or operation is genuinely prerequisite?', 'Does the example vary one relevant feature at a time?', 'Can the learner verify the result independently?'],
    cautions: ['Speed is not equivalent to understanding.', 'A correct answer does not prove a sound method.', 'Do not introduce an efficient shortcut before its meaning is secure.'],
  },
  {
    id: 'science',
    label: 'Science',
    promise: 'Use observations, measurements, models, and established knowledge to explain the natural and material world.',
    disciplinaryHabits: ['Distinguish observation, measurement, inference, model, and conclusion.', 'Control or account for relevant variables where possible.', 'Use units, uncertainty, repeat observations, and suitable comparisons.', 'Revise explanations when evidence or model limits require it.'],
    evidenceForms: ['Observation record', 'Data table or graph', 'Labelled model', 'Method and variable account', 'Evidence-bounded explanation'],
    planningQuestions: ['What can be observed directly and what must be inferred?', 'What safety or ethical constraints apply?', 'Which variable changes, which is measured, and which must be controlled?', 'Where does the model stop matching reality?'],
    cautions: ['A classroom demonstration is not always a controlled investigation.', 'Correlation does not by itself establish cause.', 'A model is a useful representation, not the phenomenon itself.'],
  },
  {
    id: 'history',
    label: 'History and histories',
    promise: 'Build warranted accounts of the past using chronology, sources, interpretations, context, and contested evidence.',
    disciplinaryHabits: ['Place events and developments in chronological and geographical context.', 'Ask who created a source, when, for what audience, and for what purpose.', 'Distinguish primary evidence, later interpretation, and present-day judgement.', 'Compare change, continuity, cause, consequence, similarity, difference, and significance.'],
    evidenceForms: ['Timeline with scale', 'Source provenance analysis', 'Comparison of interpretations', 'Causal explanation', 'Evidence-bounded historical account'],
    planningQuestions: ['Which historical question is the evidence meant to answer?', 'Whose experience is visible or absent?', 'What can this source support and what can it not support?', 'Are multiple causes or timescales being collapsed into one story?'],
    cautions: ['A source is evidence to interrogate, not a transparent window onto the past.', 'Present-day values should not replace historical context or evidence.', 'One account must not be presented as universal when the evidence is partial or contested.'],
  },
  {
    id: 'languages',
    label: 'Languages',
    promise: 'Understand and communicate meaning through listening, speaking, reading, and writing in another language.',
    disciplinaryHabits: ['Connect sound, spelling, meaning, grammar, and context.', 'Build receptive understanding before and alongside productive use.', 'Rehearse high-value language in varied meaningful combinations.', 'Use errors diagnostically while protecting willingness to communicate.'],
    evidenceForms: ['Listening discrimination', 'Short spoken exchange', 'Reading annotation', 'Sentence transformation', 'Draft and revised communication'],
    planningQuestions: ['Is the goal meaning, pronunciation, grammar, vocabulary, fluency, or a combination?', 'Which language must be understood before it is produced?', 'Does the example sound natural in its cultural and communicative context?', 'Can support be reduced while meaning remains clear?'],
    cautions: ['Word-for-word translation can hide differences in meaning and structure.', 'Accent variation is not failure when communication remains intelligible.', 'Vocabulary recall alone does not establish communicative competence.'],
  },
  {
    id: 'computing',
    label: 'Computing and IT',
    promise: 'Represent information, design processes, build and debug systems, and use technology safely and deliberately.',
    disciplinaryHabits: ['Decompose a problem and name inputs, processes, outputs, and constraints.', 'Trace an algorithm or program state before changing code.', 'Distinguish data, representation, software behavior, hardware behavior, and network behavior.', 'Treat security, privacy, accessibility, and user consequences as design requirements.'],
    evidenceForms: ['Algorithm or flow description', 'Trace table', 'Working artifact plus explanation', 'Test cases and results', 'Debugging change record'],
    planningQuestions: ['What problem is being solved and for whom?', 'Which state or data changes at each step?', 'What evidence would distinguish code error, data error, environment error, and expectation error?', 'What safety, privacy, accessibility, or security boundary applies?'],
    cautions: ['A working artifact without understood behavior is incomplete evidence.', 'Copying code is not the same as reasoning about it.', 'Do not teach security by asking learners to expose real credentials or personal data.'],
  },
] as const;

export type SubjectLens = typeof subjectLenses[number];
export type SubjectLensId = SubjectLens['id'];
