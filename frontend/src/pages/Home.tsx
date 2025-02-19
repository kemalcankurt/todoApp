import { useEffect } from 'react';
import useTodos from '../hooks/useTodos';
import TodoForm from '../components/todo/TodoForm';
import TodoList from '../components/todo/TodoList';

export const Home = () => {
  const {
    todos,
    handleTodoAdded,
    handleDelete,
    isLoading,
    error,
    onCompletedChange,
  } = useTodos();

  useEffect(() => {
    console.log('Home component mounted');
  }, []);

  return (
    <>
      <main className='py-10 h-screen space-y-5 overflow-y-auto'>
        <h1 className='font-bold text-3xl text-center'>Your Todos</h1>
        <div className='max-w-lg mx-auto bg-slate-100 rounded-md p-5 space-y-6'>
          <TodoForm handleTodoAdded={handleTodoAdded} />
          <TodoList
            todos={todos}
            onCompletedChange={onCompletedChange}
            handleDelete={handleDelete}
            isLoading={isLoading}
            error={error}
          />
        </div>
      </main>
    </>
  );
};
