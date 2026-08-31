import { teachingDataProvenanceEntries } from './teaching-data-provenance';
import './teaching-data-provenance.css';

export function TeachingDataProvenancePanel() {
  return (
    <section className="teaching-provenance" id="workspace-teaching-data-provenance" aria-labelledby="teaching-provenance-title">
      <header className="teaching-provenance__header">
        <div>
          <p className="teaching-provenance__eyebrow">Provenance / source evidence</p>
          <h2 id="teaching-provenance-title">What these teaching banks actually prove</h2>
          <p>Trace each current bank to its source and boundary before reusing, reviewing, or extending it.</p>
        </div>
        <span>{teachingDataProvenanceEntries.length} declared assets</span>
      </header>

      <div className="teaching-provenance__warning" role="note">
        Every asset below is source-present and unexecuted. None is accepted curriculum, runtime proof, learner evidence, or permission to teach autonomously.
      </div>

      <div className="teaching-provenance__grid">
        {teachingDataProvenanceEntries.map((entry) => (
          <article key={entry.id} className="teaching-provenance-card">
            <div className="teaching-provenance-card__title">
              <div><p>{entry.contentClass}</p><h3>{entry.title}</h3></div>
              <strong>{entry.evidenceStatus}</strong>
            </div>
            <dl>
              <div><dt>Declared scope</dt><dd>{entry.declaredItems}</dd></div>
              <div><dt>Source</dt><dd><code>{entry.sourcePath}</code></dd></div>
              <div><dt>Boundary</dt><dd><code>{entry.boundaryPath}</code></dd></div>
              <div><dt>Provenance</dt><dd>{entry.author}; crew activity {entry.crewActivity}; crew response {entry.crewResponse}; external assistant {String(entry.externalAssistantUsed)}; external automation {String(entry.externalAutomationUsed)}.</dd></div>
              <div className="teaching-provenance-card__boundary"><dt>Must not imply</dt><dd>{entry.authorityBoundary}</dd></div>
              <div><dt>Next evidence</dt><dd>{entry.nextEvidence}</dd></div>
            </dl>
          </article>
        ))}
      </div>
    </section>
  );
}
