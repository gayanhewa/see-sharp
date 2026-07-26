import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Expense, Paged } from "../api/types";

export default function Expenses() {
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<Paged<Expense>>("/expenses").then((p) => setExpenses(p.items)).catch((e) => setError(e.message));
  }, []);

  if (error) return <p className="error">{error}</p>;

  return (
    <section>
      <h2>Expenses</h2>
      <table>
        <thead><tr><th>Date</th><th>Description</th><th>Vendor</th><th>Amount</th></tr></thead>
        <tbody>
          {expenses.map((e) => (
            <tr key={e.id}>
              <td>{e.date}</td><td>{e.description}</td>
              <td>{e.vendor ?? ""}</td><td>{e.amount.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
