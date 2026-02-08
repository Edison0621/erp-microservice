import axios from 'axios';

const API_BASE_URL = '/api/v1';

export interface UserPreference {
    id?: string;
    userId: string;
    tenantId: string;
    language: string;
    timeZone: string;
    dateFormat: string;
    currencyFormat: string;
    theme: string;
    fullName: string;
    email: string;
    phone: string;
    department: string;
    emailNotifications: boolean;
    systemNotifications: boolean;
    pushNotifications: boolean;
    notifyOnOrders: boolean;
    notifyOnInventory: boolean;
    notifyOnFinance: boolean;
}

export const settingsApi = {
    getPreferences: async (userId: string = 'default-user'): Promise<UserPreference> => {
        const response = await axios.get(`${API_BASE_URL}/settings/preferences?userId=${userId}`);
        return response.data;
    },

    updatePreferences: async (preferences: UserPreference): Promise<UserPreference> => {
        const response = await axios.put(`${API_BASE_URL}/settings/preferences`, preferences);
        return response.data;
    },

    resetPreferences: async (userId: string = 'default-user'): Promise<UserPreference> => {
        const response = await axios.post(`${API_BASE_URL}/settings/preferences/reset?userId=${userId}`);
        return response.data;
    },
};
