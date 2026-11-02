import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5059',
      '/health': 'http://localhost:5059',
      '/uploads': 'http://localhost:5059',
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          maps: ['leaflet', 'react-leaflet'],
          react: ['react', 'react-dom', 'react-router-dom'],
        },
      },
    },
  },
});
