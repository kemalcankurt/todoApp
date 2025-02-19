import React from 'react';
import { Trash2 } from 'lucide-react';
import { Todo } from '../../types/todo';

interface TodoItemProps {
  todo: Todo;
  onCompletedChange: (id: number, isCompleted: boolean) => void;
  handleDelete: (id: number) => void;
}

const TodoItem: React.FC<TodoItemProps> = ({
  todo,
  onCompletedChange,
  handleDelete,
}) => {
  return (
    <div className='flex items-center gap-1'>
      <label className='flex items-center gap-2 border rounded-md p-2 border-gray-400 bg-white hover:bg-slate-50 grow cursor-pointer'>
        <input
          type='checkbox'
          checked={todo.isCompleted}
          onChange={(e) => onCompletedChange(todo.id, e.target.checked)}
          className='scale-125'
        />
        <span className={todo.isCompleted ? 'line-through text-gray-400' : ''}>
          {todo.title}
        </span>
      </label>
      <button onClick={() => handleDelete(todo.id)} className='p-2'>
        <Trash2 size={20} className='text-gray-500' />
      </button>
    </div>
  );
};

export default TodoItem;
