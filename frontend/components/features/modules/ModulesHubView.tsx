import React, { useState } from 'react';
import { Layers, Plus, CheckCircle, Users, ExternalLink, Code } from 'lucide-react';
import { ModuleItem } from '@/types';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface ModulesHubViewProps {
  modules: ModuleItem[];
  onRegisterModule: (data: { moduleKey: string; title: string; author?: string; description?: string; icon?: string }) => Promise<void>;
  onToggleModule: (id: number, enabled: boolean) => Promise<void>;
  onOpenModule?: (key: string) => void;
}

export const ModulesHubView: React.FC<ModulesHubViewProps> = ({
  modules,
  onRegisterModule,
  onToggleModule,
  onOpenModule,
}) => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [key, setKey] = useState('');
  const [title, setTitle] = useState('');
  const [author, setAuthor] = useState('');
  const [desc, setDesc] = useState('');
  const [loading, setLoading] = useState(false);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!key.trim() || !title.trim()) return;

    setLoading(true);
    try {
      await onRegisterModule({
        moduleKey: key.trim().toLowerCase().replace(/\s+/g, '_'),
        title: title.trim(),
        author: author.trim() || 'Collaborator',
        description: desc.trim() || 'Community assembled module',
      });
      setKey('');
      setTitle('');
      setAuthor('');
      setDesc('');
      setIsModalOpen(false);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6 pb-20">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-extrabold text-white tracking-tight">Modules Hub</h2>
          <p className="text-xs text-zinc-400">Pluggable Component Assembly architecture</p>
        </div>
        <Button onClick={() => setIsModalOpen(true)} size="sm">
          <Plus className="w-4 h-4" />
          Plug Module
        </Button>
      </div>

      <Card className="space-y-2 bg-gradient-to-r from-purple-900/20 to-accent/10 border-accent/30">
        <div className="flex items-center gap-2">
          <Users className="w-4 h-4 text-accent" />
          <h3 className="text-sm font-bold text-white">Collaborator Assembly Model</h3>
        </div>
        <p className="text-xs text-zinc-300 leading-relaxed">
          Your friend is creating new modules? They can implement clean C# controllers and Next.js view components, then register them here with zero breaking changes to the core system.
        </p>
      </Card>

      {/* Installed Modules Grid */}
      <div className="space-y-3">
        <h3 className="text-sm font-bold text-zinc-300">Installed Modules</h3>

        {modules.map((mod) => (
          <div
            key={mod.id}
            className="p-4 rounded-xl border border-surface-border bg-surface-card/60 flex items-center justify-between gap-4"
          >
            <div className="flex items-start gap-3 min-w-0 flex-1">
              <div className="w-9 h-9 rounded-xl bg-accent/15 border border-accent/30 text-accent flex items-center justify-center shrink-0 mt-0.5">
                <Layers className="w-5 h-5" />
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <h4 className="text-sm font-bold text-white truncate">{mod.title}</h4>
                  <Badge variant={mod.enabled ? 'green' : 'zinc'}>{mod.enabled ? 'Active' : 'Disabled'}</Badge>
                </div>
                <p className="text-xs text-zinc-400 mt-0.5">{mod.description}</p>
                <div className="flex items-center gap-3 text-[11px] text-zinc-500 mt-1.5 font-mono">
                  <span>Author: {mod.author || 'Sensei Core'}</span>
                  <span>Key: {mod.moduleKey}</span>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3 shrink-0">
              {mod.enabled && onOpenModule && (
                <button
                  onClick={() => onOpenModule(mod.moduleKey)}
                  className="px-2.5 py-1 rounded-lg bg-accent/20 border border-accent/40 text-accent hover:bg-accent/30 text-xs font-semibold flex items-center gap-1 transition"
                  title="Launch Module"
                >
                  <span>Open</span>
                  <ExternalLink className="w-3 h-3" />
                </button>
              )}

              <button
                onClick={() => onToggleModule(mod.id, !mod.enabled)}
                className={`w-11 h-6 rounded-full transition-colors relative p-0.5 shrink-0 ${
                  mod.enabled ? 'bg-accent' : 'bg-zinc-800'
                }`}
              >
                <div
                  className={`w-5 h-5 rounded-full bg-white transition-transform ${
                    mod.enabled ? 'translate-x-5' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>
          </div>
        ))}
      </div>

      {/* Plug New Module Modal */}
      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Plug New Study Module">
        <form onSubmit={handleRegister} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Module Key (unique identifier)</label>
            <input
              type="text"
              value={key}
              onChange={(e) => setKey(e.target.value)}
              placeholder="e.g. pyq_analytics_plugin"
              autoFocus
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Module Title</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. PYQ Difficulty Heatmap"
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Author / Collaborator</label>
            <input
              type="text"
              value={author}
              onChange={(e) => setAuthor(e.target.value)}
              placeholder="e.g. Parth's Friend"
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Description</label>
            <textarea
              value={desc}
              onChange={(e) => setDesc(e.target.value)}
              placeholder="What does this module do?"
              rows={2}
              className="w-full p-2.5 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" size="sm" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="primary" size="sm" disabled={loading || !key.trim() || !title.trim()}>
              {loading ? 'Registering...' : 'Register Module'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
