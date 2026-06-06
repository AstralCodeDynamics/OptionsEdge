import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    port: 5173,
  },
  build: {
    rollupOptions: {
      onwarn(warning, defaultHandler) {
        // Suppress INVALID_ANNOTATION warnings from @microsoft/signalr's ESM bundle
        if (warning.code === 'INVALID_ANNOTATION') return
        defaultHandler(warning)
      },
    },
  },
})
