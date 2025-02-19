import React, { useState } from 'react';
import { Todo } from '../../types/todo';

interface TodoFormProps {
  handleTodoAdded: (newTodo: Todo) => Promise<void>;
}

const TodoForm: React.FC<TodoFormProps> = ({ handleTodoAdded }) => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title) return;

    const newTodo: Todo = {
      id: 0,
      title,
      description,
      isCompleted: false,
      createdDate: new Date().toISOString(),
      updatedDate: new Date().toISOString(),
    };

    try {
      await handleTodoAdded(newTodo);
      setTitle('');
      setDescription('');
    } catch (error) {
      console.error('Failed to create todo:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit} className='flex'>
      <input
        type='text'
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        required
        placeholder='Add a task'
        className='rounded-s-md grow border border-gray-400 p-2'
      />
      <button
        type='submit'
        className='w-16 rounded-e-md bg-slate-900 text-white hover:bg-slate-800'
      >
        Add
      </button>
    </form>
  );
};

export default TodoForm;
