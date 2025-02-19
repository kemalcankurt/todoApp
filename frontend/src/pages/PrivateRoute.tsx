import { Navigate } from 'react-router-dom';

interface PrivateRouteProps {
  children: React.ReactNode;
}

export const PrivateRoute = ({ children }: PrivateRouteProps) => {
  const token = localStorage.getItem('token');
  console.log('PrivateRoute rendering');

  if (!token) {
    return <Navigate to='/login' />;
  }

  return <>{children}</>;
};
