import api from './api';

export interface Workspace {
  id: string;
  name: string;
  description: string;
  ownerName: string;
  ownerId: string;
  createdAt: string;
  memberCount: number;
  taskCount: number;
  fileCount: number;
}

export interface WorkspaceMember {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  userAvatar?: string;
  role: string;
  joinedAt: string;
}

export interface InviteData {
  email: string;
  role: string;
}

export const workspaceService = {
  async getAll(): Promise<Workspace[]> {
    const response = await api.get('/workspaces');
    return response.data;
  },

  async getById(id: string): Promise<Workspace> {
    const response = await api.get(`/workspaces/${id}`);
    return response.data;
  },

  async create(data: { name: string; description: string }): Promise<Workspace> {
    const response = await api.post('/workspaces', data);
    return response.data;
  },

  async update(id: string, data: { name: string; description: string }): Promise<Workspace> {
    const response = await api.put(`/workspaces/${id}`, data);
    return response.data;
  },

  async delete(id: string): Promise<void> {
    await api.delete(`/workspaces/${id}`);
  },

  async inviteMember(workspaceId: string, data: InviteData): Promise<void> {
    await api.post(`/workspaces/${workspaceId}/invite`, data);
  },

  async getMembers(workspaceId: string): Promise<WorkspaceMember[]> {
    const response = await api.get(`/workspaces/${workspaceId}/members`);
    return response.data;
  },

  async removeMember(workspaceId: string, memberId: string): Promise<void> {
    await api.delete(`/workspaces/${workspaceId}/members/${memberId}`);
  },

  async updateMemberRole(workspaceId: string, memberId: string, role: string): Promise<void> {
    await api.put(`/workspaces/${workspaceId}/members/${memberId}/role`, { memberId, role });
  }
};