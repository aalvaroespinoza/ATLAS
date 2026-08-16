-- ==============================================================================
-- ATLAS Personal OS — Supabase Database Schema
-- Ejecutá este script en el SQL Editor de tu proyecto en Supabase para crear
-- todas las tablas necesarias con compatibilidad total para ATLAS.
-- ==============================================================================

-- 1. NOTAS (Second Brain)
CREATE TABLE IF NOT EXISTS notes (
    id TEXT PRIMARY KEY,
    title TEXT,
    content TEXT NOT NULL,
    type TEXT NOT NULL DEFAULT 'note',
    tags TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    source TEXT NOT NULL DEFAULT 'quick_capture'
);
CREATE INDEX IF NOT EXISTS idx_notes_created_at ON notes(created_at DESC);

-- 2. METAS (Goals)
CREATE TABLE IF NOT EXISTS goals (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    description TEXT,
    status TEXT NOT NULL DEFAULT 'active',
    progress INTEGER NOT NULL DEFAULT 0,
    target_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_goals_status ON goals(status);
CREATE INDEX IF NOT EXISTS idx_goals_created_at ON goals(created_at);

-- 3. HÁBITOS (Habits)
CREATE TABLE IF NOT EXISTS habits (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    frequency TEXT NOT NULL DEFAULT 'daily',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_habits_created_at ON habits(created_at);

-- 4. EVENTOS DE HÁBITOS (Habit Events)
CREATE TABLE IF NOT EXISTS habit_events (
    id TEXT PRIMARY KEY,
    habit_id TEXT NOT NULL REFERENCES habits(id) ON DELETE CASCADE,
    completed_at TIMESTAMPTZ NOT NULL,
    note TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_habit_events_habit_id ON habit_events(habit_id);
CREATE INDEX IF NOT EXISTS idx_habit_events_completed_at ON habit_events(completed_at);

-- 5. ROADMAPS ESTRATÉGICOS
CREATE TABLE IF NOT EXISTS roadmaps (
    id TEXT PRIMARY KEY,
    goal_id TEXT REFERENCES goals(id) ON DELETE SET NULL,
    title TEXT NOT NULL,
    description TEXT,
    status TEXT NOT NULL DEFAULT 'active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_roadmaps_goal_id ON roadmaps(goal_id);
CREATE INDEX IF NOT EXISTS idx_roadmaps_status ON roadmaps(status);

-- 6. HITOS DE ROADMAP (Roadmap Milestones)
CREATE TABLE IF NOT EXISTS roadmap_milestones (
    id TEXT PRIMARY KEY,
    roadmap_id TEXT NOT NULL REFERENCES roadmaps(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    order_index INTEGER NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS idx_roadmap_milestones_roadmap_id ON roadmap_milestones(roadmap_id);
CREATE INDEX IF NOT EXISTS idx_roadmap_milestones_order_index ON roadmap_milestones(order_index);

-- 7. TRANSACCIONES FINANCIERAS
CREATE TABLE IF NOT EXISTS transactions (
    id TEXT PRIMARY KEY,
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
CREATE INDEX IF NOT EXISTS idx_transactions_fecha ON transactions(fecha DESC);
CREATE INDEX IF NOT EXISTS idx_transactions_tipo ON transactions(tipo);

-- Habilitar Row Level Security (RLS) opcionalmente para anon key
ALTER TABLE notes ENABLE ROW LEVEL SECURITY;
ALTER TABLE goals ENABLE ROW LEVEL SECURITY;
ALTER TABLE habits ENABLE ROW LEVEL SECURITY;
ALTER TABLE habit_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE roadmaps ENABLE ROW LEVEL SECURITY;
ALTER TABLE roadmap_milestones ENABLE ROW LEVEL SECURITY;
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;

-- Políticas de acceso para API anon key
CREATE POLICY "Allow anon all on notes" ON notes FOR ALL USING (true) WITH CHECK (true);
CREATE POLICY "Allow anon all on goals" ON goals FOR ALL USING (true) WITH CHECK (true);
CREATE POLICY "Allow anon all on habits" ON habits FOR ALL USING (true) WITH CHECK (true);
CREATE POLICY "Allow anon all on habit_events" ON habit_events FOR ALL USING (true) WITH CHECK (true);
CREATE POLICY "Allow anon all on roadmaps" ON roadmaps FOR ALL USING (true) WITH CHECK (true);
CREATE POLICY "Allow anon all on roadmap_milestones" ON roadmap_milestones FOR ALL USING (true) WITH CHECK (true);
CREATE POLICY "Allow anon all on transactions" ON transactions FOR ALL USING (true) WITH CHECK (true);
