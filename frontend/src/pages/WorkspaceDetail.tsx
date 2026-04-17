import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { workspaceService, WorkspaceMember } from '../services/workspaceService';
import { taskService, Task } from '../services/taskService';
import { fileService, FileItem } from '../services/fileService';
import KanbanBoard from '../components/KanbanBoard';
import toast from 'react-hot-toast';

type TabType = 'kanban' | 'files' | 'members';

const WorkspaceDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  // State for active tab
  const [activeTab, setActiveTab] = useState<TabType>('kanban');
  
  // State for members
  const [members, setMembers] = useState<WorkspaceMember[]>([]);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Member');
  const [isInviting, setIsInviting] = useState(false);
  
  // State for files
  const [files, setFiles] = useState<FileItem[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const [isLoadingFiles, setIsLoadingFiles] = useState(false);
  
  // State for workspace info
  const [workspaceName, setWorkspaceName] = useState('');
  const [isAdmin, setIsAdmin] = useState(false);

  // Load data on component mount
  useEffect(() => {
    if (id) {
      loadMembers();
      loadFiles();
      checkAdminStatus();
    }
  }, [id]);

  // Check if current user is admin
  const checkAdminStatus = async () => {
    if (!id || !user) return;
    try {
      const membersList = await workspaceService.getMembers(id);
      const currentMember = membersList.find(m => m.userEmail === user.email);
      setIsAdmin(currentMember?.role === 'Admin');
    } catch (error) {
      console.error('Error checking admin status:', error);
    }
  };

  // ========== MEMBERS TAB FUNCTIONS ==========
  
  const loadMembers = async () => {
    if (!id) return;
    try {
      const data = await workspaceService.getMembers(id);
      setMembers(data);
    } catch (error) {
      toast.error('Failed to load members');
      console.error('Error loading members:', error);
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id || !inviteEmail.trim()) {
      toast.error('Please enter an email address');
      return;
    }

    setIsInviting(true);
    try {
      await workspaceService.inviteMember(id, inviteEmail, inviteRole);
      toast.success(`Invitation sent to ${inviteEmail}!`);
      setShowInviteModal(false);
      setInviteEmail('');
      setInviteRole('Member');
      await loadMembers(); // Refresh member list
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to invite member';
      toast.error(errorMessage);
      console.error('Error inviting member:', error);
    } finally {
      setIsInviting(false);
    }
  };

  const handleRemoveMember = async (memberId: string, memberName: string) => {
    if (!isAdmin) {
      toast.error('Only admins can remove members');
      return;
    }
    
    if (window.confirm(`Are you sure you want to remove ${memberName} from this workspace?`)) {
      try {
        // Note: You need to implement this endpoint in your backend
        await workspaceService.removeMember(id!, memberId);
        toast.success(`${memberName} removed from workspace`);
        await loadMembers(); // Refresh member list
      } catch (error) {
        toast.error('Failed to remove member');
        console.error('Error removing member:', error);
      }
    }
  };

  const handleChangeRole = async (memberId: string, memberName: string, newRole: string) => {
    if (!isAdmin) {
      toast.error('Only admins can change roles');
      return;
    }
    
    try {
      // Note: You need to implement this endpoint in your backend
      await workspaceService.updateMemberRole(id!, memberId, newRole);
      toast.success(`${memberName} role changed to ${newRole}`);
      await loadMembers(); // Refresh member list
      await checkAdminStatus(); // Re-check admin status for current user
    } catch (error) {
      toast.error('Failed to change role');
      console.error('Error changing role:', error);
    }
  };

  // ========== FILES TAB FUNCTIONS ==========
  
  const loadFiles = async () => {
    if (!id) return;
    setIsLoadingFiles(true);
    try {
      const data = await fileService.getByWorkspace(id);
      setFiles(data);
    } catch (error) {
      toast.error('Failed to load files');
      console.error('Error loading files:', error);
    } finally {
      setIsLoadingFiles(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !id) return;

    // Validate file size (max 10MB)
    const maxSize = 10 * 1024 * 1024; // 10MB
    if (file.size > maxSize) {
      toast.error('File size must be less than 10MB');
      return;
    }

    setIsUploading(true);
    try {
      await fileService.upload(id, file);
      toast.success(`${file.name} uploaded successfully!`);
      await loadFiles(); // Refresh file list
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Failed to upload file';
      toast.error(errorMessage);
      console.error('Error uploading file:', error);
    } finally {
      setIsUploading(false);
      // Clear the input
      e.target.value = '';
    }
  };

  const handleFileDownload = async (fileId: string, fileName: string) => {
    if (!id) return;
    try {
      await fileService.download(id, fileId);
      toast.success(`Downloading ${fileName}...`);
    } catch (error) {
      toast.error('Failed to download file');
      console.error('Error downloading file:', error);
    }
  };

  const handleFileDelete = async (fileId: string, fileName: string) => {
    if (!isAdmin) {
      toast.error('Only admins can delete files');
      return;
    }
    
    if (window.confirm(`Are you sure you want to delete "${fileName}"?`)) {
      try {
        await fileService.delete(id!, fileId);
        toast.success(`${fileName} deleted successfully!`);
        await loadFiles(); // Refresh file list
      } catch (error) {
        toast.error('Failed to delete file');
        console.error('Error deleting file:', error);
      }
    }
  };

  // Format file size for display
  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  // Get file icon based on mime type
  const getFileIcon = (mimeType: string): string => {
    if (mimeType.startsWith('image/')) return '🖼️';
    if (mimeType.startsWith('video/')) return '🎥';
    if (mimeType.startsWith('audio/')) return '🎵';
    if (mimeType.includes('pdf')) return '📄';
    if (mimeType.includes('word') || mimeType.includes('document')) return '📝';
    if (mimeType.includes('excel') || mimeType.includes('sheet')) return '📊';
    if (mimeType.includes('powerpoint')) return '📽️';
    if (mimeType.includes('zip') || mimeType.includes('compressed')) return '🗜️';
    return '📁';
  };

  // Format date
  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  };

  // Get role badge color
  const getRoleBadgeColor = (role: string): string => {
    return role === 'Admin' 
      ? 'bg-purple-100 text-purple-800' 
      : 'bg-gray-100 text-gray-800';
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navigation Bar */}
      <nav className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center space-x-4">
              <button 
                onClick={() => navigate('/workspaces')} 
                className="text-blue-600 hover:text-blue-800 flex items-center"
              >
                ← Back
              </button>
              <h1 className="text-xl font-semibold text-gray-900">
                Workspace
              </h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-600">
                {user?.firstName} {user?.lastName}
              </span>
              {isAdmin && (
                <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded-full">
                  Admin
                </span>
              )}
            </div>
          </div>
        </div>
      </nav>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Tabs */}
        <div className="border-b border-gray-200 mb-6">
          <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setActiveTab('kanban')}
              className={`${
                activeTab === 'kanban'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
            >
              📋 Kanban Board
            </button>
            <button
              onClick={() => setActiveTab('files')}
              className={`${
                activeTab === 'files'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
            >
              📎 Files ({files.length})
            </button>
            <button
              onClick={() => setActiveTab('members')}
              className={`${
                activeTab === 'members'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors`}
            >
              👥 Members ({members.length})
            </button>
          </nav>
        </div>

        {/* Tab Content */}
        <div className="mt-6">
          {/* KANBAN BOARD TAB */}
          {activeTab === 'kanban' && id && (
            <KanbanBoard workspaceId={id} isAdmin={isAdmin} />
          )}

          {/* FILES TAB */}
          {activeTab === 'files' && (
            <div>
              {/* Upload Section */}
              <div className="bg-white rounded-lg shadow mb-6 p-6">
                <div className="flex justify-between items-center mb-4">
                  <h3 className="text-lg font-medium text-gray-900">Upload Files</h3>
                  {isAdmin && (
                    <label className="cursor-pointer px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors">
                      {isUploading ? 'Uploading...' : '+ Upload File'}
                      <input 
                        type="file" 
                        onChange={handleFileUpload} 
                        className="hidden" 
                        disabled={isUploading}
                      />
                    </label>
                  )}
                </div>
                {!isAdmin && (
                  <p className="text-sm text-gray-500">Only admins can upload files</p>
                )}
                <p className="text-xs text-gray-400 mt-2">Maximum file size: 10MB</p>
              </div>

              {/* Files List */}
              <div className="bg-white rounded-lg shadow">
                <div className="px-6 py-4 border-b border-gray-200">
                  <h3 className="text-lg font-medium text-gray-900">All Files</h3>
                </div>
                
                {isLoadingFiles ? (
                  <div className="p-8 text-center text-gray-500">Loading files...</div>
                ) : files.length === 0 ? (
                  <div className="p-8 text-center text-gray-500">
                    <div className="text-4xl mb-2">📂</div>
                    <p>No files uploaded yet</p>
                    {isAdmin && (
                      <p className="text-sm mt-2">Click the "Upload File" button to add files</p>
                    )}
                  </div>
                ) : (
                  <div className="divide-y divide-gray-200">
                    {files.map(file => (
                      <div key={file.id} className="p-4 hover:bg-gray-50 transition-colors">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center space-x-3 flex-1">
                            <div className="text-2xl">{getFileIcon(file.mimeType)}</div>
                            <div className="flex-1">
                              <p className="font-medium text-gray-900">{file.fileName}</p>
                              <div className="flex space-x-4 text-xs text-gray-500 mt-1">
                                <span>{formatFileSize(file.fileSize)}</span>
                                <span>•</span>
                                <span>Uploaded by {file.uploadedByName}</span>
                                <span>•</span>
                                <span>{formatDate(file.uploadedAt)}</span>
                              </div>
                            </div>
                          </div>
                          <div className="flex space-x-2">
                            <button
                              onClick={() => handleFileDownload(file.id, file.fileName)}
                              className="px-3 py-1 text-blue-600 hover:text-blue-800 text-sm"
                            >
                              Download
                            </button>
                            {isAdmin && (
                              <button
                                onClick={() => handleFileDelete(file.id, file.fileName)}
                                className="px-3 py-1 text-red-600 hover:text-red-800 text-sm"
                              >
                                Delete
                              </button>
                            )}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}

          {/* MEMBERS TAB */}
          {activeTab === 'members' && (
            <div>
              {/* Invite Section */}
              <div className="bg-white rounded-lg shadow mb-6 p-6">
                <div className="flex justify-between items-center mb-4">
                  <h3 className="text-lg font-medium text-gray-900">Team Members</h3>
                  {isAdmin && (
                    <button
                      onClick={() => setShowInviteModal(true)}
                      className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                    >
                      + Invite Member
                    </button>
                  )}
                </div>
                <p className="text-sm text-gray-500">
                  {members.length} member{members.length !== 1 ? 's' : ''} in this workspace
                </p>
              </div>

              {/* Members List */}
              <div className="bg-white rounded-lg shadow">
                <div className="divide-y divide-gray-200">
                  {members.map(member => (
                    <div key={member.id} className="p-4 hover:bg-gray-50 transition-colors">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center space-x-3">
                          <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center">
                            <span className="text-gray-600 font-medium">
                              {member.userName.charAt(0).toUpperCase()}
                            </span>
                          </div>
                          <div>
                            <p className="font-medium text-gray-900">{member.userName}</p>
                            <p className="text-sm text-gray-500">{member.userEmail}</p>
                            <p className="text-xs text-gray-400 mt-1">
                              Joined {formatDate(member.joinedAt)}
                            </p>
                          </div>
                        </div>
                        <div className="flex items-center space-x-3">
                          <span className={`px-2 py-1 rounded-full text-xs font-medium ${getRoleBadgeColor(member.role)}`}>
                            {member.role}
                          </span>
                          {isAdmin && member.userEmail !== user?.email && (
                            <div className="flex space-x-2">
                              <select
                                value={member.role}
                                onChange={(e) => handleChangeRole(member.id, member.userName, e.target.value)}
                                className="text-sm border rounded px-2 py-1"
                              >
                                <option value="Member">Member</option>
                                <option value="Admin">Admin</option>
                              </select>
                              <button
                                onClick={() => handleRemoveMember(member.id, member.userName)}
                                className="text-red-600 hover:text-red-800 text-sm"
                              >
                                Remove
                              </button>
                            </div>
                          )}
                          {member.userEmail === user?.email && (
                            <span className="text-xs text-gray-400">(You)</span>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Invite Member Modal */}
      {showInviteModal && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-medium">Invite Member</h3>
              <button 
                onClick={() => setShowInviteModal(false)} 
                className="text-gray-400 hover:text-gray-600 text-2xl"
              >
                ×
              </button>
            </div>
            <form onSubmit={handleInvite}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Email Address
                </label>
                <input 
                  type="email" 
                  required 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Enter user's email"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                />
                <p className="text-xs text-gray-500 mt-1">
                  User must have an account to join
                </p>
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Role
                </label>
                <select 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={inviteRole}
                  onChange={(e) => setInviteRole(e.target.value)}
                >
                  <option value="Member">Member - Can view and create tasks</option>
                  <option value="Admin">Admin - Full control over workspace</option>
                </select>
              </div>
              <div className="flex justify-end space-x-3">
                <button 
                  type="button" 
                  onClick={() => setShowInviteModal(false)} 
                  className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button 
                  type="submit" 
                  disabled={isInviting}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:bg-blue-300"
                >
                  {isInviting ? 'Inviting...' : 'Send Invitation'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default WorkspaceDetail;