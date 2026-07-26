export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Client {
  id: string;
  name: string;
  email: string | null;
  address: string | null;
  createdAt: string;
}

export interface LineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Invoice {
  id: string;
  clientId: string;
  number: string;
  status: string;
  issueDate: string;
  dueDate: string;
  notes: string | null;
  total: number;
  createdAt: string;
  lineItems: LineItem[];
}

export interface Expense {
  id: string;
  categoryId: string | null;
  description: string;
  amount: number;
  date: string;
  vendor: string | null;
  createdAt: string;
}

export interface Category {
  id: string;
  name: string;
}

export interface MonthlyRow {
  year: number;
  month: number;
  income: number;
  expenses: number;
  net: number;
}

export interface Summary {
  from: string;
  to: string;
  totalIncome: number;
  totalExpenses: number;
  net: number;
  months: MonthlyRow[];
}
