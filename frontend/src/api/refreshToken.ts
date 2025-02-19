import api from './axiosInstance';

export const refreshAccessToken = async () => {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) {
        throw new Error('No refresh token available');
    }

    try {
        const response = await api.post('/user/refresh-token', { refreshToken });
        localStorage.setItem('token', response.data.accessToken); // Update access token
        localStorage.setItem('refreshToken', response.data.refreshToken); // Update access token

        return response.data.accessToken;
    } catch (error) {
        console.error('Failed to refresh access token:', error);
        throw error;
    }
}; 