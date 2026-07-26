import { useMemo, useState } from "react";
import type { Category, Expense } from "../api/types";

export interface ExpenseFormData {
  description: string;
  amount: number;
  date: string;
  vendor: string | null;
  categoryId: string | null;
}

interface ExpenseFormProps {
  expense?: Expense;
  categories: Category[];
  onSubmit: (data: ExpenseFormData) => Promise<void>;
  onCancel: () => void;
  submitLabel: string;
}

function today() {
  return new Date().toISOString().slice(0, 10);
}

function parseDateOnly(value?: string | null) {
  return value ? value.slice(0, 10) : "";
}

export default function ExpenseForm({
  expense,
  categories,
  onSubmit,
  onCancel,
  submitLabel,
}: ExpenseFormProps) {
  const initial = useMemo<ExpenseFormData>(
    () => ({
      description: expense?.description ?? "",
      amount: expense?.amount ?? 0,
      date: parseDateOnly(expense?.date) || today(),
      vendor: expense?.vendor ?? "",
      categoryId: expense?.categoryId ?? null,
    }),
    [expense]
  );

  const [data, setData] = useState<ExpenseFormData>(initial);
  const [submitting, setSubmitting] = useState(false);

  function update(patch: Partial<ExpenseFormData>) {
    setData((d) => ({ ...d, ...patch }));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    onSubmit(data)
      .then(onCancel)
      .catch(() => setSubmitting(false));
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="form-row">
        <input
          type="text"
          placeholder="Description"
          value={data.description}
          onChange={(e) => update({ description: e.target.value })}
          required
        />
      </div>

      <div className="form-row">
        <input
          type="number"
          min="0"
          step="0.01"
          placeholder="Amount"
          value={data.amount}
          onChange={(e) => update({ amount: Number(e.target.value) || 0 })}
          required
        />
        <input
          type="date"
          value={data.date}
          onChange={(e) => update({ date: e.target.value })}
          required
        />
      </div>

      <div className="form-row">
        <input
          type="text"
          placeholder="Vendor"
          value={data.vendor ?? ""}
          onChange={(e) => update({ vendor: e.target.value || null })}
        />
        <select
          value={data.categoryId ?? ""}
          onChange={(e) => update({ categoryId: e.target.value || null })}
        >
          <option value="">No category</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
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
