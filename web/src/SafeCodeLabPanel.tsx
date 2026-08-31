import { useEffect, useRef, useState } from "react";
import "./SafeCodeLabPanel.css";

const MAX_SOURCE_LENGTH = 12_000;
const MAX_OUTPUT_LINES = 80;
const MAX_OUTPUT_LENGTH = 12_000;
const RUN_TIMEOUT_MS = 2_000;

const starterCode = `const name = "Ada";
const scores = [4, 7, 9];
const total = scores.reduce((sum, score) => sum + score, 0);

console.log("Hello, " + name + "!");
console.log("Your total is", total);`;

type WorkerMessage =
  | { type: "line"; text: string }
  | { type: "complete" }
  | { type: "error"; text: string };

function buildWorkerSource(): string {
  return `
    "use strict";
    const blocked = (name) => () => { throw new Error(name + " is turned off in the Safe Code Lab."); };
    self.fetch = blocked("Internet access");
    self.XMLHttpRequest = blocked("Internet access");
    self.WebSocket = blocked("Internet access");
    self.EventSource = blocked("Internet access");
    self.importScripts = blocked("Loading outside scripts");
    self.Worker = blocked("Starting another worker");
    self.SharedWorker = blocked("Starting another worker");
    try { self.caches = undefined; } catch {}
    try { self.indexedDB = undefined; } catch {}

    const clean = (value) => {
      if (typeof value === "string") return value;
      if (value === undefined) return "undefined";
      try { return JSON.stringify(value); } catch { return String(value); }
    };

    self.onmessage = (event) => {
      let lines = 0;
      let characters = 0;
      const write = (...values) => {
        if (lines >= ${MAX_OUTPUT_LINES} || characters >= ${MAX_OUTPUT_LENGTH}) return;
        const text = values.map(clean).join(" ").slice(0, ${MAX_OUTPUT_LENGTH} - characters);
        lines += 1;
        characters += text.length;
        self.postMessage({ type: "line", text });
      };
      const console = Object.freeze({ log: write, info: write, warn: write, error: write });
      try {
        const run = new Function("console", "fetch", "XMLHttpRequest", "WebSocket", "EventSource", "Worker", "SharedWorker", "caches", "indexedDB", '"use strict";\\n' + event.data);
        run(console, self.fetch, self.XMLHttpRequest, self.WebSocket, self.EventSource, self.Worker, self.SharedWorker, undefined, undefined);
        self.postMessage({ type: "complete" });
      } catch (error) {
        self.postMessage({ type: "error", text: error instanceof Error ? error.message : String(error) });
      }
    };
  `;
}

export default function SafeCodeLabPanel() {
  const [source, setSource] = useState(starterCode);
  const [output, setOutput] = useState<string[]>([]);
  const [state, setState] = useState<"ready" | "running" | "finished" | "stopped" | "error">("ready");
  const workerRef = useRef<Worker | null>(null);
  const timerRef = useRef<number | null>(null);

  const stopWorker = (nextState: "stopped" | "finished" | "error" = "stopped") => {
    workerRef.current?.terminate();
    workerRef.current = null;
    if (timerRef.current !== null) window.clearTimeout(timerRef.current);
    timerRef.current = null;
    setState(nextState);
  };

  useEffect(() => () => stopWorker("stopped"), []);

  const runCode = () => {
    stopWorker("stopped");
    const code = source.slice(0, MAX_SOURCE_LENGTH);
    setOutput([]);
    setState("running");

    const blob = new Blob([buildWorkerSource()], { type: "text/javascript" });
    const workerUrl = URL.createObjectURL(blob);
    const worker = new Worker(workerUrl);
    URL.revokeObjectURL(workerUrl);
    workerRef.current = worker;

    worker.onmessage = (event: MessageEvent<WorkerMessage>) => {
      const message = event.data;
      if (message.type === "line") {
        setOutput((current) => [...current, message.text].slice(0, MAX_OUTPUT_LINES));
      } else if (message.type === "complete") {
        stopWorker("finished");
      } else {
        setOutput((current) => [...current, `Problem: ${message.text}`].slice(0, MAX_OUTPUT_LINES));
        stopWorker("error");
      }
    };
    worker.onerror = () => {
      setOutput((current) => [...current, "Problem: the code could not finish safely."]);
      stopWorker("error");
    };
    timerRef.current = window.setTimeout(() => {
      setOutput((current) => [...current, "Stopped: this run took too long."]);
      stopWorker("stopped");
    }, RUN_TIMEOUT_MS);
    worker.postMessage(code);
  };

  return (
    <section className="safe-code-lab" aria-labelledby="safe-code-lab-title">
      <div className="safe-code-lab__heading">
        <div>
          <p className="safe-code-lab__eyebrow">Try, change, learn</p>
          <h2 id="safe-code-lab-title">Safe Code Lab</h2>
          <p>Try small JavaScript ideas here. Your code stays in this browser and cannot use the school network.</p>
        </div>
        <span className={`safe-code-lab__state safe-code-lab__state--${state}`}>{state}</span>
      </div>

      <div className="safe-code-lab__safety" role="note">
        <strong>Your safety bubble:</strong> no internet, files, passwords, camera, microphone or school controls.
      </div>

      <div className="safe-code-lab__workspace">
        <label>
          <span>Your JavaScript</span>
          <textarea
            value={source}
            onChange={(event) => setSource(event.target.value.slice(0, MAX_SOURCE_LENGTH))}
            spellCheck={false}
            aria-describedby="safe-code-lab-help"
          />
        </label>
        <div className="safe-code-lab__output" aria-live="polite">
          <span>What happened</span>
          <pre>{output.length ? output.join("\n") : "Press Run when you are ready."}</pre>
        </div>
      </div>

      <p id="safe-code-lab-help" className="safe-code-lab__help">
        Runs stop after two seconds. Other coding languages are explained safely but are never run on the teacher's computer.
      </p>
      <div className="safe-code-lab__actions">
        <button type="button" className="safe-code-lab__run" onClick={runCode} disabled={state === "running"}>Run</button>
        <button type="button" onClick={() => stopWorker("stopped")} disabled={state !== "running"}>Stop</button>
        <button type="button" onClick={() => { stopWorker("stopped"); setOutput([]); }}>Clear result</button>
        <button type="button" onClick={() => { stopWorker("stopped"); setSource(starterCode); setOutput([]); setState("ready"); }}>Start again</button>
      </div>
    </section>
  );
}
