import React, { useState, useEffect } from 'react';
import { DndContext, closestCenter, PointerSensor, useSensor, useSensors, DragEndEvent } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { taskService, Task } from '../services/taskService';
import TaskCard from './TaskCard';
import toast from 'react-hot-toast';

interface KanbanBoardProps { workspaceId: string; isAdmin: boolean; }

const KanbanBoard: React.FC<KanbanBoardProps> = ({ workspaceId, isAdmin }) => {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [newTask, setNewTask] = useState({ title: '', description: '', priority: 2, dueDate: '' });

  useEffect(() => { loadTasks(); }, [workspaceId]);

  const loadTasks = async () => {
    try {
      setLoading(true);
      const data = await taskService.getByWorkspace(workspaceId);
      setTasks(data);
    } catch (error) {
      toast.error('Failed to load tasks');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const task = await taskService.create(workspaceId, newTask);
      setTasks([...tasks, task]);
      setShowModal(false);
      setNewTask({ title: '', description: '', priority: 2, dueDate: '' });
      toast.success('Task created');
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
      await taskService.updateStatus(workspaceId, activeTask.id, overTask.status);
      await loadTasks();
    }
  };

  const sensors = useSensors(useSensor(PointerSensor));
  const columns = [
    { title: '📋 To Do', status: 'Todo', color: 'bg-gray-100' },
    { title: '⚙️ In Progress', status: 'InProgress', color: 'bg-blue-100' },
    { title: '✅ Done', status: 'Done', color: 'bg-green-100' },
  ];

  if (loading) return <div className="text-center py-8">Loading tasks...</div>;

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-xl font-bold">Kanban Board</h2>
        <button onClick={() => setShowModal(true)} className="btn-primary">+ Add Task</button>
      </div>

      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <div className="flex gap-4 overflow-x-auto pb-4">
          {columns.map(col => (
            <div key={col.status} className="flex-1 min-w-[300px]">
              <div className={`rounded-t-lg p-3 ${col.color}`}>
                <h3 className="font-semibold">{col.title}</h3>
                <span className="text-sm">{tasks.filter(t => t.status === col.status).length} tasks</span>
              </div>
              <div className="bg-gray-50 rounded-b-lg p-3 min-h-[500px]">
                <SortableContext items={tasks.filter(t => t.status === col.status).map(t => t.id)} strategy={verticalListSortingStrategy}>
                  {tasks.filter(t => t.status === col.status).map(task => (
                    <TaskCard key={task.id} task={task} workspaceId={workspaceId} onTaskUpdate={loadTasks} isAdmin={isAdmin} />
                  ))}
                </SortableContext>
              </div>
            </div>
          ))}
        </div>
      </DndContext>

      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-96 max-w-[90%]">
            <h3 className="text-lg font-bold mb-4">Create New Task</h3>
            <form onSubmit={handleCreate}>
              <input type="text" placeholder="Title" required value={newTask.title} onChange={e => setNewTask({ ...newTask, title: e.target.value })} className="input mb-3" />
              <textarea placeholder="Description" rows={3} value={newTask.description} onChange={e => setNewTask({ ...newTask, description: e.target.value })} className="input mb-3" />
              <select value={newTask.priority} onChange={e => setNewTask({ ...newTask, priority: parseInt(e.target.value) })} className="input mb-3">
                <option value={1}>🔵 Low Priority</option>
                <option value={2}>🟡 Medium Priority</option>
                <option value={3}>🔴 High Priority</option>
              </select>
              <input type="date" value={newTask.dueDate} onChange={e => setNewTask({ ...newTask, dueDate: e.target.value })} className="input mb-4" />
              <div className="flex justify-end gap-3">
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary">Cancel</button>
                <button type="submit" className="btn-primary">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default KanbanBoard;