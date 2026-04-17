import React, { useState, useEffect } from 'react';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { taskService, Task, CreateTaskData } from '../services/taskService';
import TaskCard from './TaskCard';
import toast from 'react-hot-toast';

interface KanbanBoardProps {
  workspaceId: string;
  isAdmin: boolean;
}

const KanbanBoard: React.FC<KanbanBoardProps> = ({ workspaceId, isAdmin }) => {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [showModal, setShowModal] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [newTask, setNewTask] = useState<CreateTaskData>({
    title: '',
    description: '',
    priority: 2,
    assignedToEmail: '',  // Now this is valid
    dueDate: '',           // Now this is valid
  });

  useEffect(() => {
    loadTasks();
  }, [workspaceId]);

  const loadTasks = async () => {
    setIsLoading(true);
    try {
      const data = await taskService.getByWorkspace(workspaceId);
      setTasks(data);
    } catch (error) {
      toast.error('Failed to load tasks');
      console.error('Error loading tasks:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateTask = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTask.title.trim()) {
      toast.error('Task title is required');
      return;
    }
    
    try {
      // If assignedToEmail is provided, you need to convert it to userId
      // For now, we'll send without assignment
      const taskData: CreateTaskData = {
        title: newTask.title,
        description: newTask.description,
        priority: newTask.priority,
      };
      
      if (newTask.dueDate) {
        taskData.dueDate = newTask.dueDate;
      }
      
      // TODO: Add logic to convert email to user ID
      // if (newTask.assignedToEmail) {
      //   const user = await userService.getUserByEmail(newTask.assignedToEmail);
      //   if (user) taskData.assignedToId = user.id;
      // }
      
      const task = await taskService.create(workspaceId, taskData);
      setTasks([...tasks, task]);
      setShowModal(false);
      setNewTask({ 
        title: '', 
        description: '', 
        priority: 2,
        assignedToEmail: '',
        dueDate: '',
      });
      toast.success('Task created successfully!');
    } catch (error) {
      toast.error('Failed to create task');
      console.error('Error creating task:', error);
    }
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over) return;

    const activeTask = tasks.find(t => t.id === active.id);
    const overTask = tasks.find(t => t.id === over.id);
    
    if (!activeTask || !overTask) return;
    
    if (activeTask.status !== overTask.status) {
      try {
        const updatedTask = await taskService.updateStatus(workspaceId, activeTask.id, overTask.status);
        setTasks(tasks.map(t => t.id === updatedTask.id ? updatedTask : t));
        
        const statusNames = { Todo: 'To Do', InProgress: 'In Progress', Done: 'Done' };
        toast.success(`Task moved to ${statusNames[overTask.status as keyof typeof statusNames]}`);
      } catch (error) {
        toast.error('Failed to move task');
        await loadTasks();
      }
    } else {
      const oldIndex = tasks.findIndex(t => t.id === active.id);
      const newIndex = tasks.findIndex(t => t.id === over.id);
      setTasks(arrayMove(tasks, oldIndex, newIndex));
    }
  };

  const handleStatusChange = async (taskId: string, newStatus: string) => {
    try {
      const updatedTask = await taskService.updateStatus(workspaceId, taskId, newStatus);
      setTasks(tasks.map(t => t.id === updatedTask.id ? updatedTask : t));
      const statusNames = { Todo: 'To Do', InProgress: 'In Progress', Done: 'Done' };
      toast.success(`Task status updated to ${statusNames[newStatus as keyof typeof statusNames]}`);
      return true;
    } catch (error) {
      toast.error('Failed to update task status');
      return false;
    }
  };

  const getTasksByStatus = (status: string) => tasks.filter(t => t.status === status);

  const columns = [
    { title: 'To Do', status: 'Todo', color: 'bg-gray-100', borderColor: 'border-gray-300' },
    { title: 'In Progress', status: 'InProgress', color: 'bg-blue-100', borderColor: 'border-blue-300' },
    { title: 'Done', status: 'Done', color: 'bg-green-100', borderColor: 'border-green-300' },
  ];

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 5 },
    }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="text-gray-500">Loading tasks...</div>
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-xl font-bold">Tasks</h2>
        <button 
          onClick={() => setShowModal(true)} 
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
        >
          + Add Task
        </button>
      </div>

      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <div className="flex gap-4 overflow-x-auto pb-4">
          {columns.map(column => (
            <div key={column.status} className="flex-1 min-w-[300px]">
              <div className={`rounded-t-lg p-3 ${column.color} border-b-2 ${column.borderColor}`}>
                <div className="flex justify-between items-center">
                  <h3 className="font-semibold text-lg">{column.title}</h3>
                  <span className="text-sm bg-white bg-opacity-50 px-2 py-1 rounded-full">
                    {getTasksByStatus(column.status).length}
                  </span>
                </div>
              </div>
              <div className="bg-gray-50 rounded-b-lg p-3 min-h-[500px]">
                <SortableContext
                  items={getTasksByStatus(column.status).map(t => t.id)}
                  strategy={verticalListSortingStrategy}
                >
                  {getTasksByStatus(column.status).length === 0 ? (
                    <div className="text-center text-gray-400 py-8 text-sm">
                      No tasks in {column.title}
                      <br />
                      <button 
                        onClick={() => setShowModal(true)}
                        className="text-blue-500 hover:text-blue-600 text-xs mt-2"
                      >
                        + Add a task
                      </button>
                    </div>
                  ) : (
                    getTasksByStatus(column.status).map(task => (
                      <TaskCard 
                        key={task.id} 
                        task={task} 
                        workspaceId={workspaceId} 
                        onTaskUpdate={loadTasks} 
                        isAdmin={isAdmin}
                        onStatusChange={handleStatusChange}
                      />
                    ))
                  )}
                </SortableContext>
              </div>
            </div>
          ))}
        </div>
      </DndContext>

      {/* Create Task Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-medium">Create New Task</h3>
              <button 
                onClick={() => setShowModal(false)} 
                className="text-gray-400 hover:text-gray-600 text-2xl"
              >
                ×
              </button>
            </div>
            <form onSubmit={handleCreateTask}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Title <span className="text-red-500">*</span>
                </label>
                <input 
                  type="text" 
                  required 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={newTask.title} 
                  onChange={(e) => setNewTask({ ...newTask, title: e.target.value })} 
                  placeholder="Enter task title"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500" 
                  rows={3} 
                  value={newTask.description} 
                  onChange={(e) => setNewTask({ ...newTask, description: e.target.value })} 
                  placeholder="Enter task description (optional)"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Priority</label>
                <select 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={newTask.priority} 
                  onChange={(e) => setNewTask({ ...newTask, priority: parseInt(e.target.value) })}>
                  <option value={1}>🔵 Low Priority</option>
                  <option value={2}>🟡 Medium Priority</option>
                  <option value={3}>🔴 High Priority</option>
                </select>
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Assign To (Optional)</label>
                <input 
                  type="email" 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Enter user email to assign"
                  value={newTask.assignedToEmail || ''}
                  onChange={(e) => setNewTask({ ...newTask, assignedToEmail: e.target.value })}
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Due Date (Optional)</label>
                <input 
                  type="date" 
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={newTask.dueDate || ''}
                  onChange={(e) => setNewTask({ ...newTask, dueDate: e.target.value })}
                />
              </div>
              <div className="flex justify-end space-x-3">
                <button 
                  type="button" 
                  onClick={() => setShowModal(false)} 
                  className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button 
                  type="submit" 
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                  Create Task
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default KanbanBoard;