import { NavLink, Outlet } from "react-router-dom";

export default function App() {
  return (
    <div className="layout">
      <header>
        <h1>See Sharp</h1>
        <nav>
          <NavLink to="/">Dashboard</NavLink>
          <NavLink to="/clients">Clients</NavLink>
          <NavLink to="/invoices">Invoices</NavLink>
          <NavLink to="/expenses">Expenses</NavLink>
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
