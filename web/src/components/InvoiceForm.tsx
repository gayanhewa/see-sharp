import { useEffect, useMemo, useState } from "react";
import type { Client, Invoice, LineItem } from "../api/types";

export interface InvoiceFormData {
  clientId?: string;
  number: string;
  issueDate: string;
  dueDate: string;
  notes: string | null;
  lineItems: { description: string; quantity: number; unitPrice: number }[];
}

interface InvoiceFormProps {
  invoice?: Invoice;
  clients: Client[];
  onSubmit: (data: InvoiceFormData) => Promise<void>;
  onCancel: () => void;
  submitLabel: string;
}

function today() {
  return new Date().toISOString().slice(0, 10);
}

function parseDateOnly(value?: string | null) {
  return value ? value.slice(0, 10) : "";
}

function emptyItems(count = 1): InvoiceFormData["lineItems"] {
  return Array.from({ length: count }, () => ({ description: "", quantity: 1, unitPrice: 0 }));
}

export default function InvoiceForm({
  invoice,
  clients,
  onSubmit,
  onCancel,
  submitLabel,
}: InvoiceFormProps) {
  const initial: InvoiceFormData = useMemo(
    () => ({
      clientId: invoice?.clientId ?? (clients[0]?.id || ""),
      number: invoice?.number ?? "",
      issueDate: parseDateOnly(invoice?.issueDate) || today(),
      dueDate: parseDateOnly(invoice?.dueDate) || today(),
      notes: invoice?.notes ?? "",
      lineItems:
        invoice?.lineItems.map((li) => ({
          description: li.description,
          quantity: li.quantity,
          unitPrice: li.unitPrice,
        })) ?? emptyItems(1),
    }),
    [clients, invoice]
  );

  const [data, setData] = useState<InvoiceFormData>(initial);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => maybeSetDefaultClient(), [clients.length]);

  function maybeSetDefaultClient() {
    if (!data.clientId && clients.length > 0) {
      setData((d) => ({ ...d, clientId: clients[0].id }));
    }
  }

  function update(patch: Partial<InvoiceFormData>) {
    setData((d) => ({ ...d, ...patch }));
  }

  function updateItem(index: number, patch: Partial<InvoiceFormData["lineItems"][0]>) {
    setData((d) => ({
      ...d,
      lineItems: d.lineItems.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    }));
  }

  function removeItem(index: number) {
    setData((d) => ({
      ...d,
      lineItems: d.lineItems.filter((_, i) => i !== index),
    }));
  }

  function addItem() {
    setData((d) => ({ ...d, lineItems: [...d.lineItems, { description: "", quantity: 1, unitPrice: 0 }] }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    onSubmit(data)
      .then(onCancel)
      .catch(() => setSubmitting(false));
  }

  const isEdit = Boolean(invoice);

  return (
    <form onSubmit={handleSubmit}>
      {!isEdit && (
        <div className="form-row">
          <select
            value={data.clientId}
            onChange={(e) => update({ clientId: e.target.value })}
            required
          >
            {clients.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="form-row">
        <input
          type="text"
          placeholder="Invoice number"
          value={data.number}
          onChange={(e) => update({ number: e.target.value })}
          required
        />
      </div>

      <div className="form-row">
        <input
          type="date"
          value={data.issueDate}
          onChange={(e) => update({ issueDate: e.target.value })}
          required
        />
        <input
          type="date"
          value={data.dueDate}
          onChange={(e) => update({ dueDate: e.target.value })}
          required
        />
      </div>

      <div className="form-row">
        <input
          type="text"
          placeholder="Notes"
          value={data.notes ?? ""}
          onChange={(e) => update({ notes: e.target.value || null })}
        />
      </div>

      <div className="line-items">
        <h4>Line items</h4>
        {data.lineItems.map((item, index) => (
          <div className="form-row" key={index}>
            <input
              type="text"
              placeholder="Description"
              value={item.description}
              onChange={(e) => updateItem(index, { description: e.target.value })}
              required
            />
            <input
              type="number"
              min="1"
              step="1"
              placeholder="Qty"
              value={item.quantity}
              onChange={(e) => updateItem(index, { quantity: Number(e.target.value) || 1 })}
              required
            />
            <input
              type="number"
              min="0"
              step="0.01"
              placeholder="Price"
              value={item.unitPrice}
              onChange={(e) => updateItem(index, { unitPrice: Number(e.target.value) || 0 })}
              required
            />
            <button type="button" onClick={() => removeItem(index)} disabled={data.lineItems.length <= 1}>
              Remove
            </button>
          </div>
        ))}
        <button type="button" onClick={addItem}>
          Add line item
        </button>
      </div>

      {submitting && <p>Saving...</p>}
      <div className="form-actions">
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
        <button type="submit" disabled={submitting}>
          {submitLabel}
        </button>
      </div>
    </form>
  );
}
