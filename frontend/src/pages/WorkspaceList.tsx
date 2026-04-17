import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { workspaceService, Workspace } from '../services/workspaceService';
import toast from 'react-hot-toast';

const WorkspaceList: React.FC = () => {
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [showModal, setShowModal] = useState(false);
  const [newWorkspace, setNewWorkspace] = useState({ name: '', description: '' });
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => { loadWorkspaces(); }, []);

  const loadWorkspaces = async () => {
    try {
      const data = await workspaceService.getAll();
      setWorkspaces(data);
    } catch (error) {
      toast.error('Failed to load workspaces');
    }
  };

  const handleCreateWorkspace = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const workspace = await workspaceService.create(newWorkspace);
      setWorkspaces([...workspaces, workspace]);
      setShowModal(false);
      setNewWorkspace({ name: '', description: '' });
      toast.success('Workspace created successfully!');
    } catch (error) {
      toast.error('Failed to create workspace');
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-semibold">Smart Collaboration</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-700">Welcome, {user?.firstName}</span>
              <button onClick={logout} className="px-3 py-1 text-sm text-red-600 hover:text-red-800">Logout</button>
            </div>
          </div>
        </div>
      </nav>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold">My Workspaces</h2>
          <button onClick={() => setShowModal(true)} className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
            + New Workspace
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {workspaces.map((workspace) => (
            <div key={workspace.id} onClick={() => navigate(`/workspace/${workspace.id}`)}
              className="bg-white rounded-lg shadow-md p-6 cursor-pointer hover:shadow-lg transition-shadow">
              <h3 className="text-lg font-semibold mb-2">{workspace.name}</h3>
              <p className="text-gray-600 text-sm mb-4">{workspace.description}</p>
              <div className="flex justify-between text-sm text-gray-500">
                <span>Owner: {workspace.ownerName}</span>
                <span>{workspace.memberCount} members</span>
                <span>{workspace.taskCount} tasks</span>
              </div>
            </div>
          ))}
        </div>

        {showModal && (
          <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full">
            <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-lg font-medium">Create Workspace</h3>
                <button onClick={() => setShowModal(false)} className="text-gray-400 hover:text-gray-600">×</button>
              </div>
              <form onSubmit={handleCreateWorkspace}>
                <div className="mb-4">
                  <label className="block text-sm font-medium text-gray-700 mb-2">Name</label>
                  <input type="text" required className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    value={newWorkspace.name} onChange={(e) => setNewWorkspace({ ...newWorkspace, name: e.target.value })} />
                </div>
                <div className="mb-4">
                  <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                  <textarea className="w-full px-3 py-2 border border-gray-300 rounded-md" rows={3}
                    value={newWorkspace.description} onChange={(e) => setNewWorkspace({ ...newWorkspace, description: e.target.value })} />
                </div>
                <div className="flex justify-end space-x-3">
                  <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                  <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default WorkspaceList;