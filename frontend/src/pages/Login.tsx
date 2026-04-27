import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await login(email, password);
      navigate('/workspaces');
    } catch { } finally { setLoading(false); }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-8">
        <div className="text-center mb-8"><h1 className="text-3xl font-bold text-gray-800">SmartCollab</h1><p className="text-gray-500 mt-2">Sign in to your account</p></div>
        <form onSubmit={handleSubmit} className="space-y-5">
          <div><label className="block text-sm font-medium text-gray-700 mb-1">Email</label><input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} className="input" placeholder="you@example.com" /></div>
          <div><label className="block text-sm font-medium text-gray-700 mb-1">Password</label><input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} className="input" placeholder="••••••" /></div>
          <button type="submit" disabled={loading} className="btn-primary w-full disabled:opacity-50">{loading ? 'Signing in...' : 'Sign In'}</button>
        </form>
        <p className="text-center text-sm text-gray-600 mt-6">Don't have an account? <Link to="/register" className="text-blue-600 hover:underline">Register</Link></p>
      </div>
    </div>
  );
};

export default Login;