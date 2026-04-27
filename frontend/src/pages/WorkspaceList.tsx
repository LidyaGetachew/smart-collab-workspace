import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { workspaceService, Workspace } from '../services/workspaceService';
import toast from 'react-hot-toast';

const WorkspaceList: React.FC = () => {
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [newWorkspace, setNewWorkspace] = useState({ name: '', description: '' });
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => { loadWorkspaces(); }, []);

  const loadWorkspaces = async () => {
    try { setWorkspaces(await workspaceService.getAll()); } catch (error) { toast.error('Failed to load workspaces'); } finally { setLoading(false); }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const workspace = await workspaceService.create(newWorkspace);
      setWorkspaces([...workspaces, workspace]);
      setShowModal(false);
      setNewWorkspace({ name: '', description: '' });
      toast.success('Workspace created!');
    } catch (error) { toast.error('Failed to create workspace'); }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm border-b sticky top-0 z-10"><div className="max-w-7xl mx-auto px-4 py-3 flex justify-between items-center"><h1 className="text-xl font-bold text-gray-800">🚀 SmartCollab</h1><div className="flex items-center gap-4"><span className="text-sm text-gray-600">👋 {user?.firstName}</span><button onClick={logout} className="text-red-600 hover:text-red-800 text-sm">Logout</button></div></div></nav>
      <div className="max-w-7xl mx-auto px-4 py-8"><div className="flex justify-between items-center mb-6"><h2 className="text-2xl font-bold">My Workspaces</h2><button onClick={() => setShowModal(true)} className="btn-primary">+ New Workspace</button></div>
        {loading ? <div className="text-center py-12">Loading workspaces...</div> : workspaces.length === 0 ? <div className="text-center py-12 bg-white rounded-xl"><div className="text-6xl mb-4">🏢</div><p className="text-gray-500">No workspaces yet</p><button onClick={() => setShowModal(true)} className="btn-primary mt-4">Create your first workspace</button></div> : <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">{workspaces.map(w => (<div key={w.id} onClick={() => navigate(`/workspace/${w.id}`)} className="bg-white rounded-xl shadow-md p-6 cursor-pointer hover:shadow-lg transition-all hover:scale-[1.02]"><h3 className="text-lg font-semibold mb-2">{w.name}</h3><p className="text-gray-600 text-sm mb-4 line-clamp-2">{w.description || 'No description'}</p><div className="flex justify-between text-sm text-gray-500"><span>👑 {w.ownerName}</span><span>👥 {w.memberCount} members</span><span>📋 {w.taskCount} tasks</span></div></div>))}</div>}
      </div>
      {showModal && (<div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"><div className="bg-white rounded-xl p-6 w-96"><h3 className="text-lg font-bold mb-4">Create Workspace</h3><form onSubmit={handleCreate}><input type="text" placeholder="Workspace Name" required value={newWorkspace.name} onChange={e => setNewWorkspace({ ...newWorkspace, name: e.target.value })} className="input mb-3" /><textarea placeholder="Description (optional)" rows={3} value={newWorkspace.description} onChange={e => setNewWorkspace({ ...newWorkspace, description: e.target.value })} className="input mb-4" /><div className="flex justify-end gap-3"><button type="button" onClick={() => setShowModal(false)} className="btn-secondary">Cancel</button><button type="submit" className="btn-primary">Create</button></div></form></div></div>)}
    </div>
  );
};

export default WorkspaceList;