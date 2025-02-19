import React from 'react';
import { Todo } from '../../types/todo';
import TodoItem from './TodoItem';

interface TodoListProps {
  todos: Todo[];
  onCompletedChange: (id: number, isCompleted: boolean) => void;
  handleDelete: (id: number) => void;
  isLoading: boolean;
  error: string | null;
}

const TodoList: React.FC<TodoListProps> = ({
  todos,
  onCompletedChange,
  handleDelete,
  isLoading,
  error,
}) => {
  if (error) return <p className='text-red-600 text-lg'>{error}</p>;
  if (isLoading)
    return <p className='text-gray-500 text-center'>Loading tasks...</p>;
  if (todos.length === 0)
    return <p className='text-gray-500 text-center'>No tasks yet.</p>;

  return (
    <div className={isLoading ? 'space-y-3 blur' : 'space-y-3'}>
      {todos.map((todo) => (
        <TodoItem
          key={todo.id}
          todo={todo}
          onCompletedChange={onCompletedChange}
          handleDelete={handleDelete}
        />
      ))}
    </div>
  );
};

export default TodoList;
