export const resourceRightsClasses = [
  {
    id: 'official-evidence',
    label: 'Official curriculum evidence',
    defaultAction: 'Capture locally for traceable review; verify the publication terms before redistributing content.',
    evidenceNeeded: ['Authoritative publisher and exact URL', 'Publication title, version, retrieval time, and content hash', 'Stated licence or terms for the exact material', 'Quotation or reproduction scope used by the lesson'],
    permitted: ['Link to the official publication.', 'Store bounded local evidence through the approved capture pipeline.', 'Write an original summary that remains faithful and cites the source.', 'Use short necessary quotations only after checking the applicable terms.'],
    prohibited: ['Assume official or publicly accessible means unrestricted reuse.', 'Republish a complete document through MA-Teacher without explicit rights.', 'Remove provenance, conditions, or version identity.'],
  },
  {
    id: 'open-licensed',
    label: 'Open-licensed resource',
    defaultAction: 'Reuse only under the exact named licence and version, including attribution and adaptation conditions.',
    evidenceNeeded: ['Creator and original source', 'Exact licence name, version, and licence URL', 'Required attribution wording', 'Whether adaptation, commercial use, or share-alike conditions apply'],
    permitted: ['Use within the granted licence scope.', 'Record attribution beside the derivative or resource.', 'Mark modifications clearly.', 'Preserve required notices and share-alike terms.'],
    prohibited: ['Call material open because it is free to view.', 'Drop attribution or licence version.', 'Mix incompatible terms without review.', 'Apply a new licence to rights not owned.'],
  },
  {
    id: 'public-domain',
    label: 'Public-domain material',
    defaultAction: 'Use only after verifying the status for the relevant work, jurisdiction, edition, and included elements.',
    evidenceNeeded: ['Creator, work, publication or creation date', 'Jurisdiction and public-domain basis', 'Edition, translation, illustration, recording, or annotation status', 'Source providing the material'],
    permitted: ['Use the verified public-domain work.', 'Create original teaching adaptations.', 'Retain provenance even where attribution is not legally required.'],
    prohibited: ['Assume an old underlying work makes a modern edition, translation, image, or recording public domain.', 'Remove source history.', 'State a universal legal conclusion from one jurisdiction.'],
  },
  {
    id: 'operator-authored',
    label: 'Operator-authored material',
    defaultAction: 'Use when the operator owns the work or has recorded permission for the intended use.',
    evidenceNeeded: ['Author or rights holder', 'Creation or acquisition provenance', 'Permission scope where ownership is shared or transferred', 'Third-party elements embedded in the material'],
    permitted: ['Store and adapt material within the recorded authority.', 'Link it to reviewed curriculum evidence.', 'Choose a future sharing licence only when all relevant rights are owned.'],
    prohibited: ['Treat compilation as ownership of included third-party content.', 'Erase co-author or source attribution.', 'Redistribute beyond recorded permission.'],
  },
  {
    id: 'learner-created',
    label: 'Learner-created work',
    defaultAction: 'Keep private within the learner record unless a separate explicit authority and safeguarding review permits another use.',
    evidenceNeeded: ['Owning learner record and exact attempt or artifact', 'Purpose for retaining the work', 'Any explicit authority for display, sharing, or reuse', 'Personal, identifying, safeguarding, or third-party content review'],
    permitted: ['Use privately for the learner-specific human review.', 'Retain only as required by the local teaching record.', 'Remove or redact through a future authorized data workflow.'],
    prohibited: ['Use as a public example by default.', 'Treat submission as consent to train, publish, or redistribute.', 'Expose identifying or safeguarding information.', 'Reuse one learner response in another learner record without explicit authority.'],
  },
  {
    id: 'link-only-third-party',
    label: 'Third-party link only',
    defaultAction: 'Store a bounded citation or link when copying rights are not established; the external publisher remains responsible for its content.',
    evidenceNeeded: ['Exact destination URL and publisher', 'Date checked', 'Reason the resource is relevant', 'Age, privacy, account, advertising, and accessibility considerations'],
    permitted: ['Record the citation and a short original description.', 'Require the operator to open and review the current destination.', 'Replace or remove a stale or unsafe link through explicit review.'],
    prohibited: ['Mirror or scrape the content without authority.', 'Claim the external site is endorsed, stable, safe, or accessible forever.', 'Send learner data or credentials to the destination silently.'],
  },
  {
    id: 'unknown-rights',
    label: 'Unknown or conflicting rights',
    defaultAction: 'Refuse copying, adaptation, bundling, training use, and redistribution until rights are established.',
    evidenceNeeded: ['Original creator or publisher', 'Source history', 'Applicable licence or permission', 'Resolution of conflicting notices or ownership claims'],
    permitted: ['Record that the resource was considered and refused.', 'Keep a link for operator investigation only when safe and necessary.', 'Seek clarification from the rights holder outside MA-Teacher.'],
    prohibited: ['Copy because the material is useful, popular, educational, or easily downloaded.', 'Infer permission from silence.', 'Strip watermarks, notices, metadata, or attribution.', 'Use uncertainty as a reason to proceed.'],
  },
] as const;

export type ResourceRightsClass = typeof resourceRightsClasses[number];
export type ResourceRightsClassId = ResourceRightsClass['id'];
