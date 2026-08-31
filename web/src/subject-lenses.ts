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
  {
    id: 'geography-environment', label: 'Geography and environment', promise: 'Explore places, people, environments, patterns, and how they change across space and time.',
    disciplinaryHabits: ['Use maps, field observations, images, data, and accounts together.', 'Compare places at more than one scale.', 'Separate a spatial pattern from a possible explanation.', 'Notice how choices affect people and environments differently.'],
    evidenceForms: ['Annotated map', 'Fieldwork record', 'Place comparison', 'Data display', 'Evidence-based explanation'], planningQuestions: ['Where is this and why there?', 'Which scale changes the story?', 'Whose view or experience is missing?', 'What evidence supports the claimed pattern?'], cautions: ['A map is a chosen representation, not the whole place.', 'One case study does not represent every place.', 'Environmental choices can involve genuine trade-offs.'],
  },
  {
    id: 'art-design', label: 'Art and design', promise: 'Look closely, explore materials and ideas, make purposeful choices, and reflect on creative work.',
    disciplinaryHabits: ['Observe before judging.', 'Try materials and processes safely.', 'Connect choices to purpose, audience, and context.', 'Keep sketches, trials, changes, and reflections as part of the work.'], evidenceForms: ['Sketchbook trail', 'Material experiment', 'Finished work', 'Design annotation', 'Spoken or written reflection'], planningQuestions: ['What is the learner trying to communicate or solve?', 'Which artist, designer, craft, or culture gives useful context?', 'What can be learned from a trial that did not work?', 'Is copying being mistaken for creative understanding?'], cautions: ['Personal taste is not the same as careful evaluation.', 'A polished result can hide a weak process.', 'Cultural work needs accurate context and respectful attribution.'],
  },
  {
    id: 'music-performance', label: 'Music and performance', promise: 'Listen, perform, create, rehearse, and respond using sound, movement, timing, and expression.',
    disciplinaryHabits: ['Listen for named features more than once.', 'Rehearse short parts with a clear purpose.', 'Connect notation or instructions to sound and action.', 'Balance individual choices with ensemble awareness.'], evidenceForms: ['Live or recorded performance', 'Listening notes', 'Composition draft', 'Rehearsal reflection', 'Notation or sequence'], planningQuestions: ['What should learners hear, feel, perform, or change?', 'Which model makes the feature clear?', 'How will rehearsal feedback lead to another attempt?', 'What access route allows full participation?'], cautions: ['Confidence and musical understanding are not the same thing.', 'One performance does not show every capability.', 'Do not treat one musical tradition as the default for all music.'],
  },
  {
    id: 'physical-education', label: 'Physical education', promise: 'Build movement skills, tactics, safe participation, teamwork, and thoughtful reflection on performance.',
    disciplinaryHabits: ['Warm up and use equipment safely.', 'Break complex movement into observable parts.', 'Use feedback for one focused change at a time.', 'Connect choices, space, timing, and teamwork.'], evidenceForms: ['Observed movement sequence', 'Tactical explanation', 'Personal practice log', 'Peer feedback', 'Safe participation record'], planningQuestions: ['What movement or decision is the real goal?', 'What safe adaptation keeps the goal meaningful?', 'What can the learner notice during and after action?', 'Does the task reward learning or only prior physical advantage?'], cautions: ['Competition results are not a complete measure of learning.', 'Fitness, health, skill, and body shape are different ideas.', 'Health or injury concerns need an appropriate adult or professional.'],
  },
  {
    id: 'citizenship-media', label: 'Citizenship and media literacy', promise: 'Ask who made a claim, what evidence supports it, who is affected, and how people can take part responsibly.',
    disciplinaryHabits: ['Separate fact, opinion, interpretation, persuasion, and uncertainty.', 'Check author, date, source, evidence, and missing context.', 'Compare rights, duties, power, and consequences.', 'Discuss disagreement without attacking people.'], evidenceForms: ['Source comparison', 'Claim check', 'Reasoned discussion', 'Stakeholder map', 'Action plan with safeguards'], planningQuestions: ['Who benefits or is affected?', 'What would change your mind?', 'Is the source current and trustworthy for this claim?', 'How can a learner act safely and lawfully?'], cautions: ['Popular does not mean true.', 'False balance can make weak evidence look equal to strong evidence.', 'Never ask learners to expose private accounts or personal political beliefs.'],
  },
  {
    id: 'health-wellbeing', label: 'Health and wellbeing', promise: 'Learn practical ways to care for the body, mind, relationships, and safety without pretending to diagnose anyone.',
    disciplinaryHabits: ['Use age-appropriate, evidence-based information.', 'Separate general education from personal medical advice.', 'Practise safe choices and ways to ask trusted adults for help.', 'Respect privacy, consent, boundaries, and different experiences.'], evidenceForms: ['Safety plan', 'Scenario response', 'Information comparison', 'Reflection with privacy boundaries', 'Practical demonstration'], planningQuestions: ['Is this suitable for the learner and setting?', 'Which safeguarding rule applies?', 'What should stay private?', 'When must a trusted adult or qualified professional help?'], cautions: ['The app does not diagnose, counsel, or handle emergencies.', 'Do not ask for private disclosures in ordinary learning records.', 'Urgent safety concerns need immediate human action, not an app response.'],
  },
  {
    id: 'money-life-skills', label: 'Money and life skills', promise: 'Use numbers, information, and careful choices for everyday tasks such as budgets, bills, saving, and planning.',
    disciplinaryHabits: ['Name the goal, limits, and trade-offs.', 'Check units, totals, dates, rates, and small print.', 'Compare more than one option using the same criteria.', 'Protect personal and financial information.'], evidenceForms: ['Simple budget', 'Option comparison', 'Step-by-step plan', 'Completed practical task', 'Risk and safeguard list'], planningQuestions: ['What does the learner need to decide or do?', 'Which information is missing?', 'What could go wrong and how can it be checked?', 'Does the example avoid real account or identity data?'], cautions: ['Example calculations are not personal financial advice.', 'Low price is not always low total cost.', 'Never enter real bank, card, password, or identity details.'],
  },
  {
    id: 'religion-philosophy', label: 'Religion, belief and philosophy', promise: 'Explore beliefs, practices, reasons, meanings, and ethical questions with evidence, care, and respect.',
    disciplinaryHabits: ['Distinguish describing a belief from agreeing with it.', 'Use accurate sources from and about traditions.', 'Compare reasons and consequences without flattening differences.', 'Ask open questions while respecting personal boundaries.'], evidenceForms: ['Concept comparison', 'Source interpretation', 'Reasoned dialogue', 'Ethical argument', 'Contextual explanation'], planningQuestions: ['Is this an insider, outsider, historical, or current account?', 'Are differences within a tradition visible?', 'What reasons support each position?', 'Can learners take part without revealing personal belief?'], cautions: ['No tradition or non-religious worldview is completely uniform.', 'Personal belief must not be demanded or graded.', 'Respectful disagreement still requires evidence and clear reasoning.'],
  },
] as const;

export type SubjectLens = typeof subjectLenses[number];
export type SubjectLensId = SubjectLens['id'];
