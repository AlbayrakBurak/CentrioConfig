import { useState, useEffect } from 'react';
import './App.css';
import ConfigList from './components/ConfigList';
import ConfigModal from './components/ConfigModal';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5079';

function App() {
  const [applicationName, setApplicationName] = useState('SERVICE-A');
  const [configs, setConfigs] = useState([]);
  const [filter, setFilter] = useState('');
  const [includeInactive, setIncludeInactive] = useState(false);
  const [loading, setLoading] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [editingConfig, setEditingConfig] = useState(null);
  const [error, setError] = useState(null);

  const loadConfigs = async () => {
    if (!applicationName) return;
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams();
      if (filter) params.append('name', filter);
      if (includeInactive) params.append('includeInactive', 'true');
      const url = `${API_BASE}/${applicationName}/configs${params.toString() ? `?${params.toString()}` : ''}`;
      const res = await fetch(url);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setConfigs(data);
    } catch (err) {
      setError(err.message);
      console.error('Load error:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadConfigs();
  }, [applicationName, includeInactive]);

  const handleCreate = () => {
    setEditingConfig(null);
    setShowModal(true);
  };

  const handleEdit = (config) => {
    setEditingConfig(config);
    setShowModal(true);
  };

  const handleSave = async (configData) => {
    setError(null);
    try {
      const url = editingConfig
        ? `${API_BASE}/${applicationName}/configs/${editingConfig.id}`
        : `${API_BASE}/${applicationName}/configs`;
      const method = editingConfig ? 'PUT' : 'POST';
      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(configData),
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}: ${await res.text()}`);
      await loadConfigs();
      setShowModal(false);
    } catch (err) {
      setError(err.message);
    }
  };

  const handleToggleActive = async (id, currentActive) => {
    setError(null);
    try {
      const res = await fetch(`${API_BASE}/${applicationName}/configs/${id}/activate?isActive=${!currentActive}`, {
        method: 'PATCH',
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      await loadConfigs();
    } catch (err) {
      setError(err.message);
    }
  };

  const filteredConfigs = filter
    ? configs.filter(c => c.name.toLowerCase().includes(filter.toLowerCase()))
    : configs;

  return (
    <div className="app">
      <header className="app-header">
        <h1>Configuration Admin</h1>
        <div className="app-selector">
          <label>
            Application:
            <select value={applicationName} onChange={e => setApplicationName(e.target.value)}>
              <option value="SERVICE-A">SERVICE-A</option>
              <option value="SERVICE-B">SERVICE-B</option>
            </select>
          </label>
        </div>
      </header>
      <main className="app-main">
        {error && <div className="error-banner">{error}</div>}
        <div className="toolbar">
          <input
            type="text"
            placeholder="Filter by name..."
            value={filter}
            onChange={e => setFilter(e.target.value)}
            className="filter-input"
          />
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={includeInactive}
              onChange={e => setIncludeInactive(e.target.checked)}
            />
            Show inactive
          </label>
          <button onClick={handleCreate} className="btn-primary">+ New Config</button>
          <button onClick={loadConfigs} className="btn-secondary" disabled={loading}>
            {loading ? 'Loading...' : 'Refresh'}
          </button>
        </div>
        <ConfigList
          configs={filteredConfigs}
          onEdit={handleEdit}
          onToggleActive={handleToggleActive}
          loading={loading}
        />
      </main>
      {showModal && (
        <ConfigModal
          config={editingConfig}
          onSave={handleSave}
          onClose={() => setShowModal(false)}
        />
      )}
    </div>
  );
}

export default App;

