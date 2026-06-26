import { HashRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Leaderboard } from './pages/Leaderboard';
import { UserDetail } from './pages/UserDetail';
import { Draws } from './pages/Draws';
import { Strategies } from './pages/Strategies';
import { Admin } from './pages/Admin';

export default function App() {
  return (
    <HashRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<Leaderboard />} />
          <Route path="users/:id" element={<UserDetail />} />
          <Route path="draws" element={<Draws />} />
          <Route path="strategies" element={<Strategies />} />
          <Route path="admin" element={<Admin />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </HashRouter>
  );
}
