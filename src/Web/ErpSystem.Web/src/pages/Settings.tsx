import React, { useState, useEffect } from 'react';
import { User, Bell, Shield, Settings as SettingsIcon, Globe, Clock, DollarSign, Palette, Save } from 'lucide-react';
import { settingsApi, UserPreference } from '../api/settingsApi';

export const Settings = () => {
    const [activeTab, setActiveTab] = useState('profile');
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [formData, setFormData] = useState<UserPreference>({
        userId: 'default-user',
        tenantId: 'default-tenant',
        fullName: '',
        email: '',
        phone: '',
        department: '',
        language: 'en',
        timeZone: 'UTC+8',
        dateFormat: 'YYYY-MM-DD',
        currencyFormat: 'USD',
        theme: 'light',
        emailNotifications: true,
        systemNotifications: true,
        pushNotifications: false,
        notifyOnOrders: true,
        notifyOnInventory: true,
        notifyOnFinance: false,
    });

    useEffect(() => {
        loadPreferences();
    }, []);

    const loadPreferences = async () => {
        try {
            setLoading(true);
            const preferences = await settingsApi.getPreferences();
            setFormData(preferences);
        } catch (error) {
            console.error('Failed to load preferences:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleInputChange = (field: string, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
    };

    const handleSave = async () => {
        try {
            setSaving(true);
            await settingsApi.updatePreferences(formData);
            alert('Settings saved successfully!');
        } catch (error) {
            console.error('Failed to save preferences:', error);
            alert('Failed to save settings. Please try again.');
        } finally {
            setSaving(false);
        }
    };

    const tabs = [
        { id: 'profile', label: 'Profile', icon: User },
        { id: 'preferences', label: 'Preferences', icon: SettingsIcon },
        { id: 'notifications', label: 'Notifications', icon: Bell },
        { id: 'security', label: 'Security', icon: Shield },
    ];

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 font-display">Settings</h1>
                    <p className="text-gray-500">Manage your account settings and preferences</p>
                </div>
                <button
                    onClick={handleSave}
                    disabled={saving}
                    className="bg-blue-600 text-white px-6 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors shadow-sm flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    <Save size={18} />
                    {saving ? 'Saving...' : 'Save Changes'}
                </button>
            </div>

            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
                {/* Tabs */}
                <div className="border-b border-gray-100">
                    <div className="flex">
                        {tabs.map(tab => (
                            <button
                                key={tab.id}
                                onClick={() => setActiveTab(tab.id)}
                                className={`flex items-center gap-2 px-6 py-4 font-medium transition-colors border-b-2 ${activeTab === tab.id
                                    ? 'border-blue-600 text-blue-600 bg-blue-50/50'
                                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                                    }`}
                            >
                                <tab.icon size={18} />
                                {tab.label}
                            </button>
                        ))}
                    </div>
                </div>

                {/* Tab Content */}
                <div className="p-8">
                    {activeTab === 'profile' && <ProfileTab formData={formData} onChange={handleInputChange} />}
                    {activeTab === 'preferences' && <PreferencesTab formData={formData} onChange={handleInputChange} />}
                    {activeTab === 'notifications' && <NotificationsTab formData={formData} onChange={handleInputChange} />}
                    {activeTab === 'security' && <SecurityTab />}
                </div>
            </div>
        </div>
    );
};

const ProfileTab = ({ formData, onChange }: any) => (
    <div className="space-y-6 max-w-2xl">
        <div>
            <h3 className="text-lg font-bold text-gray-900 mb-4">Personal Information</h3>
            <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Full Name</label>
                        <input
                            type="text"
                            value={formData.fullName}
                            onChange={(e) => onChange('fullName', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
                        <input
                            type="email"
                            value={formData.email}
                            onChange={(e) => onChange('email', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        />
                    </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Phone</label>
                        <input
                            type="tel"
                            value={formData.phone}
                            onChange={(e) => onChange('phone', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        />
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Department</label>
                        <input
                            type="text"
                            value={formData.department}
                            onChange={(e) => onChange('department', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        />
                    </div>
                </div>
            </div>
        </div>
    </div>
);

const PreferencesTab = ({ formData, onChange }: any) => (
    <div className="space-y-6 max-w-2xl">
        <div>
            <h3 className="text-lg font-bold text-gray-900 mb-4">Display Preferences</h3>
            <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2 flex items-center gap-2">
                            <Globe size={16} />
                            Language
                        </label>
                        <select
                            value={formData.language}
                            onChange={(e) => onChange('language', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        >
                            <option value="en">English</option>
                            <option value="zh">中文</option>
                            <option value="es">Español</option>
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2 flex items-center gap-2">
                            <Clock size={16} />
                            Timezone
                        </label>
                        <select
                            value={formData.timeZone}
                            onChange={(e) => onChange('timeZone', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        >
                            <option value="UTC+8">UTC+8 (Beijing)</option>
                            <option value="UTC+0">UTC+0 (London)</option>
                            <option value="UTC-5">UTC-5 (New York)</option>
                        </select>
                    </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Date Format</label>
                        <select
                            value={formData.dateFormat}
                            onChange={(e) => onChange('dateFormat', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        >
                            <option value="YYYY-MM-DD">YYYY-MM-DD</option>
                            <option value="DD/MM/YYYY">DD/MM/YYYY</option>
                            <option value="MM/DD/YYYY">MM/DD/YYYY</option>
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2 flex items-center gap-2">
                            <DollarSign size={16} />
                            Currency
                        </label>
                        <select
                            value={formData.currencyFormat}
                            onChange={(e) => onChange('currencyFormat', e.target.value)}
                            className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        >
                            <option value="USD">USD ($)</option>
                            <option value="CNY">CNY (¥)</option>
                            <option value="EUR">EUR (€)</option>
                        </select>
                    </div>
                </div>
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2 flex items-center gap-2">
                        <Palette size={16} />
                        Theme
                    </label>
                    <div className="flex gap-4">
                        {['light', 'dark', 'auto'].map(theme => (
                            <button
                                key={theme}
                                onClick={() => onChange('theme', theme)}
                                className={`px-6 py-3 rounded-lg border-2 font-medium capitalize transition-all ${formData.theme === theme
                                    ? 'border-blue-600 bg-blue-50 text-blue-600'
                                    : 'border-gray-200 text-gray-600 hover:border-gray-300'
                                    }`}
                            >
                                {theme}
                            </button>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    </div>
);

const NotificationsTab = ({ formData, onChange }: any) => (
    <div className="space-y-6 max-w-2xl">
        <div>
            <h3 className="text-lg font-bold text-gray-900 mb-4">Notification Channels</h3>
            <div className="space-y-3">
                <ToggleSwitch
                    label="Email Notifications"
                    description="Receive notifications via email"
                    checked={formData.emailNotifications}
                    onChange={(checked) => onChange('emailNotifications', checked)}
                />
                <ToggleSwitch
                    label="System Notifications"
                    description="Show notifications in the application"
                    checked={formData.systemNotifications}
                    onChange={(checked) => onChange('systemNotifications', checked)}
                />
                <ToggleSwitch
                    label="Push Notifications"
                    description="Receive push notifications on your device"
                    checked={formData.pushNotifications}
                    onChange={(checked) => onChange('pushNotifications', checked)}
                />
            </div>
        </div>
        <div>
            <h3 className="text-lg font-bold text-gray-900 mb-4">Notification Preferences</h3>
            <div className="space-y-3">
                <ToggleSwitch
                    label="Order Updates"
                    description="Notify me about order status changes"
                    checked={formData.notifyOnOrders}
                    onChange={(checked) => onChange('notifyOnOrders', checked)}
                />
                <ToggleSwitch
                    label="Inventory Alerts"
                    description="Notify me about low stock and inventory issues"
                    checked={formData.notifyOnInventory}
                    onChange={(checked) => onChange('notifyOnInventory', checked)}
                />
                <ToggleSwitch
                    label="Financial Reports"
                    description="Notify me about financial reports and updates"
                    checked={formData.notifyOnFinance}
                    onChange={(checked) => onChange('notifyOnFinance', checked)}
                />
            </div>
        </div>
    </div>
);

const SecurityTab = () => (
    <div className="space-y-6 max-w-2xl">
        <div>
            <h3 className="text-lg font-bold text-gray-900 mb-4">Password</h3>
            <div className="space-y-4">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Current Password</label>
                    <input
                        type="password"
                        placeholder="Enter current password"
                        className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                </div>
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">New Password</label>
                    <input
                        type="password"
                        placeholder="Enter new password"
                        className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                </div>
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Confirm New Password</label>
                    <input
                        type="password"
                        placeholder="Confirm new password"
                        className="w-full px-4 py-2 border border-gray-200 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                </div>
                <button className="bg-blue-600 text-white px-6 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors">
                    Update Password
                </button>
            </div>
        </div>
        <div className="pt-6 border-t border-gray-100">
            <h3 className="text-lg font-bold text-gray-900 mb-4">Two-Factor Authentication</h3>
            <p className="text-sm text-gray-600 mb-4">Add an extra layer of security to your account</p>
            <button className="bg-gray-100 text-gray-700 px-6 py-2 rounded-lg font-medium hover:bg-gray-200 transition-colors">
                Enable 2FA
            </button>
        </div>
    </div>
);

const ToggleSwitch = ({ label, description, checked, onChange }: {
    label: string;
    description: string;
    checked: boolean;
    onChange: (checked: boolean) => void;
}) => (
    <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg border border-gray-100">
        <div>
            <p className="font-medium text-gray-900">{label}</p>
            <p className="text-sm text-gray-500">{description}</p>
        </div>
        <button
            onClick={() => onChange(!checked)}
            className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${checked ? 'bg-blue-600' : 'bg-gray-300'
                }`}
        >
            <span
                className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${checked ? 'translate-x-6' : 'translate-x-1'
                    }`}
            />
        </button>
    </div>
);
