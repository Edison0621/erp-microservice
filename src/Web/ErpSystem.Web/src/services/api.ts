import axios from 'axios';

// Default to Gateway URL or local dev proxy
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api/v1';

export const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Response interceptor for error handling
api.interceptors.response.use(
    (response) => response,
    (error) => {
        console.error('API Error:', error.response?.data || error.message);
        return Promise.reject(error);
    }
);

// Helper Types
export interface PaginatedResult<T> {
    data: T[];
    page: number;
    pageSize: number;
    totalCount?: number;
}
