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
}

const TaskCard: React.FC<TaskCardProps> = ({ task, workspaceId, onTaskUpdate, isAdmin }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editedTask, setEditedTask] = useState(task);
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id: task.id });
  const style = { transform: CSS.Transform.toString(transform), transition };

  const priorityColors = {
    1: 'bg-green-100 text-green-800',
    2: 'bg-yellow-100 text-yellow-800',
    3: 'bg-red-100 text-red-800',
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
    if (window.confirm('Are you sure you want to delete this task?')) {
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
        <input type="text" className="w-full px-2 py-1 border rounded mb-2" value={editedTask.title}
          onChange={(e) => setEditedTask({ ...editedTask, title: e.target.value })} />
        <textarea className="w-full px-2 py-1 border rounded mb-2" rows={2} value={editedTask.description}
          onChange={(e) => setEditedTask({ ...editedTask, description: e.target.value })} />
        <select className="w-full px-2 py-1 border rounded mb-2" value={editedTask.priority}
          onChange={(e) => setEditedTask({ ...editedTask, priority: parseInt(e.target.value) })}>
          <option value={1}>Low</option>
          <option value={2}>Medium</option>
          <option value={3}>High</option>
        </select>
        <div className="flex space-x-2">
          <button onClick={handleUpdate} className="px-3 py-1 bg-blue-600 text-white rounded text-sm">Save</button>
          <button onClick={() => setIsEditing(false)} className="px-3 py-1 bg-gray-300 rounded text-sm">Cancel</button>
        </div>
      </div>
    );
  }

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners} className="bg-white rounded-lg shadow p-4 mb-2 cursor-move hover:shadow-md transition-shadow">
      <div className="flex justify-between items-start mb-2">
        <h4 className="font-medium text-gray-900">{task.title}</h4>
        <span className={`text-xs px-2 py-1 rounded ${priorityColors[task.priority as keyof typeof priorityColors]}`}>
          {task.priorityLabel}
        </span>
      </div>
      <p className="text-sm text-gray-600 mb-2">{task.description}</p>
      <div className="flex justify-between items-center text-xs text-gray-500">
        <span>Assigned to: {task.assignedToName}</span>
        {task.dueDate && <span>Due: {new Date(task.dueDate).toLocaleDateString()}</span>}
      </div>
      {(isAdmin || task.assignedToId === task.id) && (
        <div className="flex justify-end space-x-2 mt-2">
          <button onClick={() => setIsEditing(true)} className="text-blue-600 hover:text-blue-800 text-xs">Edit</button>
          {isAdmin && <button onClick={handleDelete} className="text-red-600 hover:text-red-800 text-xs">Delete</button>}
        </div>
      )}
    </div>
  );
};

export default TaskCard;