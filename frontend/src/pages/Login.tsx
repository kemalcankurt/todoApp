import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/axiosInstance';

interface LoginForm {
  email: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
}

export const Login = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState<LoginForm>({
    email: '',
    password: '',
  });
  const [error, setError] = useState<string>('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const response = await api.post<LoginResponse>('/user/login', formData);

      localStorage.setItem('token', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);

      navigate('/home');
    } catch (err: any) {
      console.error('Login error:', err);
      setError(
        err.response?.data?.message ||
          'Unable to connect to the server. Please try again.'
      );
    }
  };

  return (
    <div className='min-h-screen flex items-center justify-center bg-gray-50'>
      <div className='max-w-md w-full space-y-8 p-8 bg-white rounded-lg shadow'>
        <h2 className='text-center text-3xl font-bold'>Sign in</h2>
        {error && <div className='text-red-500 text-center'>{error}</div>}
        <form onSubmit={handleSubmit} className='space-y-6'>
          <div>
            <input
              type='email'
              placeholder='Email'
              value={formData.email}
              onChange={(e) =>
                setFormData({ ...formData, email: e.target.value })
              }
              className='w-full px-3 py-2 border rounded-md'
            />
          </div>
          <div>
            <input
              type='password'
              placeholder='Password'
              value={formData.password}
              onChange={(e) =>
                setFormData({ ...formData, password: e.target.value })
              }
              className='w-full px-3 py-2 border rounded-md'
            />
          </div>
          <button
            type='submit'
            className='w-full py-2 px-4 bg-blue-600 text-white rounded-md hover:bg-blue-700'
          >
            Sign in
          </button>
        </form>
      </div>
    </div>
  );
};
