#!/usr/bin/env bash
# ==============================================================================
# Sensei - Collaborator Module Scaffolding Tool
# Generates a new end-to-end pluggable module (C# Component + Next.js Feature View)
# ==============================================================================

set -e

# Color definitions
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo -e "${PURPLE}================================================================${NC}"
echo -e "${PURPLE}       Sensei - Collaborator Pluggable Module Generator         ${NC}"
echo -e "${PURPLE}================================================================${NC}"

# Module Key / Slug
MODULE_SLUG="$1"
if [ -z "$MODULE_SLUG" ]; then
    read -p "Enter module key / slug (e.g., flashcard_vault, pyq_timer): " MODULE_SLUG
fi

MODULE_SLUG=$(echo "$MODULE_SLUG" | tr '[:upper:]' '[:lower:]' | tr ' -' '__')

if [ -z "$MODULE_SLUG" ]; then
    echo -e "${RED}Error: Module slug cannot be empty.${NC}"
    exit 1
fi

# Human Readable Title
MODULE_TITLE="$2"
if [ -z "$MODULE_TITLE" ]; then
    # Capitalize words
    DEFAULT_TITLE=$(echo "$MODULE_SLUG" | sed -r 's/(^|_)([a-z])/\U\2/g')
    read -p "Enter module display title [Default: $DEFAULT_TITLE]: " MODULE_TITLE
    MODULE_TITLE="${MODULE_TITLE:-$DEFAULT_TITLE}"
fi

# PascalCase class prefix
CLASS_NAME=$(echo "$MODULE_SLUG" | sed -r 's/(^|_)([a-z])/\U\2/g')

# Author
MODULE_AUTHOR="$3"
if [ -z "$MODULE_AUTHOR" ]; then
    read -p "Enter author name [Default: Collaborator]: " MODULE_AUTHOR
    MODULE_AUTHOR="${MODULE_AUTHOR:-Collaborator}"
fi

# Description
MODULE_DESC="$4"
if [ -z "$MODULE_DESC" ]; then
    read -p "Enter short description: " MODULE_DESC
    MODULE_DESC="${MODULE_DESC:-Pluggable module for GATE preparation}"
fi

echo ""
echo -e "${CYAN}Scaffolding new Sensei study module:${NC}"
echo -e "  • Slug:        ${YELLOW}$MODULE_SLUG${NC}"
echo -e "  • Class Name:  ${YELLOW}${CLASS_NAME}Component${NC}"
echo -e "  • Title:       ${YELLOW}$MODULE_TITLE${NC}"
echo -e "  • Author:      ${YELLOW}$MODULE_AUTHOR${NC}"
echo ""

# 1. Create Backend Component
BACKEND_COMP_FILE="$ROOT_DIR/backend/Components/${CLASS_NAME}Component.cs"
if [ -f "$BACKEND_COMP_FILE" ]; then
    echo -e "${YELLOW}Warning: $BACKEND_COMP_FILE already exists. Skipping backend creation.${NC}"
else
    cat <<EOF > "$BACKEND_COMP_FILE"
using System;
using System.Collections.Generic;
using Sensei.Core.Interfaces;

namespace Sensei.Components;

public interface I${CLASS_NAME}Component
{
    List<Dictionary<string, object?>> GetAllItems();
    Dictionary<string, object?>? GetItemById(int id);
    int CreateItem(string title, string content);
    bool DeleteItem(int id);
}

public class ${CLASS_NAME}Component : I${CLASS_NAME}Component
{
    private readonly IDatabaseComponent _db;

    public ${CLASS_NAME}Component(IDatabaseComponent db)
    {
        _db = db;
        EnsureDatabaseSchema();
    }

    private void EnsureDatabaseSchema()
    {
        _db.ExecuteScript(@"
            CREATE TABLE IF NOT EXISTS module_${MODULE_SLUG} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                content TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        ");
    }

    public List<Dictionary<string, object?>> GetAllItems()
    {
        return _db.Query("SELECT * FROM module_${MODULE_SLUG} ORDER BY id DESC;");
    }

    public Dictionary<string, object?>? GetItemById(int id)
    {
        return _db.QuerySingle("SELECT * FROM module_${MODULE_SLUG} WHERE id = @id;", ("@id", id));
    }

    public int CreateItem(string title, string content)
    {
        _db.ExecuteNonQuery(@"
            INSERT INTO module_${MODULE_SLUG} (title, content, created_at)
            VALUES (@title, @content, datetime('now'));
        ", ("@title", title), ("@content", content));

        return Convert.ToInt32(_db.QueryScalar<long>("SELECT last_insert_rowid();"));
    }

    public bool DeleteItem(int id)
    {
        int rows = _db.ExecuteNonQuery("DELETE FROM module_${MODULE_SLUG} WHERE id = @id;", ("@id", id));
        return rows > 0;
    }
}
EOF
    echo -e "${GREEN}✔ Created backend component:${NC} backend/Components/${CLASS_NAME}Component.cs"
fi

# 2. Create Frontend Feature Component
FRONTEND_FEATURE_DIR="$ROOT_DIR/frontend/components/features/$MODULE_SLUG"
mkdir -p "$FRONTEND_FEATURE_DIR"
FRONTEND_COMP_FILE="$FRONTEND_FEATURE_DIR/${CLASS_NAME}View.tsx"

if [ -f "$FRONTEND_COMP_FILE" ]; then
    echo -e "${YELLOW}Warning: $FRONTEND_COMP_FILE already exists. Skipping frontend creation.${NC}"
else
    cat <<EOF > "$FRONTEND_COMP_FILE"
'use client';

import React, { useState, useEffect } from 'react';
import { Layers, Plus, Trash2, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface ${CLASS_NAME}ViewProps {
  onBack?: () => void;
}

interface ${CLASS_NAME}Item {
  id: number;
  title: string;
  content: string;
  created_at: string;
}

export const ${CLASS_NAME}View: React.FC<${CLASS_NAME}ViewProps> = ({ onBack }) => {
  const [items, setItems] = useState<${CLASS_NAME}Item[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newContent, setNewContent] = useState('');
  const [loading, setLoading] = useState(false);

  // Load items from API
  const loadItems = async () => {
    try {
      const res = await fetch('/api/modules/${MODULE_SLUG}');
      if (res.ok) {
        const data = await res.json();
        setItems(data || []);
      }
    } catch (err) {
      console.error('Failed to load items:', err);
    }
  };

  useEffect(() => {
    loadItems();
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTitle.trim() || !newContent.trim()) return;

    setLoading(true);
    try {
      const res = await fetch('/api/modules/${MODULE_SLUG}', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title: newTitle, content: newContent }),
      });
      if (res.ok) {
        setNewTitle('');
        setNewContent('');
        setIsModalOpen(false);
        loadItems();
      }
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure?')) return;
    try {
      const res = await fetch(\`/api/modules/${MODULE_SLUG}/\${id}\`, { method: 'DELETE' });
      if (res.ok) {
        setItems((prev) => prev.filter((i) => i.id !== id));
      }
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="space-y-6 pb-24">
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
              <Layers className="w-6 h-6 text-accent" />
              ${MODULE_TITLE}
            </h2>
            <p className="text-xs text-zinc-400 mt-0.5">${MODULE_DESC}</p>
          </div>
        </div>

        <Button onClick={() => setIsModalOpen(true)} size="sm">
          <Plus className="w-4 h-4" />
          Add Item
        </Button>
      </div>

      {items.length === 0 ? (
        <Card className="text-center py-10 space-y-2">
          <Layers className="w-8 h-8 text-zinc-600 mx-auto" />
          <h4 className="text-sm font-bold text-white">No items yet</h4>
          <p className="text-xs text-zinc-500">Get started by creating your first record in this module.</p>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {items.map((item) => (
            <div
              key={item.id}
              className="p-4 rounded-2xl border border-surface-border bg-surface-card/60 space-y-3"
            >
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-bold text-white">{item.title}</h3>
                <button
                  onClick={() => handleDelete(item.id)}
                  className="p-1 rounded-lg text-zinc-500 hover:text-rose-400 hover:bg-zinc-800 transition"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                </button>
              </div>
              <p className="text-xs text-zinc-300 whitespace-pre-wrap">{item.content}</p>
            </div>
          ))}
        </div>
      )}

      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Item">
        <form onSubmit={handleCreate} className="space-y-3">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Title</label>
            <input
              type="text"
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              placeholder="Item title"
              required
              className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Content</label>
            <textarea
              value={newContent}
              onChange={(e) => setNewContent(e.target.value)}
              placeholder="Details or content..."
              rows={4}
              required
              className="w-full p-2.5 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" size="sm" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="primary" size="sm" disabled={loading}>
              {loading ? 'Saving...' : 'Save Item'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
EOF
    echo -e "${GREEN}✔ Created frontend view:${NC} frontend/components/features/$MODULE_SLUG/${CLASS_NAME}View.tsx"
fi

# 3. Print Registration Instructions
echo ""
echo -e "${GREEN}================================================================${NC}"
echo -e "${GREEN}✨ Module Scaffolding Completed Successfully!${NC}"
echo -e "${GREEN}================================================================${NC}"
echo -e "To wire your module into Sensei, follow these 3 simple steps:"
echo ""
echo -e "${YELLOW}Step 1: Register in backend/Program.cs:${NC}"
echo -e "  builder.Services.AddScoped<I${CLASS_NAME}Component, ${CLASS_NAME}Component>();"
echo ""
echo -e "${YELLOW}Step 2: Map Endpoints in backend/Endpoints/ApiEndpoints.cs:${NC}"
echo -e "  var ${MODULE_SLUG}Group = app.MapGroup(\"/api/modules/${MODULE_SLUG}\").WithTags(\"${CLASS_NAME}\");"
echo -e "  ${MODULE_SLUG}Group.MapGet(\"/\", (I${CLASS_NAME}Component comp) => Results.Ok(comp.GetAllItems()));"
echo -e "  ${MODULE_SLUG}Group.MapPost(\"/\", (CreateItemReq req, I${CLASS_NAME}Component comp) => Results.Ok(comp.CreateItem(req.Title, req.Content)));"
echo -e "  ${MODULE_SLUG}Group.MapDelete(\"/{id:int}\", (int id, I${CLASS_NAME}Component comp) => comp.DeleteItem(id) ? Results.Ok() : Results.NotFound());"
echo ""
echo -e "${YELLOW}Step 3: Register in backend/Components/ModuleManagerComponent.cs:${NC}"
echo -e "  (\"${MODULE_SLUG}\", \"${MODULE_TITLE}\", \"${MODULE_AUTHOR}\", \"${MODULE_DESC}\", \"Layers\")"
echo ""
echo -e "${CYAN}Read docs/COLLABORATOR_GUIDE.md for detailed architectural patterns.${NC}"
EOF

chmod +x "$ROOT_DIR/scripts/generate_module.sh"
