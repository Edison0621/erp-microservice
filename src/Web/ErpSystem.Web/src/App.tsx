import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { MainLayout } from './layout/MainLayout';
import { Dashboard } from './pages/Dashboard';
import { Inventory } from './pages/Inventory';
import { Finance } from './pages/Finance';
import { MasterData } from './pages/MasterData';
import { Procurement } from './pages/Procurement';
import { Sales } from './pages/Sales';
import { Production } from './pages/Production';
import { Mrp } from './pages/Mrp';
import { Quality } from './pages/Quality';
import { Automation } from './pages/Automation';
import { Analytics } from './pages/Analytics';
import { Settings } from './pages/Settings';

function App() {
    return (
        <BrowserRouter>
            <MainLayout>
                <Routes>
                    <Route path="/" element={<Dashboard />} />
                    <Route path="/inventory" element={<Inventory />} />
                    <Route path="/finance" element={<Finance />} />
                    <Route path="/master-data" element={<MasterData />} />
                    <Route path="/procurement" element={<Procurement />} />
                    <Route path="/sales" element={<Sales />} />
                    <Route path="/production" element={<Production />} />
                    <Route path="/mrp" element={<Mrp />} />
                    <Route path="/quality" element={<Quality />} />
                    <Route path="/automation" element={<Automation />} />
                    <Route path="/analytics" element={<Analytics />} />
                    <Route path="/settings" element={<Settings />} />
                    <Route path="*" element={<div className="p-8">Page Not Found</div>} />
                </Routes>
            </MainLayout>
        </BrowserRouter>
    );
}

export default App;
