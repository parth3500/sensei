# 🛠️ Sensei — Collaborator & Module Developer Guide

Welcome to the **Sensei** contributor ecosystem. This guide documents how to design, scaffold, implement, and register new pluggable modules into the Sensei GATE preparation platform without causing breaking changes to the core system.

---

## 🏛️ Architecture Overview

Sensei is built around the **Component Assembly Architecture**:
- **Backend**: C# (.NET 8.0 Minimal API) running on `http://localhost:5000`.
- **Database**: Zero-NuGet direct P/Invoke to `/usr/lib/x86_64-linux-gnu/libsqlite3.so.0`.
  - Bypasses external NuGet registry dependencies and corporate proxy firewalls.
  - Parameterized queries and statement preparation via `IDatabaseComponent`.
- **Frontend**: Next.js 14 App Router with Tailwind CSS dark-theme UI and Lucide icons running on `http://localhost:3000`.
- **Module Discovery Hub**: Dynamic collaborator module registry backed by the `study_modules` table.

---

## ⚡ Rapid Scaffolding (10-Second Quickstart)

We provide a built-in CLI scaffolding utility that automatically creates both the C# backend component and the Next.js frontend feature view:

```bash
# Interactive mode:
./scripts/generate_module.sh

# Or pass parameters directly:
# ./scripts/generate_module.sh <slug> "<Title>" "<Author>" "<Description>"
./scripts/generate_module.sh pyq_analyzer "PYQ Difficulty Analyzer" "Parth" "Analyze question difficulty distributions"
```

### What gets generated:
1. **Backend Component**: `backend/Components/<ModuleName>Component.cs`
   - Contains `I<ModuleName>Component` interface
   - SQLite table initialization (`CREATE TABLE IF NOT EXISTS module_<slug> ...`)
   - CRUD methods with parameterized queries
2. **Frontend Feature View**: `frontend/components/features/<slug>/<ModuleName>View.tsx`
   - Dark-mode responsive card layout
   - Integration with `/api/modules/<slug>`
   - Creation Modal and delete actions

---

## 🧩 Step-by-Step: Anatomy of a Pluggable Module

To understand how a module is assembled end-to-end, examine the **Formula & Theorem Vault** reference module:

### 1. Backend Component & Interface (`backend/Components/FormulaVaultComponent.cs`)

Define an interface and implement the component using `IDatabaseComponent`:

```csharp
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public interface IFormulaVaultComponent
{
    List<FormulaItem> GetFormulas(string? category, string? search);
    List<string> GetCategories();
    FormulaItem? GetFormulaById(int id);
    FormulaItem CreateFormula(CreateFormulaRequest request);
    bool DeleteFormula(int id);
}

public class FormulaVaultComponent : IFormulaVaultComponent
{
    private readonly IDatabaseComponent _db;

    public FormulaVaultComponent(IDatabaseComponent db)
    {
        _db = db;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        _db.ExecuteScript(@"
            CREATE TABLE IF NOT EXISTS formula_vault (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                category TEXT NOT NULL,
                title TEXT NOT NULL,
                formula TEXT NOT NULL,
                description TEXT,
                key_variables TEXT,
                example TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        ");
    }
    // ... CRUD methods using _db.Query, _db.ExecuteNonQuery with parameters
}
```

### 2. Dependency Injection Registration (`backend/Program.cs`)

Add your component to the IoC container:

```csharp
builder.Services.AddScoped<IFormulaVaultComponent, FormulaVaultComponent>();
```

### 3. Minimal API Endpoint Mapping (`backend/Endpoints/ApiEndpoints.cs`)

Group your endpoints under `/api/modules/<your-module>`:

```csharp
var formulaVault = app.MapGroup("/api/modules/formula-vault").WithTags("FormulaVault");

formulaVault.MapGet("/", (string? category, string? search, IFormulaVaultComponent vault) =>
{
    return Results.Ok(vault.GetFormulas(category, search));
});

formulaVault.MapPost("/", (CreateFormulaRequest req, IFormulaVaultComponent vault) =>
{
    var created = vault.CreateFormula(req);
    return Results.Created($"/api/modules/formula-vault/{created.Id}", created);
});

formulaVault.MapDelete("/{id:int}", (int id, IFormulaVaultComponent vault) =>
{
    return vault.DeleteFormula(id) ? Results.Ok() : Results.NotFound();
});
```

### 4. Registry Entry (`backend/Components/ModuleManagerComponent.cs`)

Ensure your module appears in the Modules Hub by listing it in default modules or inserting into `study_modules`:

```csharp
("formula_vault", "Formula & Theorem Vault", "Community", "Quick reference sheet for Math, Algo & OS formulas", "BookMarked")
```

### 5. Frontend Client Service (`frontend/services/api.ts`)

Export type-safe methods in `frontend/services/api.ts`:

```typescript
async getFormulas(category?: string, search?: string): Promise<FormulaItem[]> {
  const params = new URLSearchParams();
  if (category) params.append('category', category);
  if (search) params.append('search', search);
  return fetchJson(`${API_BASE}/modules/formula-vault?${params.toString()}`);
},
async createFormula(data: CreateFormulaRequest): Promise<FormulaItem> {
  return fetchJson(`${API_BASE}/modules/formula-vault`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
```

### 6. Frontend View Component (`frontend/components/features/vault/FormulaVaultView.tsx`)

Build your component using existing UI primitives (`Card`, `Button`, `Badge`, `Modal`):
- Include search/filter controls.
- Style formulas with code fonts (`font-mono text-accent`).
- Provide an "Add" modal for new records.

---

## 🔒 Safe Database Conventions

1. **Never concatenate SQL strings directly**:
   ```csharp
   // ❌ BAD: SQL injection risk
   _db.Query($"SELECT * FROM items WHERE name = '{name}';");

   // ✅ GOOD: Parameterized binding
   _db.Query("SELECT * FROM items WHERE name = @name;", ("@name", name));
   ```
2. **Always use `IF NOT EXISTS`** for table and index creation in `EnsureInitialized()`.
3. **Foreign Keys**: Index columns used in joins (`CREATE INDEX IF NOT EXISTS ...`).

---

## 🎨 Frontend Styling Conventions

Sensei uses a dark cyberpunk/obsidian aesthetic:
- **Background**: `#090a0f`
- **Surface**: `bg-surface-card/60 border border-surface-border`
- **Accent**: `text-accent`, `bg-accent` (Violet/Purple `#8b5cf6`)
- **Badges**: `<Badge variant="green | purple | red | yellow | blue | zinc">`
- **Buttons**: `<Button variant="primary | secondary | ghost" size="sm | md">`

---

## 🧪 Testing and Verification

Before committing your module:

```bash
# 1. Build C# Backend
export DOTNET_ROOT="/home/parth/prog/ai/dotnet/usr/lib/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
cd backend && dotnet build

# 2. Build Next.js Frontend & Check Types
cd ../frontend && npm run build

# 3. Launch Services
cd .. && ./start.sh
```

Verify in browser:
- Open `http://localhost:3000`
- Click the **Modules Hub** (Layers icon in header)
- Check that your module is listed, toggles on/off, and opens without errors.
