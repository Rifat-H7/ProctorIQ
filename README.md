# ProctorIQ

ProctorIQ is a competitive exam platform with role-based access, live proctoring, realtime exam events, and certificate generation.

## Features

- Role-based workflows for `Admin`, `Proctor`, and `Candidate`
- JWT authentication with refresh token flow
- Exam and question management (MCQ, True/False, Written)
- Candidate attempt lifecycle: start, resume, save answers, submit
- Realtime exam timer and proctor controls via SignalR
- Candidate proctoring telemetry (tab switch, fullscreen exit, shortcuts, clipboard/context menu events)
- Proctor dashboard with live status, incidents, and evidence CSV export
- Result publishing and downloadable PDF certificates with verification code

## Tech Stack

- Backend: ASP.NET Core Web API (.NET 8), Entity Framework Core, MySQL, SignalR
- Frontend: Angular (standalone components), RxJS
- Auth: ASP.NET Core Identity + JWT + refresh tokens

## Repository Structure

```text
ProctorIQ/
+- ProctorIQ/                 # Main backend API project (active in solution)
+- proctoriq-web/             # Angular frontend
+- ProctorIQ.slnx             # Solution (currently includes ProctorIQ/ProctorIQ.csproj)
+- ProctorIQ.Application/     # Scaffold placeholder (not wired yet)
+- ProctorIQ.Domain/          # Scaffold placeholder (not wired yet)
+- ProctorIQ.Infrastructure/  # Scaffold placeholder (not wired yet)
```

## Core User Flows

### Admin

- Create exams with time window and pass criteria
- Add/update questions (MCQ, True/False, Written)
- Publish exam results

### Candidate

- View available exams
- Start or resume an existing attempt
- Submit MCQ and written answers
- Receive realtime timer, warnings, and session termination events
- Download completion certificate (when eligible)

### Proctor

- Monitor active exams in dashboard
- Track candidate session status and incidents in realtime
- Send warnings and terminate sessions
- Export evidence timeline as CSV

## API Overview

Base URL is configured in frontend environment files:

- Dev: `proctoriq-web/src/environments/environment.ts`
- Prod: `proctoriq-web/src/environments/environment.prod.ts`

Main routes:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /api/exams`
- `GET /api/exams/available`
- `POST /api/exams`
- `POST /api/exams/{examId}/questions`
- `PUT /api/exams/{examId}/questions/{questionId}`
- `GET /api/exams/{examId}/questions`
- `POST /api/attempts/start`
- `PUT /api/attempts/{attemptId}/answer`
- `POST /api/attempts/{attemptId}/submit`
- `GET /api/proctor/exams/active`
- `GET /api/proctor/exams/{examId}/dashboard`
- `GET /api/proctor/exams/{examId}/evidence.csv`
- `GET /api/results/{attemptId}`
- `POST /api/results/{examId}/publish`
- `GET /api/certificates/{attemptId}`

## Realtime Events (SignalR)

Hub endpoint:

- `/hubs/exam`

Examples of realtime messages/events used:

- Candidate channel: `TimerTick`, `ProctorWarning`, `TerminateSession`
- Proctor channel: `CandidateStatusUpdated`, `TabSwitchDetected`, `LockdownSignalDetected`

## Local Development Setup

## Prerequisites

- .NET SDK 8.x
- Node.js 18+ and npm
- MySQL server

## 1. Configure Backend

Update connection string and JWT settings in:

- `ProctorIQ/appsettings.json`
- `ProctorIQ/appsettings.Development.json` (optional for local overrides)

Ensure your MySQL database exists and credentials are correct.

## 2. Apply Database Migrations

From `ProctorIQ/`:

```bash
dotnet ef database update
```

## 3. Run Backend

From `ProctorIQ/`:

```bash
dotnet run
```

Swagger is available in development mode.

## 4. Configure Frontend

Set API base URL in:

- `proctoriq-web/src/environments/environment.ts`

Example:

```ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5277'
};
```

## 5. Run Frontend

From `proctoriq-web/`:

```bash
npm install
npm start
```

Open `http://localhost:4200`.

## Security Notes

- API endpoints are role-protected with `[Authorize]` and role policies.
- Frontend route guards enforce authenticated role-based access.
- Refresh token flow is implemented for session continuity.

## Current Status / Notes

- The active runtime backend is currently the single `ProctorIQ` project.
- Root-level `ProctorIQ.Application`, `ProctorIQ.Domain`, and `ProctorIQ.Infrastructure` folders are scaffold placeholders for future layering.

## License

This repository includes an MIT license file (`LICENSE`).
