import { NavLink, Outlet } from 'react-router-dom';
import { API_BASE_URL } from '../api/client';

const links = [
  { to: '/', label: 'Bảng xếp hạng', end: true },
  { to: '/draws', label: 'Kỳ quay', end: false },
  { to: '/strategies', label: 'Chiến lược', end: false },
  { to: '/admin', label: 'Quản trị', end: false },
];

export function Layout() {
  return (
    <div className="app">
      <header className="navbar">
        <div className="navbar-brand">
          Bingo18 <span className="navbar-brand-sub">Sim Dashboard</span>
        </div>
        <nav className="navbar-links">
          {links.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              end={l.end}
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              {l.label}
            </NavLink>
          ))}
        </nav>
        <div className="navbar-api" title="API base URL">
          API: {API_BASE_URL}
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
