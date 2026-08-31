import React from 'react';
import { createRoot } from 'react-dom/client';
import { TeacherShell } from './TeacherShell';
import ClassroomStudentShell from './ClassroomStudentShell'
import { AppErrorBoundary, RuntimeFailureBanner } from './AppErrorBoundary';
import './print.css';
import './styles.css';

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AppErrorBoundary>
      <RuntimeFailureBanner />
      {window.location.pathname.startsWith('/classroom') ? <ClassroomStudentShell /> : <TeacherShell />}
    </AppErrorBoundary>
  </React.StrictMode>,
);
