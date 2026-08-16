-- ==============================================================================
-- ATLAS Personal OS — Supabase Database Schema (Hardened RLS & Auth)
-- Ejecutá este script en el SQL Editor de tu proyecto en Supabase para crear
-- todas las tablas necesarias con Row Level Security (RLS) scoped a auth.uid().
-- ==============================================================================

-- 1. NOTAS (Second Brain)
CREATE TABLE IF NOT EXISTS notes (
    id TEXT PRIMARY KEY,
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE DEFAULT auth.uid(),
    title TEXT,
    content TEXT NOT NULL,
    type TEXT NOT NULL DEFAULT 'note',
    tags TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    source TEXT NOT NULL DEFAULT 'quick_capture'
);
CREATE INDEX IF NOT EXISTS idx_notes_user_id ON notes(user_id);
CREATE INDEX IF NOT EXISTS idx_notes_created_at ON notes(created_at DESC);

-- 2. METAS (Goals)
CREATE TABLE IF NOT EXISTS goals (
    id TEXT PRIMARY KEY,
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE DEFAULT auth.uid(),
    title TEXT NOT NULL,
    description TEXT,
    status TEXT NOT NULL DEFAULT 'active',
    progress INTEGER NOT NULL DEFAULT 0,
    target_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_goals_user_id ON goals(user_id);
CREATE INDEX IF NOT EXISTS idx_goals_status ON goals(status);
CREATE INDEX IF NOT EXISTS idx_goals_created_at ON goals(created_at);

-- 3. HÁBITOS (Habits)
CREATE TABLE IF NOT EXISTS habits (
    id TEXT PRIMARY KEY,
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE DEFAULT auth.uid(),
    name TEXT NOT NULL,
    description TEXT,
    frequency TEXT NOT NULL DEFAULT 'daily',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_habits_user_id ON habits(user_id);
CREATE INDEX IF NOT EXISTS idx_habits_created_at ON habits(created_at);

-- 4. EVENTOS DE HÁBITOS (Habit Events)
CREATE TABLE IF NOT EXISTS habit_events (
    id TEXT PRIMARY KEY,
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE DEFAULT auth.uid(),
    habit_id TEXT NOT NULL REFERENCES habits(id) ON DELETE CASCADE,
    completed_at TIMESTAMPTZ NOT NULL,
    note TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_habit_events_user_id ON habit_events(user_id);
CREATE INDEX IF NOT EXISTS idx_habit_events_habit_id ON habit_events(habit_id);
CREATE INDEX IF NOT EXISTS idx_habit_events_completed_at ON habit_events(completed_at);

-- 5. ROADMAPS ESTRATÉGICOS
CREATE TABLE IF NOT EXISTS roadmaps (
    id TEXT PRIMARY KEY,
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE DEFAULT auth.uid(),
    goal_id TEXT REFERENCES goals(id) ON DELETE SET NULL,
    title TEXT NOT NULL,
    description TEXT,
    status TEXT NOT NULL DEFAULT 'active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_roadmaps_user_id ON roadmaps(user_id);
CREATE INDEX IF NOT EXISTS idx_roadmaps_goal_id ON roadmaps(goal_id);
CREATE INDEX IF NOT EXISTS idx_roadmaps_status ON roadmaps(status);

-- 6. HITOS DE ROADMAP (Roadmap Milestones)
CREATE TABLE IF NOT EXISTS roadmap_milestones (
    id TEXT PRIMARY KEY,
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE DEFAULT auth.uid(),
    roadmap_id TEXT NOT NULL REFERENCES roadmaps(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    order_index INTEGER NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS idx_roadmap_milestones_user_id ON roadmap_milestones(user_id);
CREATE INDEX IF NOT EXISTS idx_roadmap_milestones_roadmap_id ON roadmap_milestones(roadmap_id);
CREATE INDEX IF NOT EXISTS idx_roadmap_milestones_order_index ON roadmap_milestones(order_index);

-- 7. TRANSACCIONES FINANCIERAS
CREATE TABLE IF NOT EXISTS transactions (
    id TEXT PRIMARY KEY,
    user_id UUID REFERENCES auth.users(id) ON DELETE CASCADE DEFAULT auth.uid(),
    fecha TIMESTAMPTZ NOT NULL,
    monto NUMERIC NOT NULL,
    tipo TEXT NOT NULL DEFAULT 'expense',
    origen TEXT NOT NULL DEFAULT 'manual',
    descripcion TEXT NOT NULL,
    moneda TEXT NOT NULL DEFAULT 'ARS',
    categoria TEXT,
    subcategoria TEXT,
    id_externo TEXT UNIQUE,
    estado TEXT NOT NULL DEFAULT 'approved',
    metadata TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_transactions_user_id ON transactions(user_id);
CREATE INDEX IF NOT EXISTS idx_transactions_fecha ON transactions(fecha DESC);
CREATE INDEX IF NOT EXISTS idx_transactions_tipo ON transactions(tipo);

-- ==============================================================================
-- ROW LEVEL SECURITY (RLS) POLICIES
-- ==============================================================================

ALTER TABLE notes ENABLE ROW LEVEL SECURITY;
ALTER TABLE goals ENABLE ROW LEVEL SECURITY;
ALTER TABLE habits ENABLE ROW LEVEL SECURITY;
ALTER TABLE habit_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE roadmaps ENABLE ROW LEVEL SECURITY;
ALTER TABLE roadmap_milestones ENABLE ROW LEVEL SECURITY;
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;

-- Eliminar políticas viejas si existieran
DROP POLICY IF EXISTS "Allow anon all on notes" ON notes;
DROP POLICY IF EXISTS "Allow anon all on goals" ON goals;
DROP POLICY IF EXISTS "Allow anon all on habits" ON habits;
DROP POLICY IF EXISTS "Allow anon all on habit_events" ON habit_events;
DROP POLICY IF EXISTS "Allow anon all on roadmaps" ON roadmaps;
DROP POLICY IF EXISTS "Allow anon all on roadmap_milestones" ON roadmap_milestones;
DROP POLICY IF EXISTS "Allow anon all on transactions" ON transactions;

DROP POLICY IF EXISTS "Users can manage their own notes" ON notes;
DROP POLICY IF EXISTS "Users can manage their own goals" ON goals;
DROP POLICY IF EXISTS "Users can manage their own habits" ON habits;
DROP POLICY IF EXISTS "Users can manage their own habit_events" ON habit_events;
DROP POLICY IF EXISTS "Users can manage their own roadmaps" ON roadmaps;
DROP POLICY IF EXISTS "Users can manage their own roadmap_milestones" ON roadmap_milestones;
DROP POLICY IF EXISTS "Users can manage their own transactions" ON transactions;

-- Políticas estrictas para usuarios autenticados (auth.uid() = user_id)
CREATE POLICY "Users can manage their own notes" 
    ON notes FOR ALL TO authenticated 
    USING (auth.uid() = user_id) 
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can manage their own goals" 
    ON goals FOR ALL TO authenticated 
    USING (auth.uid() = user_id) 
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can manage their own habits" 
    ON habits FOR ALL TO authenticated 
    USING (auth.uid() = user_id) 
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can manage their own habit_events" 
    ON habit_events FOR ALL TO authenticated 
    USING (auth.uid() = user_id) 
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can manage their own roadmaps" 
    ON roadmaps FOR ALL TO authenticated 
    USING (auth.uid() = user_id) 
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can manage their own roadmap_milestones" 
    ON roadmap_milestones FOR ALL TO authenticated 
    USING (auth.uid() = user_id) 
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can manage their own transactions" 
    ON transactions FOR ALL TO authenticated 
    USING (auth.uid() = user_id) 
    WITH CHECK (auth.uid() = user_id);

-- Backfill para bases existentes con datos previos
DO $$
DECLARE
    target_user_id UUID;
BEGIN
    SELECT id INTO target_user_id FROM auth.users ORDER BY created_at ASC LIMIT 1;
    IF target_user_id IS NOT NULL THEN
        UPDATE notes SET user_id = target_user_id WHERE user_id IS NULL;
        UPDATE goals SET user_id = target_user_id WHERE user_id IS NULL;
        UPDATE habits SET user_id = target_user_id WHERE user_id IS NULL;
        UPDATE habit_events SET user_id = target_user_id WHERE user_id IS NULL;
        UPDATE roadmaps SET user_id = target_user_id WHERE user_id IS NULL;
        UPDATE roadmap_milestones SET user_id = target_user_id WHERE user_id IS NULL;
        UPDATE transactions SET user_id = target_user_id WHERE user_id IS NULL;
    END IF;
END $$;
