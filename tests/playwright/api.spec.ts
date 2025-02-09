import { test, expect } from '@playwright/test';

const API_URL = process.env.PLAYWRIGHT_TEST_BASE_URL || 'http://api-gateway:8000/api';

test.describe('User API Tests', () => {
    let email = `testuser${Date.now()}@todoapp.com`;
    test('Should register a new user', async ({ request }) => {
        const response = await request.post(`${API_URL}/user/register`, {
            data: {
                email: email,
                password: 'Password123',
            },
        });

        expect(response.status()).toBe(201);
        const responseBody = await response.json();
        expect(responseBody).toHaveProperty('id');
    });

    test('Should login with valid credentials', async ({ request }) => {
        const response = await request.post(`${API_URL}/user/login`, {
            data: {
                email: email,
                password: 'Password123',
            },
        });

        expect(response.status()).toBe(200);
        const responseBody = await response.json();
        expect(responseBody).toHaveProperty('accessToken');
    });
});
