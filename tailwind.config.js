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
          canvas: '#090b10',
          dock: '#10141e',
          surface: '#121724',
          surfaceHover: '#181f30',
          elevated: '#171e2e',
          border: 'rgba(255, 255, 255, 0.07)',
          borderSubtle: 'rgba(255, 255, 255, 0.12)',
          accent: '#6366f1',
          accentGlow: '#818cf8',
          accentSubtle: 'rgba(99, 102, 241, 0.12)',
          success: '#10b981',
          warning: '#f59e0b',
          danger: '#f43f5e',
          muted: '#64748b'
        }
      }
    }
  },
  plugins: []
}
