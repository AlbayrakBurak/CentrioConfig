const ConfigList = ({ configs, onEdit, onToggleActive, loading }) => {
  const getTypeName = (type) => {
    const names = { 0: 'String', 1: 'Int', 2: 'Double', 3: 'Bool' };
    return names[type] || 'Unknown';
  };

  if (loading && configs.length === 0) {
    return <div className="loading">Loading...</div>;
  }

  if (configs.length === 0) {
    return <div className="empty-state">No configurations found</div>;
  }

  return (
    <div className="config-table-wrapper">
      <table className="config-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Type</th>
            <th>Value</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {configs.map((config) => (
            <tr key={config.id} className={config.isActive ? '' : 'inactive'}>
              <td>{config.name}</td>
              <td>{getTypeName(config.type)}</td>
              <td className="value-cell">{config.value}</td>
              <td>
                <span className={`status-badge ${config.isActive ? 'active' : 'inactive'}`}>
                  {config.isActive ? '✓' : '✗'}
                </span>
              </td>
              <td>
                <button onClick={() => onEdit(config)} className="btn-small">Edit</button>
                <button
                  onClick={() => onToggleActive(config.id, config.isActive)}
                  className="btn-small"
                >
                  {config.isActive ? 'Deactivate' : 'Activate'}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default ConfigList;

