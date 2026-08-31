export interface JurisdictionStageGuide {
  id: string;
  jurisdiction: 'England' | 'Wales' | 'Scotland' | 'Northern Ireland';
  frameworkShape: string;
  nativeStages: Array<{ label: string; broadScope: string }>;
  internalLensGuidance: string;
  mustNotAssume: string;
  sourceCandidateIds: string[];
  researchedOn: '2026-08-30';
  evidenceStatus: 'Research candidate / unaccepted';
}

export const jurisdictionStageGuides: JurisdictionStageGuide[] = [
  {
    id: 'stage-guide-england', jurisdiction: 'England', frameworkShape: 'EYFS followed by Key Stages 1-4; post-16/adult qualifications are separate frameworks.',
    nativeStages: [
      { label: 'EYFS', broadScope: 'Birth to five; provider-specific statutory framework and effective dates.' },
      { label: 'Key Stage 1', broadScope: 'Years 1-2; broadly ages five to seven.' },
      { label: 'Key Stage 2', broadScope: 'Years 3-6; broadly ages seven to eleven.' },
      { label: 'Key Stage 3', broadScope: 'Years 7-9; broadly ages eleven to fourteen.' },
      { label: 'Key Stage 4', broadScope: 'Years 10-11; broadly ages fourteen to sixteen.' },
      { label: 'Functional Skills', broadScope: 'Qualification levels, not school stages or fixed learner ages.' },
    ],
    internalLensGuidance: 'MA-Teacher KS labels resemble England terminology but remain unaccepted planning filters until an exact governed reference is selected.',
    mustNotAssume: 'Do not infer school type, statutory applicability, learner age or qualification level from a filter alone.',
    sourceCandidateIds: ['curriculum-england-eyfs-2026-transition', 'curriculum-england-national-ks1-4-framework', 'curriculum-england-functional-skills-english'],
    researchedOn: '2026-08-30', evidenceStatus: 'Research candidate / unaccepted',
  },
  {
    id: 'stage-guide-wales', jurisdiction: 'Wales', frameworkShape: 'A three-to-sixteen progression continuum across six Areas of Learning and Experience, with distinct 14-to-16 guidance.',
    nativeStages: [
      { label: 'Curriculum for Wales 3-16', broadScope: 'Progression continuum rather than England-style statutory key-stage subject blocks.' },
      { label: '14 to 16 learning', broadScope: 'Years 10-11 guidance within all six Areas.' },
      { label: 'Adult/community learning', broadScope: 'Separate policy and provision; canonical adult curriculum unresolved in this intake.' },
    ],
    internalLensGuidance: 'Use MA-Teacher stage labels only as interface planning lenses. Preserve progression-step and Area terminology when a governed Welsh reference is used.',
    mustNotAssume: 'Do not rename Areas as England subjects or convert a progression continuum into fixed age-based attainment.',
    sourceCandidateIds: ['curriculum-wales-3-16-framework', 'curriculum-wales-14-16-guidance-2026', 'curriculum-wales-adult-learning-intake-unresolved'],
    researchedOn: '2026-08-30', evidenceStatus: 'Research candidate / unaccepted',
  },
  {
    id: 'stage-guide-scotland', jurisdiction: 'Scotland', frameworkShape: 'Curriculum for Excellence Broad General Education from early learning through S3, followed by the Senior Phase.',
    nativeStages: [
      { label: 'Early level', broadScope: 'Broadly age three to Primary 1.' },
      { label: 'First level', broadScope: 'Broadly Primary 2-4.' },
      { label: 'Second level', broadScope: 'Broadly Primary 5-7.' },
      { label: 'Third/Fourth level', broadScope: 'Broadly Secondary 1-3.' },
      { label: 'Senior Phase', broadScope: 'Secondary 4-6; courses and qualifications build on Broad General Education.' },
      { label: 'Adult Literacies', broadScope: 'Learner-centred adult framework, not school-stage remediation.' },
    ],
    internalLensGuidance: 'Select Scottish governed references using Curriculum for Excellence level and curriculum-area terminology rather than translating from MA-Teacher KS filters.',
    mustNotAssume: 'Official age associations are broad guidance; do not convert levels into fixed age, ability or readiness labels.',
    sourceCandidateIds: ['curriculum-scotland-cfe-broad-general-education', 'curriculum-scotland-cfe-levels-and-senior-phase', 'curriculum-scotland-adult-literacies'],
    researchedOn: '2026-08-30', evidenceStatus: 'Research candidate / unaccepted',
  },
  {
    id: 'stage-guide-northern-ireland', jurisdiction: 'Northern Ireland', frameworkShape: 'Foundation Stage plus Key Stages 1-4 across twelve compulsory years; post-16 entitlement is separate.',
    nativeStages: [
      { label: 'Foundation Stage', broadScope: 'Primary 1-2.' },
      { label: 'Key Stage 1', broadScope: 'Primary 3-4.' },
      { label: 'Key Stage 2', broadScope: 'Primary 5-7.' },
      { label: 'Key Stage 3', broadScope: 'Years 8-10.' },
      { label: 'Key Stage 4', broadScope: 'Years 11-12.' },
      { label: 'Post-16', broadScope: 'Course entitlement context; not a single subject-content curriculum.' },
    ],
    internalLensGuidance: 'MA-Teacher KS labels must retain Northern Ireland year and Area-of-Learning context when a governed reference is selected.',
    mustNotAssume: 'Do not use the 2026 consultation as current authority or map Northern Ireland years directly onto England year groups.',
    sourceCandidateIds: ['curriculum-northern-ireland-current-statutory', 'curriculum-northern-ireland-2026-consultation', 'curriculum-northern-ireland-post16-entitlement'],
    researchedOn: '2026-08-30', evidenceStatus: 'Research candidate / unaccepted',
  },
];
