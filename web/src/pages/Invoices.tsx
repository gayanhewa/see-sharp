import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Invoice, Paged } from "../api/types";

export default function Invoices() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<Paged<Invoice>>("/invoices").then((p) => setInvoices(p.items)).catch((e) => setError(e.message));
  }, []);

  if (error) return <p className="error">{error}</p>;

  return (
    <section>
      <h2>Invoices</h2>
      <table>
        <thead><tr><th>Number</th><th>Status</th><th>Total</th><th>Due</th></tr></thead>
        <tbody>
          {invoices.map((i) => (
            <tr key={i.id}>
              <td>{i.number}</td><td>{i.status}</td>
              <td>{i.total.toFixed(2)}</td><td>{i.dueDate}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
