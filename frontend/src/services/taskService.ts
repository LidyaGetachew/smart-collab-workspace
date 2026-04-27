import api from './api';

export interface Task {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: number;
  priorityLabel: string;
  priorityColor: string;
  assignedToName: string;
  assignedToId?: string;
  assignedToAvatar?: string;
  createdByName: string;
  createdById: string;
  createdAt: string;
  dueDate?: string;
  updatedAt?: string;
  isOverdue: boolean;
  daysUntilDue: number;
  commentCount: number;
  statusIcon: string;
}

export interface Comment {
  id: string;
  content: string;
  authorId: string;
  authorName: string;
  authorAvatar?: string;
  createdAt: string;
  timeAgo: string;
}

export interface ActivityLog {
  id: string;
  action: string;
  description: string;
  userName: string;
  userAvatar?: string;
  createdAt: string;
  timeAgo: string;
}

export interface DashboardStats {
  totalTasks: number;
  completedTasks: number;
  inProgressTasks: number;
  todoTasks: number;
  completionRate: number;
  highPriorityTasks: number;
  overdueTasks: number;
  totalMembers: number;
  totalFiles: number;
  totalMessages: number;
}

export interface TaskStatistics {
  status: string;
  label: string;
  count: number;
  color: string;
  percentage: number;
}

export interface RecentActivity {
  id: string;
  action: string;
  description: string;
  userName: string;
  userAvatar?: string;
  taskTitle?: string;
  timestamp: string;
  timeAgo: string;
  icon: string;
}

export const taskService = {
  async getByWorkspace(workspaceId: string): Promise<Task[]> {
    const response = await api.get(`/workspaces/${workspaceId}/tasks`);
    return response.data;
  },

  async getById(workspaceId: string, taskId: string): Promise<Task> {
    const response = await api.get(`/workspaces/${workspaceId}/tasks/${taskId}`);
    return response.data;
  },

  async create(workspaceId: string, data: Partial<Task>): Promise<Task> {
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

  async getComments(workspaceId: string, taskId: string): Promise<Comment[]> {
    const response = await api.get(`/workspaces/${workspaceId}/tasks/${taskId}/comments`);
    return response.data;
  },

  async addComment(workspaceId: string, taskId: string, content: string): Promise<Comment> {
    const response = await api.post(`/workspaces/${workspaceId}/tasks/${taskId}/comments`, { content });
    return response.data;
  },

  async deleteComment(workspaceId: string, taskId: string, commentId: string): Promise<void> {
    await api.delete(`/workspaces/${workspaceId}/tasks/${taskId}/comments/${commentId}`);
  },

  async getActivities(workspaceId: string, taskId: string): Promise<ActivityLog[]> {
    const response = await api.get(`/workspaces/${workspaceId}/tasks/${taskId}/activities`);
    return response.data;
  },

  async getDashboardStats(workspaceId: string): Promise<DashboardStats> {
    const response = await api.get(`/workspaces/${workspaceId}/dashboard/stats`);
    return response.data;
  },

  async getTaskStatistics(workspaceId: string): Promise<TaskStatistics[]> {
    const response = await api.get(`/workspaces/${workspaceId}/dashboard/task-statistics`);
    return response.data;
  },

  async getRecentActivities(workspaceId: string, limit: number = 10): Promise<RecentActivity[]> {
    const response = await api.get(`/workspaces/${workspaceId}/dashboard/recent-activities?limit=${limit}`);
    return response.data;
  }
};