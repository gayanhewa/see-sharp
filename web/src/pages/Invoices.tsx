import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Client, Invoice, Paged } from "../api/types";
import Modal from "../components/Modal";
import InvoiceForm, { type InvoiceFormData } from "../components/InvoiceForm";

export default function Invoices() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [clients, setClients] = useState<Client[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [editingInvoice, setEditingInvoice] = useState<Invoice | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  useEffect(() => {
    loadInvoices();
    loadClients();
  }, []);

  async function loadInvoices() {
    try {
      const p = await api.get<Paged<Invoice>>("/invoices");
      setInvoices(p.items);
      setError(null);
    } catch (e) {
      setError((e as Error).message);
    }
  }

  async function loadClients() {
    try {
      const p = await api.get<Paged<Client>>("/clients");
      setClients(p.items);
    } catch {
      // the invoice page can still show data without clients
    }
  }

  async function createInvoice(data: InvoiceFormData) {
    await api.post("/invoices", {
      clientId: data.clientId,
      number: data.number,
      issueDate: data.issueDate,
      dueDate: data.dueDate,
      notes: data.notes,
      lineItems: data.lineItems,
    });
    await loadInvoices();
    setShowCreate(false);
  }

  async function updateInvoice(data: InvoiceFormData) {
    if (!editingInvoice) return;
    await api.put(`/invoices/${editingInvoice.id}`, {
      number: data.number,
      issueDate: data.issueDate,
      dueDate: data.dueDate,
      notes: data.notes,
      lineItems: data.lineItems,
    });
    await loadInvoices();
    setEditingInvoice(null);
  }

  async function changeStatus(id: string, status: string) {
    await api.post(`/invoices/${id}/status`, { status });
    await loadInvoices();
  }

  async function removeInvoice(id: string) {
    if (!window.confirm("Delete this draft invoice?")) return;
    await api.del(`/invoices/${id}`);
    await loadInvoices();
  }

  return (
    <section>
      <div className="page-header">
        <h2>Invoices</h2>
        <button onClick={() => setShowCreate(true)}>Create invoice</button>
      </div>

      {error && <p className="error">{error}</p>}

      <Modal title="Create invoice" isOpen={showCreate} onClose={() => setShowCreate(false)}>
        <InvoiceForm
          clients={clients}
          submitLabel="Create invoice"
          onSubmit={createInvoice}
          onCancel={() => setShowCreate(false)}
        />
      </Modal>

      {editingInvoice && (
        <Modal
          title="Edit draft invoice"
          isOpen={Boolean(editingInvoice)}
          onClose={() => setEditingInvoice(null)}
        >
          <InvoiceForm
            invoice={editingInvoice}
            clients={clients}
            submitLabel="Save changes"
            onSubmit={updateInvoice}
            onCancel={() => setEditingInvoice(null)}
          />
        </Modal>
      )}

      <table>
        <thead>
          <tr>
            <th>Number</th>
            <th>Status</th>
            <th>Total</th>
            <th>Due</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {invoices.map((i) => (
            <tr key={i.id}>
              <td>{i.number}</td>
              <td>{i.status}</td>
              <td>{i.total.toFixed(2)}</td>
              <td>{i.dueDate}</td>
              <td className="actions">
                {i.status === "Draft" && (
                  <>
                    <button onClick={() => setEditingInvoice(i)}>Edit</button>
                    <button onClick={() => changeStatus(i.id, "sent")}>Send</button>
                    <button onClick={() => changeStatus(i.id, "cancelled")}>Cancel</button>
                    <button onClick={() => removeInvoice(i.id)}>Delete</button>
                  </>
                )}
                {i.status === "Sent" && (
                  <>
                    <button onClick={() => changeStatus(i.id, "paid")}>Pay</button>
                    <button onClick={() => changeStatus(i.id, "overdue")}>Overdue</button>
                    <button onClick={() => changeStatus(i.id, "cancelled")}>Cancel</button>
                  </>
                )}
                {(i.status === "Paid" || i.status === "Overdue" || i.status === "Cancelled") && (
                  <span>—</span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
