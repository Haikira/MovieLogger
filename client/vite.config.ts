import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  // In development the Vite dev server proxies /api to the ASP.NET Core API, so the browser
  // talks to a single origin and the API needs no CORS configuration.
  const apiProxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5021';

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': { target: apiProxyTarget, changeOrigin: true },
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      css: { modules: { classNameStrategy: 'non-scoped' } },
      restoreMocks: true,
      unstubGlobals: true,
    },
  };
});
