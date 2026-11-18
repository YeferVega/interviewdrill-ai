import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
    extensions: ['.ts', '.js', '.json'],
  },
  plugins: [
    {
      name: 'resolve-ts-from-js',
      resolveId(id, importer) {
        // Resolve .js imports to .ts files for Vitest
        if (id.endsWith('.js') && importer) {
          return id.replace(/\.js$/, '.ts');
        }
      },
    },
  ],
  test: {
    globals: true,
    environment: 'node',
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'dist/',
        'tests/',
        '**/*.test.ts',
        '**/*.config.ts'
      ]
    },
    include: ['tests/**/*.test.ts'],
    exclude: ['node_modules', 'dist']
  }
});
