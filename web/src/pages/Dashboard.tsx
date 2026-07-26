import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Summary } from "../api/types";

export default function Dashboard() {
  const [summary, setSummary] = useState<Summary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const year = new Date().getFullYear();
    api
      .get<Summary>(`/reports/summary?from=${year}-01-01&to=${year}-12-31`)
      .then(setSummary)
      .catch((e) => setError(e.message));
  }, []);

  if (error) return <p className="error">{error}</p>;
  if (!summary) return <p>Loading...</p>;

  return (
    <section>
      <h2>This year</h2>
      <p>
        Income: {summary.totalIncome.toFixed(2)} | Expenses:{" "}
        {summary.totalExpenses.toFixed(2)} | Net: {summary.net.toFixed(2)}
      </p>
      <table>
        <thead>
          <tr><th>Month</th><th>Income</th><th>Expenses</th><th>Net</th></tr>
        </thead>
        <tbody>
          {summary.months.map((m) => (
            <tr key={`${m.year}-${m.month}`}>
              <td>{m.year}-{String(m.month).padStart(2, "0")}</td>
              <td>{m.income.toFixed(2)}</td>
              <td>{m.expenses.toFixed(2)}</td>
              <td>{m.net.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
