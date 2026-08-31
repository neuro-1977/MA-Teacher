import React from 'react';
import { createRoot } from 'react-dom/client';
import { TeacherShell } from './TeacherShell';
import { AppErrorBoundary, RuntimeFailureBanner } from './AppErrorBoundary';
import './print.css';
import './styles.css';

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppErrorBoundary>
      <RuntimeFailureBanner />
      <TeacherShell />
    </AppErrorBoundary>
  </React.StrictMode>,
);
