
import api from "./../../api/axiosInstance";

export const getTodos = async () => {
    try {
        const response = await api.get("/todo");
        return response.data;
    } catch (error) {
        console.error("Error fetching todos:", error);
        throw error;
    }
};

export const addTodo = async (todo: { title: string; description: string }) => {
    try {
        const response = await api.post("/todo", todo);
        return response.data;
    } catch (error) {
        console.error("Error adding todo:", error);
        throw error;
    }
};

export const updateTodo = async (id: number, updatedData: object) => {
    try {
        const response = await api.put(`/todo/${id}`, updatedData);
        return response.data;
    } catch (error) {
        console.error("Error updating todo:", error);
        throw error;
    }
};

export const deleteTodo = async (id: number) => {
    try {
        const response = await api.delete(`/todo/${id}`);
        return response.data;
    } catch (error) {
        console.error("Error deleting todo:", error);
        throw error;
    }
};
