export type CurriculumJurisdiction = 'England' | 'Wales' | 'Scotland' | 'Northern Ireland';
export type CurriculumCandidateState = 'Pending governed review' | 'Consultation only' | 'Research incomplete';
export type CurriculumResearchDate = `${number}-${number}-${number}`;

export interface CurriculumReferenceCandidate {
  id: string;
  jurisdiction: CurriculumJurisdiction;
  title: string;
  publisher: string;
  url: string;
  scope: string;
  state: CurriculumCandidateState;
  caution: string;
  researchedOn: CurriculumResearchDate;
}

export const curriculumReferenceCandidates: CurriculumReferenceCandidate[] = [
  {
    id: 'curriculum-england-eyfs-2026-transition', jurisdiction: 'England', title: 'Early years foundation stage statutory framework', publisher: 'Department for Education / GOV.UK',
    url: 'https://www.gov.uk/government/publications/early-years-foundation-stage-framework--2', scope: 'Birth to five; separate provider documents before and from 1 September 2026.', state: 'Pending governed review',
    caution: 'Resolve provider type and effective date; do not treat superseded and future-effective documents as interchangeable.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-england-national-ks1-4-framework', jurisdiction: 'England', title: 'National curriculum in England: framework for key stages 1 to 4', publisher: 'Department for Education / GOV.UK',
    url: 'https://www.gov.uk/government/publications/national-curriculum-in-england-framework-for-key-stages-1-to-4/the-national-curriculum-in-england-framework-for-key-stages-1-to-4', scope: 'Key Stages 1-4, twelve subjects and programme-of-study links.', state: 'Pending governed review',
    caution: 'Preserve school-type applicability and statutory versus non-statutory content.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-england-functional-skills-english', jurisdiction: 'England', title: 'Functional Skills subject content: English', publisher: 'Department for Education / GOV.UK',
    url: 'https://www.gov.uk/government/publications/functional-skills-subject-content-english', scope: 'Entry Levels 1-3 and Levels 1-2 for English Functional Skills.', state: 'Pending governed review',
    caution: 'Qualification content is not a generic adult curriculum or a school-key-stage mapping.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-england-functional-skills-mathematics', jurisdiction: 'England', title: 'Maths Functional Skills: subject content', publisher: 'Department for Education / GOV.UK',
    url: 'https://www.gov.uk/government/publications/functional-skills-subject-content-mathematics/subject-content-functional-skills-maths', scope: 'Entry Levels 1-3 and Levels 1-2 for mathematics Functional Skills.', state: 'Pending governed review',
    caution: 'Qualification level must not be equated mechanically with age or school key stage.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-england-functional-skills-digital', jurisdiction: 'England', title: 'Digital Functional Skills: subject content', publisher: 'Department for Education / GOV.UK',
    url: 'https://www.gov.uk/government/publications/digital-functional-skills-qualifications/digital-functional-skills-qualifications-subject-content', scope: 'Entry and Level 1 digital Functional Skills.', state: 'Pending governed review',
    caution: 'Not interchangeable with school computing or a software-development curriculum.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-wales-3-16-framework', jurisdiction: 'Wales', title: 'Curriculum for Wales', publisher: 'Welsh Government / Hwb',
    url: 'https://hwb.gov.wales/curriculum-for-wales', scope: 'Three-to-sixteen continuum and six Areas of Learning and Experience.', state: 'Pending governed review',
    caution: 'Do not translate Areas directly into England subjects; preserve progression and Welsh-language context.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-wales-14-16-guidance-2026', jurisdiction: 'Wales', title: '14 to 16 learning guidance', publisher: 'Welsh Government / Hwb',
    url: 'https://hwb.gov.wales/curriculum-for-wales/14-to-16-learning-guidance/', scope: 'Current Years 10-11 guidance within all six Areas.', state: 'Pending governed review',
    caution: 'Version separately from the wider framework because this guidance is actively time-sensitive.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-wales-cross-curricular-skills', jurisdiction: 'Wales', title: 'Cross-curricular skills frameworks', publisher: 'Welsh Government / Hwb',
    url: 'https://hwb.gov.wales/curriculum-for-wales/cross-curricular-skills-frameworks/', scope: 'Literacy, numeracy and digital competence across learning.', state: 'Pending governed review',
    caution: 'Cross-curricular status must not be rewritten as an independent subject sequence.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-wales-adult-learning-intake-unresolved', jurisdiction: 'Wales', title: 'Adult and community learning', publisher: 'Welsh Government',
    url: 'https://www.gov.wales/adult-and-community-learning', scope: 'Adult/community policy and review entry point.', state: 'Research incomplete',
    caution: 'No single canonical adult curriculum was established in this pass; do not ingest as curriculum authority.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-scotland-cfe-broad-general-education', jurisdiction: 'Scotland', title: 'Curriculum for Excellence: Broad General Education', publisher: 'Education Scotland',
    url: 'https://education.gov.scot/curriculum-for-excellence/about-curriculum-for-excellence/curriculum-stages/broad-general-education/', scope: 'Early learning through S3, eight curriculum areas, experiences/outcomes and benchmarks.', state: 'Pending governed review',
    caution: 'Do not map Curriculum for Excellence levels directly to England key stages.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-scotland-cfe-levels-and-senior-phase', jurisdiction: 'Scotland', title: 'Curriculum levels', publisher: 'Education Scotland',
    url: 'https://education.gov.scot/parentzone/curriculum-in-scotland/curriculum-levels/', scope: 'Early through Fourth levels and Senior Phase S4-S6.', state: 'Pending governed review',
    caution: 'Age associations are a general guide; do not turn levels into fixed age or readiness labels.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-scotland-adult-literacies', jurisdiction: 'Scotland', title: "Scotland's Adult Literacies Curriculum Guidelines", publisher: 'Education Scotland',
    url: 'https://education.gov.scot/media/s1tnlee5/adult-literacies-curriculum-framework.pdf', scope: 'Learner-centred adult literacy learning, teaching and assessment.', state: 'Pending governed review',
    caution: 'Review currency, accessibility, licensing and supersession before statement-level extraction.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-northern-ireland-current-statutory', jurisdiction: 'Northern Ireland', title: 'Statutory curriculum', publisher: 'Department of Education, Northern Ireland',
    url: 'https://www.education-ni.gov.uk/articles/statutory-curriculum', scope: 'Foundation Stage and Key Stages 1-4 across compulsory education.', state: 'Pending governed review',
    caution: 'Preserve Northern Ireland year, stage, Area of Learning and cross-curricular terminology.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-northern-ireland-2026-consultation', jurisdiction: 'Northern Ireland', title: 'Subject Frameworks and Key Stage Documents', publisher: 'Department of Education, Northern Ireland',
    url: 'https://www.education-ni.gov.uk/publications/subject-frameworks-and-key-stage-documents', scope: 'Curriculum consultation documents published 16 June 2026.', state: 'Consultation only',
    caution: 'Proposal material must not overwrite or masquerade as the current statutory curriculum.', researchedOn: '2026-08-30',
  },
  {
    id: 'curriculum-northern-ireland-post16-entitlement', jurisdiction: 'Northern Ireland', title: 'Entitlement Framework', publisher: 'Department of Education, Northern Ireland',
    url: 'https://www.education-ni.gov.uk/articles/entitlement-framework', scope: 'Minimum course range at Key Stage 4 and post-16.', state: 'Pending governed review',
    caution: 'Course entitlement is not subject-content curriculum.', researchedOn: '2026-08-30',
  },
];
