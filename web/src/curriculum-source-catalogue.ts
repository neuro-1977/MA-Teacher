export type CurriculumSourceJurisdiction =
  | 'England'
  | 'Scotland'
  | 'Wales'
  | 'Northern Ireland'
  | 'Cross-UK reference';

export type CurriculumSourceClass =
  | 'Statutory framework'
  | 'Curriculum guidance'
  | 'Qualification content'
  | 'Occupational standard route'
  | 'Teaching resource'
  | 'Rights guidance';

export type CurriculumSourceReference = {
  id: string;
  title: string;
  authority: string;
  jurisdiction: CurriculumSourceJurisdiction;
  sourceClass: CurriculumSourceClass;
  stage: string;
  scope: string;
  url: string;
  caution: string;
  observedUtc: string;
  importState: 'NOT_IMPORTED';
};

export const curriculumSourceReferences: readonly CurriculumSourceReference[] = [
  {
    id: 'england-eyfs',
    title: 'Early years foundation stage statutory framework',
    authority: 'Department for Education',
    jurisdiction: 'England',
    sourceClass: 'Statutory framework',
    stage: 'Birth to five',
    scope: 'Separate current frameworks for childminders and group or school-based providers.',
    url: 'https://www.gov.uk/government/publications/early-years-foundation-stage-framework--2',
    caution: 'Capture provider type and exact effective period; the official page can expose overlapping outgoing and incoming versions.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'england-national-curriculum',
    title: 'National curriculum framework for key stages 1 to 4',
    authority: 'Department for Education',
    jurisdiction: 'England',
    sourceClass: 'Statutory framework',
    stage: 'Ages 5 to 16',
    scope: 'Framework, subjects, programmes of study, and attainment targets for maintained schools.',
    url: 'https://www.gov.uk/government/publications/national-curriculum-in-england-framework-for-key-stages-1-to-4/the-national-curriculum-in-england-framework-for-key-stages-1-to-4',
    caution: 'Do not infer identical legal obligations for academies, private schools, or another UK nation.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'england-a-level-content',
    title: 'GCE AS and A level subject content',
    authority: 'Department for Education',
    jurisdiction: 'England',
    sourceClass: 'Qualification content',
    stage: 'Post-16 academic',
    scope: 'Common subject knowledge, understanding, skills, and assessment expectations.',
    url: 'https://www.gov.uk/government/collections/gce-as-and-a-level-subject-content',
    caution: 'An awarding-body specification is still required for a complete current course.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'england-functional-english',
    title: 'Functional Skills subject content: English',
    authority: 'Department for Education',
    jurisdiction: 'England',
    sourceClass: 'Qualification content',
    stage: 'Entry levels 1 to 3 and levels 1 to 2',
    scope: 'English Functional Skills purposes, learning aims, outcomes, and scope.',
    url: 'https://www.gov.uk/government/publications/functional-skills-subject-content-english',
    caution: 'Adult learning is goal- and qualification-specific; do not map these levels to school years.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'england-functional-maths',
    title: 'Functional Skills subject content: mathematics',
    authority: 'Department for Education',
    jurisdiction: 'England',
    sourceClass: 'Qualification content',
    stage: 'Entry levels 1 to 3 and levels 1 to 2',
    scope: 'Mathematics Functional Skills purposes, learning aims, outcomes, and scope.',
    url: 'https://www.gov.uk/government/publications/functional-skills-subject-content-mathematics',
    caution: 'Pair with the learner goal and current awarding-body specification.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'england-functional-digital',
    title: 'Functional Skills subject content: digital',
    authority: 'Department for Education',
    jurisdiction: 'England',
    sourceClass: 'Qualification content',
    stage: 'Entry level and level 1',
    scope: 'Digital Functional Skills purposes, learning aims, and outcomes.',
    url: 'https://www.gov.uk/government/publications/digital-functional-skills-qualifications',
    caution: 'Technology and qualification requirements can drift; capture exact version and effective dates.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'england-occupational-maps',
    title: 'Skills England occupational maps',
    authority: 'Skills England',
    jurisdiction: 'England',
    sourceClass: 'Occupational standard route',
    stage: 'Post-16 technical and occupational',
    scope: 'Occupations, standards, technical-education products, and progression routes.',
    url: 'https://skillsengland.education.gov.uk/occupational-maps/',
    caution: 'An occupational standard describes competence; it is not automatically a lesson sequence or assessment.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'scotland-cfe',
    title: 'Curriculum for Excellence: Building the Curriculum',
    authority: 'Education Scotland',
    jurisdiction: 'Scotland',
    sourceClass: 'Curriculum guidance',
    stage: 'Early learning to senior phase',
    scope: 'Curriculum areas, four capacities, learning, teaching, skills, and assessment frameworks.',
    url: 'https://education.gov.scot/curriculum-for-excellence/curriculum-for-excellence-documents/building-the-curriculum/',
    caution: 'Do not convert Curriculum for Excellence levels to English key stages by age alone.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'wales-curriculum',
    title: 'Curriculum for Wales',
    authority: 'Welsh Government through Hwb',
    jurisdiction: 'Wales',
    sourceClass: 'Curriculum guidance',
    stage: 'Nursery to 16 and transition beyond',
    scope: 'Four purposes, six areas of learning and experience, progression, assessment, and 14-to-16 guidance.',
    url: 'https://hwb.gov.wales/curriculum-for-wales',
    caution: 'Capture section-level changes and legal status; the 14-to-16 guidance changed during 2026.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'northern-ireland-curriculum',
    title: 'Northern Ireland school curriculum overview',
    authority: 'Northern Ireland government, routing to CCEA',
    jurisdiction: 'Northern Ireland',
    sourceClass: 'Curriculum guidance',
    stage: 'Foundation Stage to key stage 4',
    scope: 'Stage, year, assessment, and minimum-content overview with detailed CCEA routes.',
    url: 'https://www.nidirect.gov.uk/articles/school-curriculum',
    caution: 'This is a routing source. Missing detailed CCEA evidence must remain missing rather than inferred.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'oak-curricula',
    title: 'Oak National Academy curriculum plans',
    authority: 'Oak National Academy',
    jurisdiction: 'England',
    sourceClass: 'Teaching resource',
    stage: 'Key stages 1 to 4',
    scope: 'Sequenced curriculum-aligned plans and teaching resources across multiple subjects.',
    url: 'https://www.thenational.academy/curriculum',
    caution: 'Aligned teaching material is not statutory authority. Rights must be checked per resource before adaptation.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
  {
    id: 'oak-licensing',
    title: 'Oak National Academy website licensing guide',
    authority: 'Oak National Academy',
    jurisdiction: 'Cross-UK reference',
    sourceClass: 'Rights guidance',
    stage: 'Resource reuse',
    scope: 'Publication-era licence split, attribution, media restrictions, and third-party rights.',
    url: 'https://support.thenational.academy/a-guide-to-our-website-licensing',
    caution: 'Do not apply one blanket licence: pre-September 2022 and later collections have different terms.',
    observedUtc: '2026-08-30T00:00:00Z',
    importState: 'NOT_IMPORTED',
  },
] as const;
