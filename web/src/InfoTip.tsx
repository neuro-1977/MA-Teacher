import './info-tip.css';

export function InfoTip({ label, children }: { label: string; children: string }) {
  return <span className="info-tip">
    <button type="button" aria-label={label}>?</button>
    <span role="tooltip">{children}</span>
  </span>;
}
