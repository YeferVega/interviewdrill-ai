import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// El proxy de /api apunta a la API local. El default coincide con scripts/run-local.ps1
// y con la documentación (API en http://127.0.0.1:5210). Se puede sobrescribir con
// VITE_API_PROXY_TARGET si corres la API en otro puerto.
const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://127.0.0.1:5210'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
      },
      '/hubs': {
        target: apiTarget,
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
