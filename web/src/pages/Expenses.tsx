import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Category, Expense, Paged } from "../api/types";
import Modal from "../components/Modal";
import ExpenseForm, { type ExpenseFormData } from "../components/ExpenseForm";

export default function Expenses() {
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoriesById, setCategoriesById] = useState<Map<string, string>>(new Map());
  const [error, setError] = useState<string | null>(null);
  const [editingExpense, setEditingExpense] = useState<Expense | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  useEffect(() => {
    loadExpenses();
    loadCategories();
  }, []);

  useEffect(() => {
    const map = new Map<string, string>();
    categories.forEach((c) => map.set(c.id, c.name));
    setCategoriesById(map);
  }, [categories]);

  async function loadExpenses() {
    try {
      const p = await api.get<Paged<Expense>>("/expenses");
      setExpenses(p.items);
      setError(null);
    } catch (e) {
      setError((e as Error).message);
    }
  }

  async function loadCategories() {
    try {
      const list = await api.get<Category[]>("/categories");
      setCategories(list);
    } catch {
      // categories are optional for display
    }
  }

  async function createExpense(data: ExpenseFormData) {
    await api.post("/expenses", data);
    await loadExpenses();
    setShowCreate(false);
  }

  async function updateExpense(data: ExpenseFormData) {
    if (!editingExpense) return;
    await api.put(`/expenses/${editingExpense.id}`, data);
    await loadExpenses();
    setEditingExpense(null);
  }

  async function removeExpense(id: string) {
    if (!window.confirm("Delete this expense?")) return;
    await api.del(`/expenses/${id}`);
    await loadExpenses();
  }

  return (
    <section>
      <div className="page-header">
        <h2>Expenses</h2>
        <button onClick={() => setShowCreate(true)}>Create expense</button>
      </div>

      {error && <p className="error">{error}</p>}

      <Modal title="Create expense" isOpen={showCreate} onClose={() => setShowCreate(false)}>
        <ExpenseForm
          categories={categories}
          submitLabel="Create expense"
          onSubmit={createExpense}
          onCancel={() => setShowCreate(false)}
        />
      </Modal>

      {editingExpense && (
        <Modal
          title="Edit expense"
          isOpen={Boolean(editingExpense)}
          onClose={() => setEditingExpense(null)}
        >
          <ExpenseForm
            expense={editingExpense}
            categories={categories}
            submitLabel="Save changes"
            onSubmit={updateExpense}
            onCancel={() => setEditingExpense(null)}
          />
        </Modal>
      )}

      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Description</th>
            <th>Vendor</th>
            <th>Amount</th>
            <th>Category</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {expenses.map((e) => (
            <tr key={e.id}>
              <td>{e.date}</td>
              <td>{e.description}</td>
              <td>{e.vendor ?? ""}</td>
              <td>{e.amount.toFixed(2)}</td>
              <td>{(e.categoryId && categoriesById.get(e.categoryId)) ?? ""}</td>
              <td className="actions">
                <button onClick={() => setEditingExpense(e)}>Edit</button>
                <button onClick={() => removeExpense(e.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
