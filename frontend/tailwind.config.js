/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    './pages/**/*.{js,ts,jsx,tsx,mdx}',
    './components/**/*.{js,ts,jsx,tsx,mdx}',
    './app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        accent: '#7c5cff',
        'accent-hover': '#6b4ce6',
        surface: '#0f111a',
        'surface-card': '#171926',
        'surface-border': '#282b3d',
      },
    },
  },
  plugins: [],
}
