import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { workspaceService, WorkspaceMember } from '../services/workspaceService';
import { taskService, Task } from '../services/taskService';
import { fileService, FileItem } from '../services/fileService';
import KanbanBoard from '../components/KanbanBoard';
import toast from 'react-hot-toast';

const WorkspaceDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [members, setMembers] = useState<WorkspaceMember[]>([]);
  const [files, setFiles] = useState<FileItem[]>([]);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Member');

  useEffect(() => {
    if (id) {
      loadMembers();
      loadFiles();
    }
  }, [id]);

  const loadMembers = async () => {
    if (!id) return;
    try {
      const data = await workspaceService.getMembers(id);
      setMembers(data);
    } catch (error) {
      toast.error('Failed to load members');
    }
  };

  const loadFiles = async () => {
    if (!id) return;
    try {
      const data = await fileService.getByWorkspace(id);
      setFiles(data);
    } catch (error) {
      toast.error('Failed to load files');
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;
    try {
      await workspaceService.inviteMember(id, inviteEmail, inviteRole);
      toast.success('Member invited successfully!');
      setShowInviteModal(false);
      setInviteEmail('');
      loadMembers();
    } catch (error) {
      toast.error('Failed to invite member');
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !id) return;
    try {
      await fileService.upload(id, file);
      toast.success('File uploaded successfully!');
      loadFiles();
    } catch (error) {
      toast.error('Failed to upload file');
    }
  };

  const handleFileDownload = async (fileId: string) => {
    if (!id) return;
    try {
      await fileService.download(id, fileId);
    } catch (error) {
      toast.error('Failed to download file');
    }
  };

  const handleFileDelete = async (fileId: string) => {
    if (!id) return;
    try {
      await fileService.delete(id, fileId);
      toast.success('File deleted successfully!');
      loadFiles();
    } catch (error) {
      toast.error('Failed to delete file');
    }
  };

  const isAdmin = members.find(m => m.userId === user?.id)?.role === 'Admin';

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center space-x-4">
              <button onClick={() => navigate('/workspaces')} className="text-blue-600 hover:text-blue-800">← Back</button>
              <h1 className="text-xl font-semibold">Workspace</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-700">{user?.firstName}</span>
            </div>
          </div>
        </div>
      </nav>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Tabs */}
        <div className="border-b border-gray-200 mb-6">
          <nav className="-mb-px flex space-x-8">
            <button className="border-blue-500 text-blue-600 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
              Kanban Board
            </button>
            <button className="border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
              Files
            </button>
            <button className="border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
              Members
            </button>
          </nav>
        </div>

        {/* Kanban Board */}
        {id && <KanbanBoard workspaceId={id} isAdmin={isAdmin} />}

        {/* Files Section - Simplified */}
        <div className="mt-8">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-medium">Files</h3>
            <label className="cursor-pointer px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700">
              Upload File
              <input type="file" onChange={handleFileUpload} className="hidden" />
            </label>
          </div>
          <div className="bg-white rounded-lg shadow">
            {files.map(file => (
              <div key={file.id} className="flex justify-between items-center p-4 border-b">
                <div>
                  <p className="font-medium">{file.fileName}</p>
                  <p className="text-sm text-gray-500">{file.formattedFileSize} • Uploaded by {file.uploadedByName}</p>
                </div>
                <div className="space-x-2">
                  <button onClick={() => handleFileDownload(file.id)} className="text-blue-600 hover:text-blue-800">Download</button>
                  {isAdmin && (
                    <button onClick={() => handleFileDelete(file.id)} className="text-red-600 hover:text-red-800">Delete</button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Invite Modal */}
        {showInviteModal && (
          <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full">
            <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-lg font-medium">Invite Member</h3>
                <button onClick={() => setShowInviteModal(false)} className="text-gray-400 hover:text-gray-600">×</button>
              </div>
              <form onSubmit={handleInvite}>
                <div className="mb-4">
                  <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
                  <input type="email" required className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} />
                </div>
                <div className="mb-4">
                  <label className="block text-sm font-medium text-gray-700 mb-2">Role</label>
                  <select className="w-full px-3 py-2 border border-gray-300 rounded-md"
                    value={inviteRole} onChange={(e) => setInviteRole(e.target.value)}>
                    <option value="Member">Member</option>
                    <option value="Admin">Admin</option>
                  </select>
                </div>
                <div className="flex justify-end space-x-3">
                  <button type="button" onClick={() => setShowInviteModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                  <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Invite</button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default WorkspaceDetail;