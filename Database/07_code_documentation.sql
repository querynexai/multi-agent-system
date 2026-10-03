-- ============================================
-- AI Code Documentation Generator Schema
-- Reuses existing e-commerce tables
-- ============================================

CREATE TABLE IF NOT EXISTS analyzed_projects (
    project_id SERIAL PRIMARY KEY,
    project_name VARCHAR(255) NOT NULL,
    project_path TEXT,
    assembly_name VARCHAR(255),
    target_framework VARCHAR(50),
    total_files INT DEFAULT 0,
    total_classes INT DEFAULT 0,
    total_methods INT DEFAULT 0,
    total_properties INT DEFAULT 0,
    documented_members INT DEFAULT 0,
    undocumented_members INT DEFAULT 0,
    analysis_status VARCHAR(50) DEFAULT 'pending', -- pending, analyzing, complete, failed
    analysis_duration_ms INT,
    ai_model VARCHAR(100),
    analyzed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    notes TEXT
);

CREATE INDEX IF NOT EXISTS idx_projects_status ON analyzed_projects(analysis_status);
CREATE INDEX IF NOT EXISTS idx_projects_analyzed ON analyzed_projects(analyzed_at DESC);

-- Generated documentation per member
CREATE TABLE IF NOT EXISTS generated_docs (
    doc_id SERIAL PRIMARY KEY,
    project_id INT REFERENCES analyzed_projects(project_id) ON DELETE CASCADE,
    member_type VARCHAR(50),           -- class, method, property, field, event
    member_name VARCHAR(500) NOT NULL,
    fully_qualified_name TEXT,
    file_path TEXT,
    line_number INT,
    original_xml TEXT,                  -- existing XML doc (if any)
    generated_xml TEXT,                 -- AI-generated XML doc
    ai_confidence DECIMAL(5,4),
    was_accepted BOOLEAN DEFAULT FALSE,
    generated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_docs_project ON generated_docs(project_id);
CREATE INDEX IF NOT EXISTS idx_docs_member ON generated_docs(member_name);

-- Analysis event log (for debugging and audit)
CREATE TABLE IF NOT EXISTS analysis_logs (
    log_id SERIAL PRIMARY KEY,
    project_id INT REFERENCES analyzed_projects(project_id) ON DELETE CASCADE,
    event_type VARCHAR(100),
    message TEXT,
    duration_ms INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_logs_project ON analysis_logs(project_id);

-- README generated per project
CREATE TABLE IF NOT EXISTS project_readmes (
    readme_id SERIAL PRIMARY KEY,
    project_id INT REFERENCES analyzed_projects(project_id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    generated_by VARCHAR(100) DEFAULT 'groq',
    generated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_readmes_project ON project_readmes(project_id);