/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    "./src/ATLAS.UI/**/*.{razor,html,cshtml,cs}",
    "./src/ATLAS.UI/wwwroot/index.html"
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['JetBrains Mono', 'Menlo', 'Consolas', 'monospace']
      },
      transitionTimingFunction: {
        'atlas': 'cubic-bezier(0.16, 1, 0.3, 1)',
        DEFAULT: 'cubic-bezier(0.16, 1, 0.3, 1)'
      },
      colors: {
        atlas: {
          canvas: '#07090e',
          dock: '#0d111c',
          surface: '#121828',
          surfaceHover: '#1c243c',
          elevated: '#182036',
          floating: '#0f1422',
          border: 'rgba(255, 255, 255, 0.08)',
          borderSubtle: 'rgba(255, 255, 255, 0.04)',
          borderHighlight: 'rgba(255, 255, 255, 0.16)',
          borderTop: 'rgba(255, 255, 255, 0.14)',
          accent: '#8b5cf6',
          accentGlow: '#a78bfa',
          accentIndigo: '#6366f1',
          accentCyan: '#06b6d4',
          accentPink: '#ec4899',
          success: '#10b981',
          warning: '#f59e0b',
          danger: '#f43f5e',
          info: '#38bdf8',
          muted: '#64748b'
        }
      },
      boxShadow: {
        'glow-purple': '0 0 28px rgba(139, 92, 246, 0.35)',
        'glow-emerald': '0 0 22px rgba(16, 185, 129, 0.30)',
        'glow-orange': '0 0 22px rgba(249, 115, 22, 0.30)',
        'glow-cyan': '0 0 22px rgba(6, 182, 212, 0.30)',
        'glow-hero': '0 0 35px rgba(139, 92, 246, 0.50)',
        'surface': '0 12px 28px -6px rgba(0, 0, 0, 0.50)',
        'floating': '0 24px 48px -10px rgba(0, 0, 0, 0.85)'
      },
      borderRadius: {
        'card': '1.125rem',
        'panel': '1.375rem'
      }
    }
  },
  plugins: []
}
