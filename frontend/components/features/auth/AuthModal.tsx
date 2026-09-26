import React, { useState } from 'react';
import { KeyRound, Lock, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface AuthModalProps {
  isOpen: boolean;
  onSuccess: () => void;
  onLogin: (passphrase: string) => Promise<void>;
}

export const AuthModal: React.FC<AuthModalProps> = ({ isOpen, onSuccess, onLogin }) => {
  const [passphrase, setPassphrase] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!passphrase.trim()) return;

    setLoading(true);
    setError(null);
    try {
      await onLogin(passphrase);
      onSuccess();
    } catch (err: any) {
      setError(err.message || 'Invalid passphrase. Please verify your credentials.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md">
      <div className="w-full max-w-sm bg-surface border border-surface-border p-6 rounded-2xl shadow-2xl space-y-5 animate-scale-up">
        <div className="text-center space-y-2">
          <div className="w-12 h-12 rounded-2xl bg-accent/15 border border-accent/30 mx-auto flex items-center justify-center text-accent">
            <Lock className="w-6 h-6" />
          </div>
          <h2 className="text-xl font-bold text-white tracking-tight">Welcome to Sensei</h2>
          <p className="text-xs text-zinc-400">Enter your secure passphrase to access your GATE prep dashboard.</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-zinc-300 flex items-center gap-1.5">
              <KeyRound className="w-3.5 h-3.5 text-accent" />
              Passphrase
            </label>
            <input
              type="password"
              value={passphrase}
              onChange={(e) => setPassphrase(e.target.value)}
              placeholder="Enter passphrase..."
              autoFocus
              className="w-full px-4 py-2.5 rounded-xl bg-zinc-900 border border-zinc-800 text-white placeholder-zinc-600 focus:outline-none focus:border-accent text-sm transition"
            />
          </div>

          {error && (
            <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 flex items-center gap-2 text-xs text-red-400">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <Button type="submit" variant="primary" className="w-full" disabled={loading || !passphrase.trim()}>
            {loading ? 'Authenticating...' : 'Unlock Dashboard'}
          </Button>

          <p className="text-[11px] text-center text-zinc-500">
            Single-user authentication powered by C# ASP.NET Core
          </p>
        </form>
      </div>
    </div>
  );
};
