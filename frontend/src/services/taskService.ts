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
  createdById: string;
  createdAt: string;
  dueDate?: string;
  updatedAt?: string;
  isOverdue: boolean;
  daysUntilDue?: number;
  createdBy: string;
}

export interface CreateTaskData {
  title: string;
  description: string;
  priority: number;
  assignedToId?: string;      // Use ID instead of email
  assignedToEmail?: string;    // Add this for email input
  dueDate?: string;            // Add this for date input
}

export interface UpdateTaskData {
  title: string;
  description: string;
  status: string;
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
    // Convert email to ID if needed (you'll need to fetch user ID by email)
    const createData: any = {
      title: data.title,
      description: data.description,
      priority: data.priority,
    };
    
    if (data.dueDate) {
      createData.dueDate = data.dueDate;
    }
    
    if (data.assignedToId) {
      createData.assignedToId = data.assignedToId;
    }
    
    const response = await api.post(`/workspaces/${workspaceId}/tasks`, createData);
    return response.data;
  },

  async update(workspaceId: string, taskId: string, data: UpdateTaskData): Promise<Task> {
    const response = await api.put(`/workspaces/${workspaceId}/tasks/${taskId}`, data);
    return response.data;
  },

  async updateStatus(workspaceId: string, taskId: string, status: string): Promise<Task> {
    const response = await api.patch(`/workspaces/${workspaceId}/tasks/${taskId}/status`, status, {
      headers: { 'Content-Type': 'application/json' }
    });
    return response.data;
  },

  async delete(workspaceId: string, taskId: string): Promise<void> {
    await api.delete(`/workspaces/${workspaceId}/tasks/${taskId}`);
  },
};