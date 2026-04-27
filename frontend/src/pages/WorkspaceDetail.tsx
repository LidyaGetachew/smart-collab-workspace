import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { workspaceService, WorkspaceMember } from '../services/workspaceService';
import { fileService, FileItem } from '../services/fileService';
import { chatService } from '../services/chatService';
import KanbanBoard from '../components/KanbanBoard';
import toast from 'react-hot-toast';

type TabType = 'kanban' | 'files' | 'members' | 'chat';

const WorkspaceDetail: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<TabType>('kanban');
  const [workspaceName, setWorkspaceName] = useState('');
  const [members, setMembers] = useState<WorkspaceMember[]>([]);
  const [files, setFiles] = useState<FileItem[]>([]);
  const [isAdmin, setIsAdmin] = useState(false);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Member');

  // Chat state
  const [chatMessages, setChatMessages] = useState<any[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [isChatConnected, setIsChatConnected] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [typingUsers, setTypingUsers] = useState<{ userId: string; userName: string }[]>([]);
  let typingTimeout: NodeJS.Timeout;

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { if (id) { loadMembers(); loadFiles(); checkAdmin(); loadWorkspaceInfo(); connectChat(); } return () => { chatService.disconnect(); }; }, [id]);

  const connectChat = async () => {
    if (!id || !user?.token) return;
    const connected = await chatService.connect(id, user.token);
    if (!connected) return;

    setIsChatConnected(true);
    chatService.onMessage((msg) => { setChatMessages(prev => [...prev, msg]); setUnreadCount(prev => prev + 1); });
    chatService.onHistory((msgs) => setChatMessages(msgs));
    chatService.onUnreadCount((count) => setUnreadCount(count));
    chatService.onTyping((userId, userName, isTyping) => { setTypingUsers(prev => isTyping ? [...prev.filter(u => u.userId !== userId), { userId, userName }] : prev.filter(u => u.userId !== userId)); });
    await chatService.loadHistory(id);
  };

  const sendMessage = async () => {
    if (!newMessage.trim() || !id) return;
    await chatService.sendMessage(id, newMessage);
    setNewMessage('');
  };

  const handleTyping = (isTyping: boolean) => {
    if (!id) return;
    chatService.sendTyping(id, isTyping);
    if (typingTimeout) clearTimeout(typingTimeout);
    if (isTyping) typingTimeout = setTimeout(() => chatService.sendTyping(id, false), 1000);
  };

  const loadWorkspaceInfo = async () => { if (id) { const w = await workspaceService.getById(id); setWorkspaceName(w.name); } };
  const checkAdmin = async () => { if (id && user) { const m = await workspaceService.getMembers(id); setIsAdmin(m.some(m => m.userEmail === user.email && m.role === 'Admin')); } };
  const loadMembers = async () => { if (id) setMembers(await workspaceService.getMembers(id)); };
  const loadFiles = async () => { if (id) setFiles(await fileService.getByWorkspace(id)); };
  const handleInvite = async (e: React.FormEvent) => { 
    e.preventDefault(); 
    if (!id) return; 
    try {
      await workspaceService.inviteMember(id, { email: inviteEmail, role: inviteRole }); 
      toast.success('Invitation sent'); 
      setShowInviteModal(false); 
      setInviteEmail(''); 
      loadMembers(); 
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Failed to invite member');
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => { const file = e.target.files?.[0]; if (!file || !id) return; await fileService.upload(id, file); toast.success('File uploaded'); loadFiles(); };
  const handleFileDownload = async (fileId: string) => { if (!id) return; await fileService.download(id, fileId); };
  const handleFileDelete = async (fileId: string) => { if (!id) return; await fileService.delete(id, fileId); toast.success('File deleted'); loadFiles(); };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm border-b sticky top-0 z-10"><div className="max-w-7xl mx-auto px-4 py-3 flex justify-between items-center"><div className="flex items-center gap-4"><button onClick={() => navigate('/workspaces')} className="text-blue-600 hover:text-blue-800">← Back</button><h1 className="text-xl font-semibold">{workspaceName || 'Workspace'}</h1>{isAdmin && <span className="text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded-full">Admin</span>}</div><button onClick={() => setShowInviteModal(true)} className="btn-primary text-sm py-1.5">+ Invite</button></div></nav>
      <div className="max-w-7xl mx-auto px-4 py-6"><div className="border-b mb-6"><div className="flex gap-6 overflow-x-auto"><TabButton active={activeTab === 'kanban'} onClick={() => setActiveTab('kanban')}>📋 Kanban</TabButton><TabButton active={activeTab === 'files'} onClick={() => setActiveTab('files')}>📎 Files ({files.length})</TabButton><TabButton active={activeTab === 'members'} onClick={() => setActiveTab('members')}>👥 Members ({members.length})</TabButton><TabButton active={activeTab === 'chat'} onClick={() => setActiveTab('chat')}>💬 Chat {unreadCount > 0 && <span className="ml-1 bg-red-500 text-white text-xs rounded-full px-1.5">{unreadCount}</span>}</TabButton></div></div>
        {activeTab === 'kanban' && id && <KanbanBoard workspaceId={id} isAdmin={isAdmin} />}
        {activeTab === 'files' && (<div><div className="card mb-4"><label className="btn-primary cursor-pointer inline-block"><input type="file" onChange={handleFileUpload} className="hidden" /> Upload File</label><p className="text-xs text-gray-400 mt-2">Max file size: 10MB</p></div><div className="bg-white rounded-xl shadow divide-y">{files.map(f => (<div key={f.id} className="p-4 flex justify-between items-center hover:bg-gray-50"><div className="flex items-center gap-3"><span className="text-2xl">{f.fileIcon}</span><div><p className="font-medium">{f.fileName}</p><p className="text-xs text-gray-400">{f.formattedFileSize} • {f.uploadedByName} • {f.uploadedAtRelative}</p></div></div><div className="flex gap-2"><button onClick={() => handleFileDownload(f.id)} className="text-blue-600 text-sm">Download</button>{isAdmin && <button onClick={() => handleFileDelete(f.id)} className="text-red-600 text-sm">Delete</button>}</div></div>))}{files.length === 0 && <div className="p-8 text-center text-gray-400">No files uploaded yet</div>}</div></div>)}
        {activeTab === 'members' && (<div className="bg-white rounded-xl shadow divide-y">{members.map(m => (<div key={m.id} className="p-4 flex justify-between items-center"><div className="flex items-center gap-3"><div className="w-10 h-10 rounded-full bg-gradient-to-r from-blue-500 to-purple-500 flex items-center justify-center text-white font-bold">{m.userName.charAt(0)}</div><div><p className="font-medium">{m.userName}</p><p className="text-sm text-gray-500">{m.userEmail}</p></div></div><span className={`px-2 py-1 rounded-full text-xs ${m.role === 'Admin' ? 'bg-purple-100 text-purple-800' : 'bg-gray-100 text-gray-800'}`}>{m.role}</span></div>))}</div>)}
        {activeTab === 'chat' && (
          <div className="bg-white rounded-xl shadow flex flex-col h-[600px] border border-gray-100">
            <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-slate-50/50">
              {chatMessages.map(m => {
                const isMe = m.userId === user?.id;
                return (
                  <div key={m.id} className={`flex ${isMe ? 'justify-end' : 'justify-start'}`}>
                    <div className={`flex items-end gap-2 max-w-[80%] ${isMe ? 'flex-row-reverse' : 'flex-row'}`}>
                      {!isMe && (
                        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-purple-500 flex items-center justify-center text-white text-xs font-bold flex-shrink-0 shadow-sm">
                          {m.userName?.charAt(0) || '?'}
                        </div>
                      )}
                      <div className={`flex flex-col ${isMe ? 'items-end' : 'items-start'}`}>
                        {!isMe && <span className="text-xs text-gray-500 ml-1 mb-1 font-medium">{m.userName}</span>}
                        <div className={`px-4 py-2.5 rounded-2xl shadow-sm ${isMe ? 'bg-gradient-to-r from-blue-600 to-indigo-600 text-white rounded-br-sm' : 'bg-white border border-gray-100 text-gray-800 rounded-bl-sm'}`}>
                          <p className="text-sm leading-relaxed">{m.message}</p>
                        </div>
                        <span className="text-[10px] text-gray-400 mt-1 mx-1">{m.timeAgo}</span>
                      </div>
                    </div>
                  </div>
                );
              })}
              {typingUsers.length > 0 && (
                <div className="flex items-center gap-2 text-xs text-gray-400 italic">
                  <div className="flex gap-1">
                    <span className="animate-bounce">•</span>
                    <span className="animate-bounce" style={{ animationDelay: '150ms' }}>•</span>
                    <span className="animate-bounce" style={{ animationDelay: '300ms' }}>•</span>
                  </div>
                  {typingUsers.map(u => u.userName).join(', ')} typing...
                </div>
              )}
              <div ref={el => el?.scrollIntoView({ behavior: 'smooth' })} />
            </div>
            <div className="p-3 bg-white border-t flex gap-2 rounded-b-xl">
              <input 
                type="text" 
                value={newMessage} 
                onChange={e => setNewMessage(e.target.value)} 
                onKeyDown={(e) => { handleTyping(true); if (e.key === 'Enter') sendMessage(); }} 
                onBlur={() => handleTyping(false)} 
                placeholder="Type your message..." 
                className="flex-1 input bg-slate-50 border-transparent focus:bg-white focus:border-indigo-500 focus:ring-2 focus:ring-indigo-200 text-sm rounded-full px-5 transition-all" 
              />
              <button onClick={sendMessage} disabled={!newMessage.trim()} className="btn-primary py-2 px-6 rounded-full disabled:opacity-50 disabled:cursor-not-allowed shadow-md hover:shadow-lg transition-all active:scale-95 bg-indigo-600 hover:bg-indigo-700">
                Send
              </button>
            </div>
          </div>
        )}
      </div>
      {showInviteModal && (<div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"><div className="bg-white rounded-xl p-6 w-96"><h3 className="text-lg font-bold mb-4">Invite Member</h3><form onSubmit={handleInvite}><input type="email" placeholder="Email address" required value={inviteEmail} onChange={e => setInviteEmail(e.target.value)} className="input mb-3" /><select value={inviteRole} onChange={e => setInviteRole(e.target.value)} className="input mb-4"><option value="Member">Member</option><option value="Admin">Admin</option></select><div className="flex justify-end gap-3"><button type="button" onClick={() => setShowInviteModal(false)} className="btn-secondary">Cancel</button><button type="submit" className="btn-primary">Send Invitation</button></div></form></div></div>)}
    </div>
  );
};

const TabButton: React.FC<{ active: boolean; onClick: () => void; children: React.ReactNode }> = ({ active, onClick, children }) => (<button onClick={onClick} className={`pb-2 px-1 transition-colors ${active ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}>{children}</button>);

export default WorkspaceDetail;