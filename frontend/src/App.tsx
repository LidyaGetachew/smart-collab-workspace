import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import Login from './pages/Login';
import Register from './pages/Register';
import WorkspaceList from './pages/WorkspaceList';
import WorkspaceDetail from './pages/WorkspaceDetail';

const PrivateRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, loading } = useAuth();
  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }
  return user ? <>{children}</> : <Navigate to="/login" />;
};

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Toaster position="top-right" toastOptions={{ duration: 3000 }} />
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/workspaces" element={
            <PrivateRoute>
              <WorkspaceList />
            </PrivateRoute>
          } />
          <Route path="/workspace/:id" element={
            <PrivateRoute>
              <WorkspaceDetail />
            </PrivateRoute>
          } />
          <Route path="/" element={<Navigate to="/workspaces" />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;