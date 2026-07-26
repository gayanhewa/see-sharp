import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Client, Paged } from "../api/types";

export default function Clients() {
  const [clients, setClients] = useState<Client[]>([]);
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const load = () =>
    api.get<Paged<Client>>("/clients").then((p) => setClients(p.items)).catch((e) => setError(e.message));

  useEffect(() => { load(); }, []);

  const add = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await api.post("/clients", { name, email: null, address: null });
      setName("");
      await load();
    } catch (err) {
      setError((err as Error).message);
    }
  };

  return (
    <section>
      <h2>Clients</h2>
      <form onSubmit={add}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Client name" />
        <button type="submit">Add</button>
      </form>
      {error && <p className="error">{error}</p>}
      <table>
        <thead><tr><th>Name</th><th>Email</th></tr></thead>
        <tbody>
          {clients.map((c) => (
            <tr key={c.id}><td>{c.name}</td><td>{c.email ?? ""}</td></tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
