import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';

import './App.css';

import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

import { PrivateRoute } from './pages/PrivateRoute';
import { Home } from './pages/Home';
import { Login } from './pages/Login';

function App() {
  return (
    <>
      <ToastContainer position='top-right' autoClose={3000} hideProgressBar />

      <Router>
        <Routes>
          <Route path='/login' element={<Login />} />
          <Route
            path='/home'
            element={
              <PrivateRoute>
                <Home />
              </PrivateRoute>
            }
          />
          <Route path='/' element={<Login />} />
        </Routes>
      </Router>
    </>
  );
}

export default App;
