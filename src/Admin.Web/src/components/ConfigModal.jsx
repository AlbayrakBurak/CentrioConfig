import { useState, useEffect } from 'react';

const ConfigModal = ({ config, onSave, onClose }) => {
  const [formData, setFormData] = useState({
    name: '',
    type: 0,
    value: '',
    isActive: true,
  });

  useEffect(() => {
    if (config) {
      setFormData({
        name: config.name || '',
        type: config.type ?? 0,
        value: config.value || '',
        isActive: config.isActive ?? true,
      });
    }
  }, [config]);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!formData.name.trim()) {
      alert('Name is required');
      return;
    }
    onSave(formData);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{config ? 'Edit Configuration' : 'New Configuration'}</h2>
          <button className="modal-close" onClick={onClose}>×</button>
        </div>
        <form onSubmit={handleSubmit} className="modal-form">
          <div className="form-group">
            <label>Name *</label>
            <input
              type="text"
              value={formData.name}
              onChange={e => setFormData({ ...formData, name: e.target.value })}
              required
              disabled={!!config}
            />
          </div>
          <div className="form-group">
            <label>Type *</label>
            <select
              value={formData.type}
              onChange={e => setFormData({ ...formData, type: parseInt(e.target.value) })}
            >
              <option value={0}>String</option>
              <option value={1}>Int</option>
              <option value={2}>Double</option>
              <option value={3}>Bool</option>
            </select>
          </div>
          <div className="form-group">
            <label>Value *</label>
            <input
              type="text"
              value={formData.value}
              onChange={e => setFormData({ ...formData, value: e.target.value })}
              required
            />
          </div>
          <div className="form-group">
            <label>
              <input
                type="checkbox"
                checked={formData.isActive}
                onChange={e => setFormData({ ...formData, isActive: e.target.checked })}
              />
              Active
            </label>
          </div>
          <div className="modal-actions">
            <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
            <button type="submit" className="btn-primary">Save</button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ConfigModal;

