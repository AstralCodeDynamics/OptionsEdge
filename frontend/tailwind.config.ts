import type { Config } from 'tailwindcss'

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    screens: {
      md: '768px',
      lg: '1200px',
    },
    extend: {},
  },
  plugins: [],
} satisfies Config
