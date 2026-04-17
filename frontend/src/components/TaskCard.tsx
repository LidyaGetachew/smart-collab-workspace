import React, { useState } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Task, taskService } from '../services/taskService';
import toast from 'react-hot-toast';

interface TaskCardProps {
  task: Task;
  workspaceId: string;
  onTaskUpdate: () => void;
  isAdmin: boolean;
  onStatusChange?: (taskId: string, newStatus: string) => Promise<boolean>;
}

const TaskCard: React.FC<TaskCardProps> = ({ 
  task, 
  workspaceId, 
  onTaskUpdate, 
  isAdmin,
  onStatusChange 
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editedTask, setEditedTask] = useState(task);
  const [isUpdating, setIsUpdating] = useState(false);
  
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ 
    id: task.id,
    disabled: isEditing // Disable drag while editing
  });
  
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  const priorityColors = {
    1: 'bg-green-100 text-green-800',
    2: 'bg-yellow-100 text-yellow-800',
    3: 'bg-red-100 text-red-800',
  };

  const priorityIcons = {
    1: '🔵',
    2: '🟡',
    3: '🔴',
  };

  const statusColors = {
    'Todo': 'bg-gray-100 text-gray-800',
    'InProgress': 'bg-blue-100 text-blue-800',
    'Done': 'bg-green-100 text-green-800',
  };

  const statusLabels = {
    'Todo': 'To Do',
    'InProgress': 'In Progress',
    'Done': 'Done',
  };

  // Method 2: Manual status change via dropdown
  const handleLocalStatusChange = async (newStatus: string) => {
    if (newStatus === task.status || isUpdating) return;
    
    setIsUpdating(true);
    try {
      if (onStatusChange) {
        const success = await onStatusChange(task.id, newStatus);
        if (success) {
          onTaskUpdate();
        }
      } else {
        // Fallback: direct API call
        await taskService.updateStatus(workspaceId, task.id, newStatus);
        toast.success(`Status changed to ${statusLabels[newStatus as keyof typeof statusLabels]}`);
        onTaskUpdate();
      }
    } catch (error) {
      toast.error('Failed to change status');
    } finally {
      setIsUpdating(false);
    }
  };

  const handleUpdate = async () => {
    try {
      await taskService.update(workspaceId, task.id, editedTask);
      toast.success('Task updated successfully!');
      setIsEditing(false);
      onTaskUpdate();
    } catch (error) {
      toast.error('Failed to update task');
    }
  };

  const handleDelete = async () => {
    if (window.confirm(`Are you sure you want to delete "${task.title}"?`)) {
      try {
        await taskService.delete(workspaceId, task.id);
        toast.success('Task deleted successfully!');
        onTaskUpdate();
      } catch (error) {
        toast.error('Failed to delete task');
      }
    }
  };

  if (isEditing) {
    return (
      <div className="bg-white rounded-lg shadow p-4 mb-2">
        <input 
          type="text" 
          className="w-full px-2 py-1 border rounded mb-2 focus:outline-none focus:ring-2 focus:ring-blue-500" 
          value={editedTask.title}
          onChange={(e) => setEditedTask({ ...editedTask, title: e.target.value })} 
        />
        <textarea 
          className="w-full px-2 py-1 border rounded mb-2 focus:outline-none focus:ring-2 focus:ring-blue-500" 
          rows={2} 
          value={editedTask.description}
          onChange={(e) => setEditedTask({ ...editedTask, description: e.target.value })} 
        />
        <select 
          className="w-full px-2 py-1 border rounded mb-2" 
          value={editedTask.priority}
          onChange={(e) => setEditedTask({ ...editedTask, priority: parseInt(e.target.value) })}>
          <option value={1}>Low Priority</option>
          <option value={2}>Medium Priority</option>
          <option value={3}>High Priority</option>
        </select>
        <div className="flex space-x-2">
          <button onClick={handleUpdate} className="px-3 py-1 bg-blue-600 text-white rounded text-sm hover:bg-blue-700">
            Save
          </button>
          <button onClick={() => setIsEditing(false)} className="px-3 py-1 bg-gray-300 rounded text-sm hover:bg-gray-400">
            Cancel
          </button>
        </div>
      </div>
    );
  }

  return (
    <div 
      ref={setNodeRef} 
      style={style} 
      {...attributes} 
      {...listeners} 
      className={`bg-white rounded-lg shadow p-4 mb-2 cursor-move hover:shadow-md transition-all ${isDragging ? 'shadow-lg' : ''}`}
    >
      {/* Task Header */}
      <div className="flex justify-between items-start mb-2">
        <h4 className="font-medium text-gray-900 flex-1 pr-2">{task.title}</h4>
        <span className={`text-xs px-2 py-1 rounded-full ${priorityColors[task.priority as keyof typeof priorityColors]}`}>
          {priorityIcons[task.priority as keyof typeof priorityIcons]} {task.priorityLabel}
        </span>
      </div>
      
      {/* Task Description */}
      {task.description && (
        <p className="text-sm text-gray-600 mb-2 line-clamp-2">{task.description}</p>
      )}
      
      {/* Status Dropdown - Easy Status Change */}
      <div className="mb-2">
        <label className="text-xs text-gray-500 block mb-1">Status:</label>
        <select
          value={task.status}
          onChange={(e) => handleLocalStatusChange(e.target.value)}
          disabled={isUpdating}
          className={`text-xs px-2 py-1 rounded border cursor-pointer w-full ${statusColors[task.status as keyof typeof statusColors]} focus:outline-none focus:ring-2 focus:ring-blue-500`}
        >
          <option value="Todo">📋 To Do</option>
          <option value="InProgress">⚙️ In Progress</option>
          <option value="Done">✅ Done</option>
        </select>
        {isUpdating && (
          <span className="text-xs text-gray-400 ml-2">Updating...</span>
        )}
      </div>
      
      {/* Quick Status Buttons - Alternative Method */}
      <div className="flex space-x-1 mb-2">
        <button
          onClick={() => handleLocalStatusChange('Todo')}
          className={`text-xs px-2 py-1 rounded transition-colors flex-1 ${
            task.status === 'Todo' 
              ? 'bg-gray-600 text-white' 
              : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
          disabled={isUpdating}
        >
          To Do
        </button>
        <button
          onClick={() => handleLocalStatusChange('InProgress')}
          className={`text-xs px-2 py-1 rounded transition-colors flex-1 ${
            task.status === 'InProgress' 
              ? 'bg-blue-600 text-white' 
              : 'bg-blue-50 text-blue-700 hover:bg-blue-100'
          }`}
          disabled={isUpdating}
        >
          In Progress
        </button>
        <button
          onClick={() => handleLocalStatusChange('Done')}
          className={`text-xs px-2 py-1 rounded transition-colors flex-1 ${
            task.status === 'Done' 
              ? 'bg-green-600 text-white' 
              : 'bg-green-50 text-green-700 hover:bg-green-100'
          }`}
          disabled={isUpdating}
        >
          Done
        </button>
      </div>
      
      {/* Task Metadata */}
      <div className="flex justify-between items-center text-xs text-gray-500 mt-2 pt-2 border-t">
        <div className="flex items-center space-x-2">
          <span>👤 {task.assignedToName}</span>
        </div>
        {task.dueDate && (
          <span className={task.isOverdue ? 'text-red-600 font-medium' : ''}>
            📅 {new Date(task.dueDate).toLocaleDateString()}
            {task.isOverdue && ' (Overdue)'}
          </span>
        )}
      </div>
      
      {/* Action Buttons */}
      {(isAdmin || task.createdBy === task.id) && (
        <div className="flex justify-end space-x-2 mt-2 pt-2 border-t">
          <button 
            onClick={() => setIsEditing(true)} 
            className="text-blue-600 hover:text-blue-800 text-xs px-2 py-1 rounded hover:bg-blue-50 transition-colors"
          >
            ✏️ Edit
          </button>
          {isAdmin && (
            <button 
              onClick={handleDelete} 
              className="text-red-600 hover:text-red-800 text-xs px-2 py-1 rounded hover:bg-red-50 transition-colors"
            >
              🗑️ Delete
            </button>
          )}
        </div>
      )}
      
      {/* Drag Hint */}
      <div className="text-xs text-gray-400 text-center mt-1">
        ⋮⋮ Drag to reorder or move between columns ⋮⋮
      </div>
    </div>
  );
};

export default TaskCard;