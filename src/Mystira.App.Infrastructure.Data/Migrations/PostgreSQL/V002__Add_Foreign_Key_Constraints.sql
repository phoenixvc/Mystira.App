-- ═══════════════════════════════════════════════════════════════════════════════
-- PostgreSQL Migration Script: Add Foreign Key Constraints
-- Per ADR-0013/0014 Polyglot Persistence Strategy
--
-- Adds referential integrity between polyglot persistence tables.
-- Note: user_profiles remains in Cosmos DB only, so profile_id FKs are not enforced.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══════════════════════════════════════════════════════════════════════════════
-- FOREIGN KEY: game_sessions.account_id -> accounts.id
-- ═══════════════════════════════════════════════════════════════════════════════
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_game_sessions_account'
    ) THEN
        ALTER TABLE game_sessions
        ADD CONSTRAINT fk_game_sessions_account
        FOREIGN KEY (account_id) REFERENCES accounts(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE;
    END IF;
END $$;

COMMENT ON CONSTRAINT fk_game_sessions_account ON game_sessions IS
    'Cascading FK: Deleting an account removes all associated game sessions';

-- ═══════════════════════════════════════════════════════════════════════════════
-- FOREIGN KEY: player_scenario_scores.game_session_id -> game_sessions.id
-- ═══════════════════════════════════════════════════════════════════════════════
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_player_scores_game_session'
    ) THEN
        ALTER TABLE player_scenario_scores
        ADD CONSTRAINT fk_player_scores_game_session
        FOREIGN KEY (game_session_id) REFERENCES game_sessions(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE;
    END IF;
END $$;

COMMENT ON CONSTRAINT fk_player_scores_game_session ON player_scenario_scores IS
    'FK to game_sessions: Set NULL if session deleted (scores remain for analytics)';

-- ═══════════════════════════════════════════════════════════════════════════════
-- NOTE: profile_id FK not enforced
-- user_profiles table remains in Cosmos DB only (document store).
-- The profile_id column is kept for analytics joins but cannot have FK constraint.
-- ═══════════════════════════════════════════════════════════════════════════════

-- Add comment to document the intentional absence of profile_id FK
COMMENT ON COLUMN game_sessions.profile_id IS
    'References user_profiles in Cosmos DB (no FK - cross-store reference)';
COMMENT ON COLUMN player_scenario_scores.profile_id IS
    'References user_profiles in Cosmos DB (no FK - cross-store reference)';
