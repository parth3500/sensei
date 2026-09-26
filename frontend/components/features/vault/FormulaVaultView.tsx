'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  BookMarked,
  Search,
  Plus,
  Trash2,
  Tag,
  Code,
  Sparkles,
  Info,
  ChevronRight,
  ArrowLeft,
} from 'lucide-react';
import { FormulaItem, CreateFormulaRequest } from '@/types';
import { api } from '@/services/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface FormulaVaultViewProps {
  onBack?: () => void;
}

export const FormulaVaultView: React.FC<FormulaVaultViewProps> = ({ onBack }) => {
  const [formulas, setFormulas] = useState<FormulaItem[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);

  // New Formula Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [newCategory, setNewCategory] = useState('');
  const [newTitle, setNewTitle] = useState('');
  const [newFormula, setNewFormula] = useState('');
  const [newDescription, setNewDescription] = useState('');
  const [newKeyVariables, setNewKeyVariables] = useState('');
  const [newExample, setNewExample] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Load Categories & Formulas
  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [cats, items] = await Promise.all([
        api.getFormulaCategories(),
        api.getFormulas(selectedCategory === 'all' ? undefined : selectedCategory, searchQuery || undefined),
      ]);
      setCategories(cats || []);
      setFormulas(items || []);
    } catch (err) {
      console.error('Failed to load formulas:', err);
    } finally {
      setLoading(false);
    }
  }, [selectedCategory, searchQuery]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleCreateFormula = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTitle.trim() || !newFormula.trim() || !newCategory.trim()) return;

    setSubmitting(true);
    try {
      await api.createFormula({
        category: newCategory.trim(),
        title: newTitle.trim(),
        formula: newFormula.trim(),
        description: newDescription.trim() || undefined,
        keyVariables: newKeyVariables.trim() || undefined,
        example: newExample.trim() || undefined,
      });

      setNewTitle('');
      setNewFormula('');
      setNewDescription('');
      setNewKeyVariables('');
      setNewExample('');
      setIsModalOpen(false);
      loadData();
    } catch (err: any) {
      alert('Failed to save formula: ' + err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteFormula = async (id: number) => {
    if (!confirm('Are you sure you want to delete this formula?')) return;
    try {
      await api.deleteFormula(id);
      setFormulas((prev) => prev.filter((f) => f.id !== id));
    } catch (err: any) {
      alert('Failed to delete formula: ' + err.message);
    }
  };

  return (
    <div className="space-y-6 pb-24">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          {onBack && (
            <button
              onClick={onBack}
              className="p-1.5 rounded-xl border border-surface-border bg-surface-card hover:text-white text-zinc-400 transition"
            >
              <ArrowLeft className="w-4 h-4" />
            </button>
          )}
          <div>
            <h2 className="text-2xl font-black text-white tracking-tight flex items-center gap-2">
              <BookMarked className="w-6 h-6 text-accent" />
              Formula & Theorem Vault
            </h2>
            <p className="text-xs text-zinc-400 mt-0.5">
              High-yield GATE formulas, recurrences, and quick reference cheatsheets
            </p>
          </div>
        </div>

        <Button onClick={() => setIsModalOpen(true)} size="sm">
          <Plus className="w-4 h-4" />
          Add Formula
        </Button>
      </div>

      {/* Search & Filter Controls */}
      <div className="space-y-3">
        {/* Search Bar */}
        <div className="relative">
          <Search className="w-4 h-4 text-zinc-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search formulas, theorems (e.g. Master Theorem, Shannon, TLB)..."
            className="w-full pl-10 pr-4 py-2.5 rounded-xl bg-surface-card border border-surface-border text-xs text-white placeholder-zinc-500 focus:outline-none focus:border-accent"
          />
        </div>

        {/* Categories Bar */}
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none">
          <button
            onClick={() => setSelectedCategory('all')}
            className={`px-3 py-1 rounded-xl text-xs whitespace-nowrap transition ${
              selectedCategory === 'all'
                ? 'bg-accent text-white font-semibold'
                : 'bg-zinc-800 text-zinc-400 hover:text-white'
            }`}
          >
            All Formulas ({formulas.length})
          </button>
          {categories.map((cat) => (
            <button
              key={cat}
              onClick={() => setSelectedCategory(cat)}
              className={`px-3 py-1 rounded-xl text-xs whitespace-nowrap transition ${
                selectedCategory === cat
                  ? 'bg-accent text-white font-semibold'
                  : 'bg-zinc-800 text-zinc-400 hover:text-white'
              }`}
            >
              {cat}
            </button>
          ))}
        </div>
      </div>

      {/* Formula Cards Grid */}
      {loading ? (
        <div className="text-center py-12 text-zinc-500 text-xs font-mono">Loading formula repository...</div>
      ) : formulas.length === 0 ? (
        <Card className="text-center py-10 space-y-2">
          <BookMarked className="w-8 h-8 text-zinc-600 mx-auto" />
          <h4 className="text-sm font-bold text-white">No formulas matched your search</h4>
          <p className="text-xs text-zinc-500">Try adjusting your keyword filter or add a new formula.</p>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {formulas.map((item) => (
            <div
              key={item.id}
              className="p-4 rounded-2xl border border-surface-border bg-surface-card/70 hover:border-zinc-700 transition flex flex-col justify-between space-y-3"
            >
              <div>
                <div className="flex items-start justify-between gap-2 mb-2">
                  <Badge variant="purple">{item.category}</Badge>
                  <button
                    onClick={() => handleDeleteFormula(item.id)}
                    className="p-1 rounded-lg text-zinc-500 hover:text-rose-400 hover:bg-zinc-800 transition"
                    title="Delete formula"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>

                <h3 className="text-sm font-bold text-white mb-2">{item.title}</h3>

                {/* Highlighted Formula Box */}
                <div className="p-3 rounded-xl bg-zinc-900 border border-zinc-800/80 font-mono text-xs sm:text-sm text-accent font-semibold overflow-x-auto whitespace-pre-wrap select-all">
                  {item.formula}
                </div>

                {item.description && (
                  <p className="text-xs text-zinc-300 mt-2.5 leading-relaxed">{item.description}</p>
                )}
              </div>

              {(item.keyVariables || item.example) && (
                <div className="pt-2 border-t border-surface-border space-y-1.5 text-[11px]">
                  {item.keyVariables && (
                    <div className="text-zinc-400">
                      <span className="font-semibold text-zinc-300">Variables: </span>
                      {item.keyVariables}
                    </div>
                  )}
                  {item.example && (
                    <div className="text-zinc-400 bg-zinc-900/40 p-2 rounded-lg border border-zinc-800/50">
                      <span className="font-semibold text-accent">Example: </span>
                      {item.example}
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Add Formula Modal */}
      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Add Formula to Vault">
        <form onSubmit={handleCreateFormula} className="space-y-3">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Subject / Category</label>
            <input
              type="text"
              value={newCategory}
              onChange={(e) => setNewCategory(e.target.value)}
              placeholder="e.g. Computer Networks, OS, Algorithms"
              required
              className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Formula Title</label>
            <input
              type="text"
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              placeholder="e.g. Nyquist Bit Rate Formula"
              required
              className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Formula Equation / Statement</label>
            <input
              type="text"
              value={newFormula}
              onChange={(e) => setNewFormula(e.target.value)}
              placeholder="e.g. BitRate = 2 * Bandwidth * log2(L)"
              required
              className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 font-mono text-xs text-accent placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Description (Optional)</label>
            <textarea
              value={newDescription}
              onChange={(e) => setNewDescription(e.target.value)}
              placeholder="Theoretical context and application rules"
              rows={2}
              className="w-full p-2.5 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Key Variables / Notation (Optional)</label>
            <input
              type="text"
              value={newKeyVariables}
              onChange={(e) => setNewKeyVariables(e.target.value)}
              placeholder="e.g. B = Bandwidth in Hz, L = Number of signal levels"
              className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Example / PYQ Application (Optional)</label>
            <input
              type="text"
              value={newExample}
              onChange={(e) => setNewExample(e.target.value)}
              placeholder="e.g. Bandwidth = 3000 Hz, 8 levels: 2 * 3000 * 3 = 18 kbps"
              className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" size="sm" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="primary" size="sm" disabled={submitting}>
              {submitting ? 'Saving...' : 'Add to Vault'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
