import { useState, useEffect } from "react";
import { getTodos, deleteTodo, addTodo, updateTodo } from "../services/todo/todoService";
import { Todo } from "../types/todo";

const useTodos = () => {
    const [todos, setTodos] = useState<Todo[]>([]);
    const [isLoading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const data = await getTodos();
                setTodos(data);
                // handle the response
                setLoading(false);
            } catch (error) {
                // handle error
            }
            finally {
                // handle finally
                setLoading(false);
            }
        };
        fetchData();
    }, []);

    const handleDelete = async (id: number) => {
        try {
            setLoading(true);
            await deleteTodo(id);

            setTodos((prevTodos) => prevTodos.filter((todo) => todo.id !== id));

        } catch (error) {
            setError("Failed to delete todo");
            console.error(error);
        }
        finally {
            setLoading(false);
        }
    };

    const handleTodoAdded = async (newTodo: Todo): Promise<void> => {
        try {
            const addedTodo = await addTodo(newTodo);

            console.log("newly added todo:", addedTodo);

            setTodos((prevTodos) => [...prevTodos, addedTodo]);

        } catch (error) {
            console.error("Failed to add todo:", error);
        }
    };

    const onCompletedChange = async (id: number) => {
        console.log('onCompletedChange', id);

        try {
            const todoToUpdate = todos.find((todo) => todo.id === id);
            if (!todoToUpdate) return;

            const updatedTodo = { ...todoToUpdate, isCompleted: !todoToUpdate.isCompleted };

            await updateTodo(id, updatedTodo);

            setTodos((prevTodos) =>
                prevTodos.map((todo) => {
                    if (todo.id === id) {
                        return { ...todo, isCompleted: !todo.isCompleted };
                    }
                    return todo;
                })
            )

            console.log("Todo updated successfully:", updatedTodo);
        } catch (error) {
            console.error("Error updating todo:", error);
        }
    };

    return { todos, isLoading, error, handleDelete, onCompletedChange, handleTodoAdded };
};

export default useTodos;
