import { Component, type ErrorInfo, type ReactNode, useEffect, useState } from 'react';
import './ui-failure.css';

type BoundaryProps = { children: ReactNode };
type SurfaceBoundaryProps = BoundaryProps & { name: string };
type BoundaryState = { failed: boolean; summary: string };

function boundedSummary(value: unknown, fallback: string) {
  const raw = value instanceof Error ? value.message : typeof value === 'string' ? value : fallback;
  const normalized = raw.replace(/\s+/g, ' ').trim();
  return (normalized || fallback).slice(0, 220);
}

export class AppErrorBoundary extends Component<BoundaryProps, BoundaryState> {
  state: BoundaryState = { failed: false, summary: '' };

  static getDerivedStateFromError(error: unknown): BoundaryState {
    return { failed: true, summary: boundedSummary(error, 'The interface could not render this workspace.') };
  }

  componentDidCatch(_error: unknown, _info: ErrorInfo) {
    // The visible fallback is the evidence channel. Do not emit stacks or send telemetry.
  }

  render() {
    if (!this.state.failed) return this.props.children;

    return <main className="fatal-ui-failure" aria-labelledby="fatal-ui-failure-title">
      <p>INTERFACE STOPPED · NO AUTOMATIC RETRY</p>
      <h1 id="fatal-ui-failure-title">MA-Teacher hit a display failure.</h1>
      <span>{this.state.summary}</span>
      <div className="failure-boundary-note">
        <b>The local database remains the record authority.</b>
        <span>This message does not prove whether an action immediately before the failure committed. Check the relevant record after reloading. Unsaved form text may be lost.</span>
      </div>
      <button type="button" onClick={() => window.location.reload()}>Reload interface</button>
    </main>;
  }
}

export class SurfaceErrorBoundary extends Component<SurfaceBoundaryProps, BoundaryState> {
  state: BoundaryState = { failed: false, summary: '' };

  static getDerivedStateFromError(error: unknown): BoundaryState {
    return { failed: true, summary: boundedSummary(error, 'This workspace surface could not render.') };
  }

  componentDidCatch(_error: unknown, _info: ErrorInfo) {
    // Keep evidence visible and local. Do not emit stacks, telemetry, or automatic retries.
  }

  render() {
    if (!this.state.failed) return this.props.children;

    return <section className="surface-ui-failure" role="alert" aria-label={`${this.props.name} failed to render`}>
      <div>
        <p>SURFACE STOPPED · OTHER WORKSPACE AREAS REMAIN AVAILABLE</p>
        <h2>{this.props.name} could not render.</h2>
        <span>{this.state.summary}</span>
        <small>Check the owning database record before repeating a write. Retrying only remounts this surface.</small>
      </div>
      <button type="button" onClick={() => this.setState({ failed: false, summary: '' })}>Retry this surface</button>
    </section>;
  }
}

export function RuntimeFailureBanner() {
  const [failure, setFailure] = useState('');

  useEffect(() => {
    const onError = (event: ErrorEvent) => setFailure(boundedSummary(event.error ?? event.message, 'A browser operation failed.'));
    const onRejection = (event: PromiseRejectionEvent) => setFailure(boundedSummary(event.reason, 'An asynchronous operation did not complete.'));

    window.addEventListener('error', onError);
    window.addEventListener('unhandledrejection', onRejection);
    return () => {
      window.removeEventListener('error', onError);
      window.removeEventListener('unhandledrejection', onRejection);
    };
  }, []);

  if (!failure) return null;

  return <aside className="runtime-failure-banner" role="alert" aria-labelledby="runtime-failure-title">
    <div>
      <p>UI OPERATION FAILED · NO AUTOMATIC RETRY</p>
      <b id="runtime-failure-title">{failure}</b>
      <span>Check the relevant database-backed record before repeating a write. Dismissing this notice changes no application data.</span>
    </div>
    <button type="button" onClick={() => setFailure('')}>Dismiss notice</button>
  </aside>;
}
