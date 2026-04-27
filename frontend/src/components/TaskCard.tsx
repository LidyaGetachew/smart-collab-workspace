import React, { useState } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { taskService, Comment } from '../services/taskService';
import toast from 'react-hot-toast';

interface TaskCardProps { task: any; workspaceId: string; onTaskUpdate: () => void; isAdmin: boolean; }

const TaskCard: React.FC<TaskCardProps> = ({ task, workspaceId, onTaskUpdate, isAdmin }) => {
  const [showDetails, setShowDetails] = useState(false);
  const [comments, setComments] = useState<Comment[]>([]);
  const [newComment, setNewComment] = useState('');
  const [showComments, setShowComments] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);

  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: task.id });
  const style = { transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.5 : 1 };

  const priorityConfig: Record<number, { label: string; icon: string; color: string }> = {
    1: { label: 'Low', icon: '🔵', color: 'bg-green-100 text-green-800' },
    2: { label: 'Medium', icon: '🟡', color: 'bg-yellow-100 text-yellow-800' },
    3: { label: 'High', icon: '🔴', color: 'bg-red-100 text-red-800' },
  };

  const statusConfig: Record<string, { label: string; icon: string }> = {
    Todo: { label: 'To Do', icon: '📋' },
    InProgress: { label: 'In Progress', icon: '⚙️' },
    Done: { label: 'Done', icon: '✅' },
  };

  const loadComments = async () => {
    try {
      const data = await taskService.getComments(workspaceId, task.id);
      setComments(data);
    } catch (error) { console.error(error); }
  };

  const addComment = async () => {
    if (!newComment.trim()) return;
    try {
      await taskService.addComment(workspaceId, task.id, newComment);
      setNewComment('');
      await loadComments();
      toast.success('Comment added');
    } catch (error) { toast.error('Failed to add comment'); }
  };

  const handleStatusChange = async (newStatus: string) => {
    if (isUpdating) return;
    setIsUpdating(true);
    try {
      await taskService.updateStatus(workspaceId, task.id, newStatus);
      toast.success(`Moved to ${statusConfig[newStatus].label}`);
      onTaskUpdate();
    } catch (error) { toast.error('Failed to update status'); }
    finally { setIsUpdating(false); }
  };

  const handleDelete = async () => {
    if (window.confirm(`Delete "${task.title}"?`)) {
      await taskService.delete(workspaceId, task.id);
      toast.success('Task deleted');
      onTaskUpdate();
    }
  };

  const openDetails = () => { setShowDetails(true); loadComments(); };

  return (
    <>
      <div ref={setNodeRef} style={style} {...attributes} {...listeners} onClick={openDetails} className={`bg-white rounded-lg shadow p-3 mb-2 cursor-pointer hover:shadow-md transition-all border-l-4 ${task.priority === 3 ? 'border-l-red-500' : task.priority === 2 ? 'border-l-yellow-500' : 'border-l-green-500'}`}>
        <div className="flex justify-between items-start">
          <h4 className="font-medium text-sm flex-1">{task.title}</h4>
          <span className={`text-xs px-2 py-0.5 rounded-full ${priorityConfig[task.priority]?.color || ''}`}>{priorityConfig[task.priority]?.icon} {priorityConfig[task.priority]?.label}</span>
        </div>
        <p className="text-xs text-gray-500 mt-1 line-clamp-2">{task.description}</p>
        <div className="flex justify-between items-center mt-2 text-xs text-gray-400">
          <span>👤 {task.assignedToName}</span>
          <span>{statusConfig[task.status]?.icon} {statusConfig[task.status]?.label}</span>
          <button onClick={(e) => { e.stopPropagation(); setShowComments(!showComments); if (!showComments) loadComments(); }} className="text-blue-500">💬 {task.commentCount}</button>
        </div>
      </div>

      {showDetails && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl w-full max-w-lg max-h-[90vh] overflow-hidden">
            <div className="p-4 border-b flex justify-between items-center bg-gray-50">
              <h3 className="font-bold text-lg">{task.title}</h3>
              <button onClick={() => setShowDetails(false)} className="text-gray-500 hover:text-gray-700">✕</button>
            </div>
            <div className="p-4 overflow-y-auto max-h-[60vh] space-y-4">
              <div><label className="text-sm font-medium text-gray-700">Description</label><p className="text-gray-600 mt-1">{task.description || 'No description'}</p></div>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div><label className="font-medium">Priority</label><p>{priorityConfig[task.priority]?.icon} {priorityConfig[task.priority]?.label}</p></div>
                <div><label className="font-medium">Status</label><select value={task.status} onChange={(e) => handleStatusChange(e.target.value)} className="text-sm border rounded px-2 py-1">{Object.entries(statusConfig).map(([key, val]) => <option key={key} value={key}>{val.icon} {val.label}</option>)}</select></div>
                <div><label className="font-medium">Assigned To</label><p>{task.assignedToName}</p></div>
                <div><label className="font-medium">Due Date</label><p className={task.isOverdue ? 'text-red-500' : ''}>{task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'Not set'}{task.isOverdue && ' (Overdue)'}</p></div>
              </div>
              <div><label className="text-sm font-medium text-gray-700">Comments</label><div className="mt-2 space-y-2 max-h-48 overflow-y-auto">{comments.map(c => <div key={c.id} className="bg-gray-50 rounded p-2"><p className="text-xs font-medium">{c.authorName}</p><p className="text-sm">{c.content}</p></div>)}</div><div className="flex gap-2 mt-2"><input type="text" value={newComment} onChange={e => setNewComment(e.target.value)} placeholder="Write a comment..." className="flex-1 input text-sm" /><button onClick={addComment} className="btn-primary text-sm px-3 py-1">Post</button></div></div>
            </div>
            {(isAdmin || task.createdBy === task.id) && (<div className="p-4 border-t bg-gray-50 flex justify-end gap-2"><button onClick={handleDelete} className="text-red-600 hover:text-red-800 text-sm">Delete Task</button></div>)}
          </div>
        </div>
      )}
    </>
  );
};

export default TaskCard;