import api from './api';

export interface FileItem {
  id: string;
  fileName: string;
  fileSize: number;
  formattedFileSize: string;
  mimeType: string;
  fileIcon: string;
  uploadedByName: string;
  uploadedById: string;
  uploadedAt: string;
  uploadedAtRelative: string;
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
    const contentDisposition = response.headers['content-disposition'];
    const filename = contentDisposition?.split('filename=')[1]?.replace(/["']/g, '') || 'download';
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  async delete(workspaceId: string, fileId: string): Promise<void> {
    await api.delete(`/workspaces/${workspaceId}/files/${fileId}`);
  }
};