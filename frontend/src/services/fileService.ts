import api from './api';

export interface FileItem {
  id: string;
  fileName: string;
  fileSize: number;
  formattedFileSize: string;
  mimeType: string;
  uploadedByName: string;
  uploadedAt: string;
}

export const fileService = {
  async getByWorkspace(workspaceId: string): Promise<FileItem[]> {
    const response = await api.get(`/workspaces/${workspaceId}/files`);
    return response.data;
  },

  async upload(workspaceId: string, file: File): Promise<FileItem> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post(`/workspaces/${workspaceId}/files/upload`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  async download(workspaceId: string, fileId: string): Promise<void> {
    const response = await api.get(`/workspaces/${workspaceId}/files/${fileId}/download`, {
      responseType: 'blob',
    });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', response.headers['content-disposition']?.split('filename=')[1] || 'file');
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  async delete(workspaceId: string, fileId: string): Promise<void> {
    await api.delete(`/workspaces/${workspaceId}/files/${fileId}`);
  },
};