-- ============================================
-- 08_agentflow.sql
-- Multi-Agent Workflow System (Project 5)
-- Additive only — reuses existing e-commerce DB
-- ============================================

CREATE TABLE IF NOT EXISTS agents (
    id           VARCHAR(100) PRIMARY KEY,
    name         VARCHAR(200) NOT NULL,
    type         VARCHAR(50)  NOT NULL,
    description  TEXT,
    config       JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS workflows (
    id           VARCHAR(100) PRIMARY KEY,
    name         VARCHAR(200) NOT NULL,
    description  TEXT,
    definition   JSONB NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS workflow_runs (
    id           UUID PRIMARY KEY,
    workflow_id  VARCHAR(100) NOT NULL REFERENCES workflows(id),
    status       VARCHAR(30)  NOT NULL,
    input        JSONB,
    context      JSONB NOT NULL DEFAULT '{}'::jsonb,
    error        TEXT,
    started_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    finished_at  TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS idx_runs_workflow ON workflow_runs(workflow_id);
CREATE INDEX IF NOT EXISTS idx_runs_status   ON workflow_runs(status);

CREATE TABLE IF NOT EXISTS step_runs (
    id           UUID PRIMARY KEY,
    run_id       UUID NOT NULL REFERENCES workflow_runs(id) ON DELETE CASCADE,
    step_id      VARCHAR(100) NOT NULL,
    name         VARCHAR(200) NOT NULL,
    type         VARCHAR(30)  NOT NULL DEFAULT 'agent',
    status       VARCHAR(30)  NOT NULL,
    attempt      INT NOT NULL DEFAULT 0,
    input        JSONB,
    output       JSONB,
    error        TEXT,
    logs         TEXT,
    started_at   TIMESTAMPTZ,
    finished_at  TIMESTAMPTZ,
    UNIQUE (run_id, step_id)
);
CREATE INDEX IF NOT EXISTS idx_steps_run ON step_runs(run_id);