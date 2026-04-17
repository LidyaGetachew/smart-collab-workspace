import api from './api';

export interface Task {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: number;
  priorityLabel: string;
  assignedToName: string;
  assignedToId?: string;
  createdByName: string;
  createdAt: string;
  dueDate?: string;
  isOverdue: boolean;
}

export interface CreateTaskData {
  title: string;
  description: string;
  priority: number;
  assignedToId?: string;
  dueDate?: string;
}

export const taskService = {
  async getByWorkspace(workspaceId: string): Promise<Task[]> {
    const response = await api.get(`/workspaces/${workspaceId}/tasks`);
    return response.data;
  },

  async create(workspaceId: string, data: CreateTaskData): Promise<Task> {
    const response = await api.post(`/workspaces/${workspaceId}/tasks`, data);
    return response.data;
  },

  async update(workspaceId: string, taskId: string, data: Partial<Task>): Promise<Task> {
    const response = await api.put(`/workspaces/${workspaceId}/tasks/${taskId}`, data);
    return response.data;
  },

  async updateStatus(workspaceId: string, taskId: string, status: string): Promise<Task> {
    const response = await api.patch(`/workspaces/${workspaceId}/tasks/${taskId}/status`, status);
    return response.data;
  },

  async delete(workspaceId: string, taskId: string): Promise<void> {
    await api.delete(`/workspaces/${workspaceId}/tasks/${taskId}`);
  },
};