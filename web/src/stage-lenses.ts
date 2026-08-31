export const stageLenses = [
  {
    id: 'early-learning',
    label: 'Early learning',
    support: 'reference-only',
    curriculumBoundary: 'EYFS is recognized but not configured as a supported curriculum lane.',
    planningFocus: ['Short purposeful interactions with a responsive adult.', 'Concrete objects, movement, talk, play, stories, songs, and visible routines.', 'Observation of communication and action rather than formal written output.', 'Vocabulary and ideas revisited in varied familiar contexts.'],
    responseOptions: ['Point, choose, sort, build, move, draw, say, imitate, retell, or demonstrate.'],
    dignityRules: ['Do not convert ordinary developmental variation into a diagnosis.', 'Do not use formal scoring to replace careful adult observation.', 'Do not store unnecessary family, health, or safeguarding detail in lesson responses.'],
  },
  {
    id: 'ks1',
    label: 'Key Stage 1',
    support: 'configured-partial',
    curriculumBoundary: 'England KS1 evidence lanes are configured for selected subjects but remain partial and review-gated.',
    planningFocus: ['State one narrow goal in concrete language.', 'Model with objects, images, talk, actions, and short written examples.', 'Separate decoding or handwriting demand from the intended subject knowledge where possible.', 'Use brief guided practice and immediate descriptive feedback.'],
    responseOptions: ['Oral answer, selection, sequencing, drawing, labelled representation, practical action, or short written response.'],
    dignityRules: ['Do not treat reading speed, handwriting, or attention duration as general ability.', 'Avoid praise or correction that attaches performance to fixed identity.', 'Use age-appropriate warmth without making the content babyish.'],
  },
  {
    id: 'ks2',
    label: 'Key Stage 2',
    support: 'configured-partial',
    curriculumBoundary: 'England KS2 evidence lanes are configured for selected subjects but remain partial and review-gated.',
    planningFocus: ['Connect new material to explicit prior knowledge.', 'Move between concrete, visual, verbal, written, numerical, and practical representations.', 'Model subject vocabulary and increasingly complete explanations.', 'Increase independence by removing support deliberately rather than abruptly.'],
    responseOptions: ['Explanation, worked example, annotated source, diagram, comparison, practical record, short composition, or structured discussion.'],
    dignityRules: ['Do not assume every learner shares the same cultural or background knowledge.', 'Do not mistake longer writing for stronger understanding.', 'Keep support available without making it socially punitive.'],
  },
  {
    id: 'ks3',
    label: 'Key Stage 3',
    support: 'configured-partial',
    curriculumBoundary: 'England KS3 evidence lanes are configured for selected subjects but remain partial and review-gated.',
    planningFocus: ['Make disciplinary vocabulary, representations, and reasoning explicit.', 'Surface prerequisite gaps without turning them into fixed learner labels.', 'Use examples that expose increasing abstraction and connected concepts.', 'Ask learners to explain, compare, model, test, revise, and justify.'],
    responseOptions: ['Extended explanation, investigation, source evaluation, model, program, presentation, debate preparation, or multi-step solution.'],
    dignityRules: ['Avoid childish presentation when revisiting earlier knowledge.', 'Do not equate compliance, confidence, or fluent speech with understanding.', 'Give feedback on the work and next evidence, not personality.'],
  },
  {
    id: 'ks4',
    label: 'Key Stage 4',
    support: 'configured-partial',
    curriculumBoundary: 'England KS4 evidence lanes are configured for selected subjects but do not yet constitute qualification or exam-board coverage.',
    planningFocus: ['Connect detailed knowledge to disciplinary methods and larger structures.', 'Use cumulative practice without reducing learning to test rehearsal.', 'Model complex responses, then inspect planning, evidence, reasoning, and revision separately.', 'Make qualification-specific demands explicit only when their official evidence is configured.'],
    responseOptions: ['Sustained analysis, multi-step solution, practical investigation, extended composition, source synthesis, program artifact, or oral defence.'],
    dignityRules: ['Do not present predicted grades or exam performance without an authorized evidence model.', 'Do not let time pressure erase accessibility or reasoning.', 'Respect different destinations and motivations without lowering stated evidence standards silently.'],
  },
  {
    id: 'post16-adult',
    label: 'Post-16 and adult learning',
    support: 'reference-only',
    curriculumBoundary: 'Post-16, qualifications, Functional Skills, workplace, and adult-learning curricula are reference-only until each route is configured.',
    planningFocus: ['Name the exact qualification, workplace, personal, or further-study purpose.', 'Recognize relevant experience without assuming prior formal knowledge.', 'Use adult-respectful examples, interface language, and pacing at every content level.', 'Make optional support discreet and preserve learner agency.'],
    responseOptions: ['Professional scenario, practical demonstration, portfolio artifact, discussion, extended response, project, revision record, or applied problem.'],
    dignityRules: ['Never use childish styling because content is foundational.', 'Do not infer intelligence from interrupted education, literacy, language, disability, or digital confidence.', 'Keep employment, health, immigration, financial, and family details out of teaching records unless an explicit future lawful need is designed.'],
  },
] as const;

export type StageLens = typeof stageLenses[number];
export type StageLensId = StageLens['id'];
