import { defineConfig } from '@playwright/test';

export default defineConfig({
  timeout: 30000,
  retries: 2,
  use: {
    baseURL: 'http://api-gateway:8000',
    headless: true,
    screenshot: 'only-on-failure',
    video: 'retry-with-video',
  },
  reporter: [
    ['list'], // Console output
    ['html', { outputFolder: 'test-results/html-report' }], // HTML report
    ['json', { outputFile: 'test-results/report.json' }], // JSON report
    ['junit', { outputFile: 'test-results/report.xml' }], // JUnit XML for CI/CD
  ],
  webServer: {
    command: 'npx playwright test',
    url: 'http://api-gateway:8000/healthz',
    timeout: 60000,
    reuseExistingServer: true,
  },
});
