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
  const [newTask, setNewTask] = useState<CreateTaskData>({
    title: '',
    description: '',
    priority: 2,
  });

  useEffect(() => {
    loadTasks();
  }, [workspaceId]);

  const loadTasks = async () => {
    try {
      const data = await taskService.getByWorkspace(workspaceId);
      setTasks(data);
    } catch (error) {
      toast.error('Failed to load tasks');
    }
  };

  const handleCreateTask = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const task = await taskService.create(workspaceId, newTask);
      setTasks([...tasks, task]);
      setShowModal(false);
      setNewTask({ title: '', description: '', priority: 2 });
      toast.success('Task created successfully!');
    } catch (error) {
      toast.error('Failed to create task');
    }
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over) return;

    const activeTask = tasks.find(t => t.id === active.id);
    const overTask = tasks.find(t => t.id === over.id);
    
    if (activeTask && overTask && activeTask.status !== overTask.status) {
      // Update status when moving between columns
      try {
        const updatedTask = await taskService.updateStatus(workspaceId, activeTask.id, overTask.status);
        setTasks(tasks.map(t => t.id === updatedTask.id ? updatedTask : t));
        toast.success(`Task moved to ${overTask.status}`);
      } catch (error) {
        toast.error('Failed to move task');
      }
    } else {
      // Just reorder within same column
      const oldIndex = tasks.findIndex(t => t.id === active.id);
      const newIndex = tasks.findIndex(t => t.id === over.id);
      setTasks(arrayMove(tasks, oldIndex, newIndex));
    }
  };

  const getTasksByStatus = (status: string) => tasks.filter(t => t.status === status);

  const columns = [
    { title: 'To Do', status: 'Todo', color: 'bg-gray-100' },
    { title: 'In Progress', status: 'InProgress', color: 'bg-blue-100' },
    { title: 'Done', status: 'Done', color: 'bg-green-100' },
  ];

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-xl font-bold">Tasks</h2>
        <button onClick={() => setShowModal(true)} className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
          + Add Task
        </button>
      </div>

      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <div className="flex gap-4 overflow-x-auto pb-4">
          {columns.map(column => (
            <div key={column.status} className="flex-1 min-w-[300px]">
              <div className={`rounded-t-lg p-3 ${column.color}`}>
                <h3 className="font-semibold text-lg">{column.title}</h3>
                <span className="text-sm text-gray-600">{getTasksByStatus(column.status).length} tasks</span>
              </div>
              <div className="bg-gray-50 rounded-b-lg p-3 min-h-[500px]">
                <SortableContext
                  items={getTasksByStatus(column.status).map(t => t.id)}
                  strategy={verticalListSortingStrategy}
                >
                  {getTasksByStatus(column.status).map(task => (
                    <TaskCard key={task.id} task={task} workspaceId={workspaceId} onTaskUpdate={loadTasks} isAdmin={isAdmin} />
                  ))}
                </SortableContext>
              </div>
            </div>
          ))}
        </div>
      </DndContext>

      {/* Create Task Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-medium">Create Task</h3>
              <button onClick={() => setShowModal(false)} className="text-gray-400 hover:text-gray-600">×</button>
            </div>
            <form onSubmit={handleCreateTask}>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Title</label>
                <input type="text" required className="w-full px-3 py-2 border border-gray-300 rounded-md"
                  value={newTask.title} onChange={(e) => setNewTask({ ...newTask, title: e.target.value })} />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
                <textarea className="w-full px-3 py-2 border border-gray-300 rounded-md" rows={3}
                  value={newTask.description} onChange={(e) => setNewTask({ ...newTask, description: e.target.value })} />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Priority</label>
                <select className="w-full px-3 py-2 border border-gray-300 rounded-md"
                  value={newTask.priority} onChange={(e) => setNewTask({ ...newTask, priority: parseInt(e.target.value) })}>
                  <option value={1}>Low</option>
                  <option value={2}>Medium</option>
                  <option value={3}>High</option>
                </select>
              </div>
              <div className="flex justify-end space-x-3">
                <button type="button" onClick={() => setShowModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default KanbanBoard;