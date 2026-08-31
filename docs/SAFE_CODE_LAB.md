# Safe Code Lab

MA-Teacher lets learners try small JavaScript ideas without running code on the teacher's computer or classroom server.

## What learners see

- A small editor with a friendly starter example.
- A **Run** button, a **Stop** button and a clear result area.
- A plain explanation that the lab cannot use files, passwords, cameras, microphones, school controls or the internet.
- A two-second limit so an accidental endless loop cannot keep running.

## The security boundary

Each run starts in a new disposable Web Worker. The worker blocks network and storage surfaces before learner code starts, including `fetch`, `XMLHttpRequest`, `WebSocket`, `EventSource`, `importScripts`, nested workers, browser caches and IndexedDB. It has no page DOM, Node.js, operating-system process, filesystem or MA-Teacher authority. Source size and visible output are bounded. Completion, failure, timeout, stopping or leaving the panel terminates the worker.

Learner code is not posted to MA-Teacher and is not written to its database.

## Why this matters

"It runs in a browser" does not automatically mean "it cannot reach the network." Ordinary browser code can send data elsewhere unless those capabilities are removed. Education software often focuses on preventing crashes but overlooks accidental or malicious data transfer. MA-Teacher treats code execution and classroom communication as separate capabilities.

The classroom network is a relay for approved lessons, submissions and teacher feedback. It is never a learner-code execution service.

## Honest limits

This boundary reduces risk; it does not replace current browser and operating-system security updates. JavaScript is the only executable language in the first release. Other languages may be taught using reading, tracing and teacher-reviewed examples, but MA-Teacher must not run them on the teacher host until an independently isolated execution system exists and is proven.
