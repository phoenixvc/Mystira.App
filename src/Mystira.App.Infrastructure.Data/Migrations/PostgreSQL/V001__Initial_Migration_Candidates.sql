-- ═══════════════════════════════════════════════════════════════════════════════
-- PostgreSQL Migration Script: Initial Migration Candidates
-- Per ADR-0013/0014 Polyglot Persistence Strategy
--
-- Migration candidates from Cosmos DB:
--   1. accounts - Transactional, relational, FK target
--   2. game_sessions - ACID required, frequent updates
--   3. player_scenario_scores - Analytical queries, joins needed
-- ═══════════════════════════════════════════════════════════════════════════════

-- Enable UUID extension for proper GUID handling
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ═══════════════════════════════════════════════════════════════════════════════
-- TABLE: accounts
-- ═══════════════════════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS accounts (
    id VARCHAR(36) PRIMARY KEY,
    external_user_id VARCHAR(256),
    email VARCHAR(256) NOT NULL,
    display_name VARCHAR(256),
    role VARCHAR(50) DEFAULT 'Guest',
    user_profile_ids JSONB DEFAULT '[]'::JSONB,
    completed_scenario_ids JSONB DEFAULT '[]'::JSONB,
    subscription JSONB DEFAULT '{}'::JSONB,
    settings JSONB DEFAULT '{}'::JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_login_at TIMESTAMP WITH TIME ZONE
);

-- Indexes for accounts
CREATE UNIQUE INDEX IF NOT EXISTS ix_accounts_email ON accounts(email);
CREATE INDEX IF NOT EXISTS ix_accounts_external_user_id ON accounts(external_user_id);
CREATE INDEX IF NOT EXISTS ix_accounts_created_at ON accounts(created_at);

-- Comments
COMMENT ON TABLE accounts IS 'User accounts migrated from Cosmos DB for transactional operations';
COMMENT ON COLUMN accounts.id IS 'GUID primary key (was Cosmos partition key /id)';
COMMENT ON COLUMN accounts.external_user_id IS 'External identity provider reference (Entra External ID)';
COMMENT ON COLUMN accounts.subscription IS 'JSONB: subscription details (type, tier, purchased scenarios)';
COMMENT ON COLUMN accounts.settings IS 'JSONB: account settings (language, theme, notifications)';

-- ═══════════════════════════════════════════════════════════════════════════════
-- TABLE: game_sessions
-- ═══════════════════════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS game_sessions (
    id VARCHAR(36) PRIMARY KEY,
    scenario_id VARCHAR(36) NOT NULL,
    account_id VARCHAR(36) NOT NULL,
    profile_id VARCHAR(36),
    status VARCHAR(20) DEFAULT 'NotStarted',
    current_scene_id VARCHAR(100),
    player_names JSONB DEFAULT '[]'::JSONB,
    choice_history JSONB DEFAULT '[]'::JSONB,
    echo_history JSONB DEFAULT '[]'::JSONB,
    compass_values JSONB DEFAULT '{}'::JSONB,
    player_compass_progress_totals JSONB DEFAULT '[]'::JSONB,
    achievements JSONB DEFAULT '[]'::JSONB,
    character_assignments JSONB DEFAULT '[]'::JSONB,
    start_time TIMESTAMP WITH TIME ZONE,
    end_time TIMESTAMP WITH TIME ZONE,
    elapsed_time INTERVAL,
    is_paused BOOLEAN DEFAULT FALSE,
    paused_at TIMESTAMP WITH TIME ZONE,
    scene_count INTEGER DEFAULT 0,
    target_age_group VARCHAR(20),
    selected_character_id VARCHAR(100)
);

-- Indexes for game_sessions
CREATE INDEX IF NOT EXISTS ix_game_sessions_account_id ON game_sessions(account_id);
CREATE INDEX IF NOT EXISTS ix_game_sessions_profile_id ON game_sessions(profile_id);
CREATE INDEX IF NOT EXISTS ix_game_sessions_scenario_id ON game_sessions(scenario_id);
CREATE INDEX IF NOT EXISTS ix_game_sessions_status ON game_sessions(status);
CREATE INDEX IF NOT EXISTS ix_game_sessions_start_time ON game_sessions(start_time);
CREATE INDEX IF NOT EXISTS ix_game_sessions_account_status ON game_sessions(account_id, status);

-- Comments
COMMENT ON TABLE game_sessions IS 'Game sessions migrated from Cosmos DB for ACID transactions';
COMMENT ON COLUMN game_sessions.id IS 'GUID primary key';
COMMENT ON COLUMN game_sessions.account_id IS 'FK to accounts (was Cosmos partition key /accountId)';
COMMENT ON COLUMN game_sessions.choice_history IS 'JSONB: array of SessionChoice objects with compass changes';
COMMENT ON COLUMN game_sessions.compass_values IS 'JSONB: dictionary of axis -> CompassTracking';
COMMENT ON COLUMN game_sessions.character_assignments IS 'JSONB: array of SessionCharacterAssignment with nested PlayerAssignment';

-- ═══════════════════════════════════════════════════════════════════════════════
-- TABLE: player_scenario_scores
-- ═══════════════════════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS player_scenario_scores (
    id VARCHAR(36) PRIMARY KEY,
    profile_id VARCHAR(36) NOT NULL,
    scenario_id VARCHAR(36) NOT NULL,
    game_session_id VARCHAR(36),
    axis_scores JSONB DEFAULT '{}'::JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes for player_scenario_scores
CREATE INDEX IF NOT EXISTS ix_player_scenario_scores_profile_id ON player_scenario_scores(profile_id);
CREATE INDEX IF NOT EXISTS ix_player_scenario_scores_scenario_id ON player_scenario_scores(scenario_id);
CREATE UNIQUE INDEX IF NOT EXISTS uix_player_scenario_scores_profile_scenario
    ON player_scenario_scores(profile_id, scenario_id);
CREATE INDEX IF NOT EXISTS ix_player_scenario_scores_created_at ON player_scenario_scores(created_at);

-- Comments
COMMENT ON TABLE player_scenario_scores IS 'Player scores per scenario for analytics and reporting';
COMMENT ON COLUMN player_scenario_scores.profile_id IS 'FK to user_profiles (was Cosmos partition key /profileId)';
COMMENT ON COLUMN player_scenario_scores.axis_scores IS 'JSONB: dictionary of axis -> score (e.g., {"honesty": 15.5})';

-- ═══════════════════════════════════════════════════════════════════════════════
-- MIGRATION TRACKING TABLE (for polyglot sync validation)
-- ═══════════════════════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS _polyglot_sync_log (
    id SERIAL PRIMARY KEY,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(36) NOT NULL,
    operation VARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
    source_backend VARCHAR(20) NOT NULL, -- cosmos, postgres
    sync_status VARCHAR(20) DEFAULT 'pending', -- pending, synced, failed
    cosmos_timestamp TIMESTAMP WITH TIME ZONE,
    postgres_timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    error_message TEXT,
    retry_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_polyglot_sync_log_status ON _polyglot_sync_log(sync_status);
CREATE INDEX IF NOT EXISTS ix_polyglot_sync_log_entity ON _polyglot_sync_log(entity_type, entity_id);

COMMENT ON TABLE _polyglot_sync_log IS 'Tracks dual-write operations for consistency validation';

-- ═══════════════════════════════════════════════════════════════════════════════
-- GRANTS (adjust as needed for your deployment)
-- ═══════════════════════════════════════════════════════════════════════════════
-- GRANT SELECT, INSERT, UPDATE, DELETE ON accounts TO mystira_app;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON game_sessions TO mystira_app;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON player_scenario_scores TO mystira_app;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON _polyglot_sync_log TO mystira_app;
-- GRANT USAGE, SELECT ON SEQUENCE _polyglot_sync_log_id_seq TO mystira_app;
