import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Register: React.FC = () => {
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '', confirmPassword: '' });
  const [loading, setLoading] = useState(false);
  const { register } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (form.password !== form.confirmPassword) { alert('Passwords do not match'); return; }
    setLoading(true);
    try {
      await register({ email: form.email, password: form.password, firstName: form.firstName, lastName: form.lastName });
      navigate('/workspaces');
    } catch { } finally { setLoading(false); }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-8"><h1 className="text-3xl font-bold text-center text-gray-800 mb-6">Create Account</h1>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3"><input type="text" placeholder="First Name" required value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} className="input" /><input type="text" placeholder="Last Name" required value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} className="input" /></div>
          <input type="email" placeholder="Email" required value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} className="input" />
          <input type="password" placeholder="Password" required value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} className="input" />
          <input type="password" placeholder="Confirm Password" required value={form.confirmPassword} onChange={e => setForm({ ...form, confirmPassword: e.target.value })} className="input" />
          <button type="submit" disabled={loading} className="btn-primary w-full disabled:opacity-50">{loading ? 'Creating account...' : 'Register'}</button>
        </form>
        <p className="text-center mt-4">Already have an account? <Link to="/login" className="text-blue-600 hover:underline">Sign In</Link></p>
      </div>
    </div>
  );
};

export default Register;