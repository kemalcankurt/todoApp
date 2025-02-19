import axios, { InternalAxiosRequestConfig } from 'axios';
import { refreshAccessToken } from './refreshToken';
import { toast } from "react-toastify";


const api = axios.create({
    baseURL: 'api',
    headers: {
        'Content-Type': 'application/json'
    },
    withCredentials: true
});

// Add request interceptor for auth token
api.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// Add response interceptor to handle token refresh
api.interceptors.response.use(
    response => response,
    async error => {
        const originalRequest = error.config;

        // Check if the error is due to an expired token
        if (error.response.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true; // Prevent infinite loop
            try {
                const newToken = await refreshAccessToken();
                originalRequest.headers['Authorization'] = 'Bearer ' + newToken;
                return api(originalRequest); // Retry the original request
            } catch (refreshError) {
                console.error('Refresh token failed:', refreshError);
                toast.warn("Your session might have expired. Click here to log in.", {
                    position: "top-right",
                    autoClose: false,
                    closeOnClick: true,
                    draggable: true,
                    onClick: () => {
                        localStorage.removeItem("token");
                        localStorage.removeItem("refreshToken");
                        window.location.href = "/login";
                    },
                });

                return Promise.reject(refreshError);
            }
        }

        return Promise.reject(error);
    }
);

export default api; 