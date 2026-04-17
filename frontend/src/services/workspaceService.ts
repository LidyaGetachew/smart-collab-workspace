import api from './api';

export interface Workspace {
  id: string;
  name: string;
  description: string;
  ownerName: string;
  createdAt: string;
  memberCount: number;
  taskCount: number;
}

export interface WorkspaceMember {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  role: string;
  joinedAt: string;
}

export const workspaceService = {
  async getAll(): Promise<Workspace[]> {
    const response = await api.get('/workspaces');
    return response.data;
  },

  async create(data: { name: string; description: string }): Promise<Workspace> {
    const response = await api.post('/workspaces', data);
    return response.data;
  },

  async inviteMember(workspaceId: string, email: string, role: string): Promise<void> {
    await api.post(`/workspaces/${workspaceId}/invite`, { email, role });
  },

  async getMembers(workspaceId: string): Promise<WorkspaceMember[]> {
    const response = await api.get(`/workspaces/${workspaceId}/members`);
    return response.data;
  },
};