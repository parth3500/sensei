import React from 'react';
import { Bell, Shield, LogOut, Database, Cpu } from 'lucide-react';
import { ReminderItem } from '@/types';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';

interface SettingsViewProps {
  reminders: ReminderItem[];
  onToggleReminder: (id: number, enabled: boolean) => Promise<void>;
  onLogout: () => Promise<void>;
}

export const SettingsView: React.FC<SettingsViewProps> = ({
  reminders,
  onToggleReminder,
  onLogout,
}) => {
  return (
    <div className="space-y-6 pb-20">
      <div>
        <h2 className="text-2xl font-extrabold text-white tracking-tight">Settings</h2>
        <p className="text-xs text-zinc-400">System configuration and notifications</p>
      </div>

      {/* Reminders Card */}
      <Card className="space-y-4">
        <div className="flex items-center gap-2">
          <Bell className="w-4 h-4 text-accent" />
          <h3 className="text-sm font-bold text-white">Daily Reminders</h3>
        </div>

        <div className="space-y-3">
          {reminders.map((r) => (
            <div
              key={r.id}
              className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/60 border border-surface-border"
            >
              <div>
                <p className="text-xs font-semibold text-zinc-200">{r.title}</p>
                <p className="text-[11px] text-zinc-500 font-mono">Cron: {r.cron}</p>
              </div>

              <button
                onClick={() => onToggleReminder(r.id, !r.enabled)}
                className={`w-11 h-6 rounded-full transition-colors relative p-0.5 ${
                  r.enabled ? 'bg-accent' : 'bg-zinc-800'
                }`}
              >
                <div
                  className={`w-5 h-5 rounded-full bg-white transition-transform ${
                    r.enabled ? 'translate-x-5' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>
          ))}
        </div>
      </Card>

      {/* Architecture Card */}
      <Card className="space-y-3">
        <h3 className="text-sm font-bold text-white flex items-center gap-2">
          <Cpu className="w-4 h-4 text-accent" />
          Architecture &amp; Assembly
        </h3>
        <div className="space-y-2 text-xs text-zinc-400">
          <div className="flex justify-between py-1 border-b border-zinc-800">
            <span>Model</span>
            <span className="font-semibold text-zinc-200">Component Assembly Model (CBSE)</span>
          </div>
          <div className="flex justify-between py-1 border-b border-zinc-800">
            <span>Backend</span>
            <span className="font-semibold text-accent">C# (.NET 8.0 Web API)</span>
          </div>
          <div className="flex justify-between py-1 border-b border-zinc-800">
            <span>Frontend</span>
            <span className="font-semibold text-accent">Next.js 14 + React 18</span>
          </div>
          <div className="flex justify-between py-1 border-b border-zinc-800">
            <span>Persistence</span>
            <span className="font-semibold text-zinc-200">SQLite 3 (WAL Mode, zero-config)</span>
          </div>
        </div>
      </Card>

      {/* Logout */}
      <Card className="border-red-500/20 bg-red-500/5 space-y-3">
        <h3 className="text-sm font-bold text-red-400 flex items-center gap-2">
          <Shield className="w-4 h-4 text-red-400" />
          Session Security
        </h3>
        <p className="text-xs text-zinc-400 leading-relaxed">
          Log out of this browser session to clear your authentication cookie.
        </p>
        <Button variant="danger" onClick={onLogout} className="w-full">
          <LogOut className="w-4 h-4" />
          Log Out
        </Button>
      </Card>
    </div>
  );
};
