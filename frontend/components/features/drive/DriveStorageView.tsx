import React, { useState, useRef } from 'react';
import {
  FolderGit2,
  Upload,
  FileText,
  CheckCircle2,
  Database,
  Sparkles,
  BookOpen,
  FileUp,
  X,
  AlertCircle,
  FileCode,
  Zap,
} from 'lucide-react';
import { DriveFileItem, DriveProcessResult } from '@/types';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';

interface DriveStorageViewProps {
  driveStatus: any;
  files: DriveFileItem[];
  onIngestFile: (data: { fileName: string; content: string }) => Promise<DriveProcessResult>;
  onBackup: () => Promise<{ success: boolean; backupPath: string }>;
}

const ALLOWED_EXTENSIONS = ['.txt', '.md', '.markdown', '.json'];

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

export const DriveStorageView: React.FC<DriveStorageViewProps> = ({
  driveStatus,
  files,
  onIngestFile,
  onBackup,
}) => {
  const [fileName, setFileName] = useState('');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);
  const [backupLoading, setBackupLoading] = useState(false);
  const [lastResult, setLastResult] = useState<DriveProcessResult | null>(null);
  const [backupMsg, setBackupMsg] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [autoIngestOnDrop, setAutoIngestOnDrop] = useState(false);
  const [loadedFileInfo, setLoadedFileInfo] = useState<{
    name: string;
    size: string;
    lines: number;
  } | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);

  const isSupportedFile = (file: File): boolean => {
    const name = file.name.toLowerCase();
    return ALLOWED_EXTENSIONS.some((ext) => name.endsWith(ext));
  };

  const readFileContent = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (event) => {
        const text = event.target?.result;
        if (typeof text === 'string') {
          resolve(text);
        } else {
          reject(new Error('Unable to read text file.'));
        }
      };
      reader.onerror = () => reject(new Error('Failed to read file from disk.'));
      reader.readAsText(file);
    });
  };

  const executeIngest = async (nameToIngest: string, contentToIngest: string) => {
    if (!nameToIngest.trim() || !contentToIngest.trim()) return;

    setLoading(true);
    setLastResult(null);
    setErrorMsg(null);
    try {
      const res = await onIngestFile({
        fileName: nameToIngest.trim(),
        content: contentToIngest.trim(),
      });
      setLastResult(res);
      setFileName('');
      setContent('');
      setLoadedFileInfo(null);
    } catch (err: any) {
      setErrorMsg(err?.message || 'Failed to ingest file.');
    } finally {
      setLoading(false);
    }
  };

  const handleProcessFile = async (file: File) => {
    if (!isSupportedFile(file)) {
      setErrorMsg(`"${file.name}" is not supported. Please upload a .txt, .md, or .json file.`);
      return;
    }

    setErrorMsg(null);
    try {
      const text = await readFileContent(file);
      const lines = text.split('\n').length;
      setFileName(file.name);
      setContent(text);
      setLoadedFileInfo({
        name: file.name,
        size: formatFileSize(file.size),
        lines,
      });

      if (autoIngestOnDrop) {
        await executeIngest(file.name, text);
      }
    } catch (err: any) {
      setErrorMsg(err?.message || 'Error processing selected file.');
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragEnter = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  };

  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    const droppedFiles = e.dataTransfer.files;
    if (droppedFiles && droppedFiles.length > 0) {
      await handleProcessFile(droppedFiles[0]);
    }
  };

  const handleFileInputChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = e.target.files;
    if (selectedFiles && selectedFiles.length > 0) {
      await handleProcessFile(selectedFiles[0]);
    }
    e.target.value = '';
  };

  const handleClear = () => {
    setFileName('');
    setContent('');
    setLoadedFileInfo(null);
    setErrorMsg(null);
  };

  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeIngest(fileName, content);
  };

  const handleBackupNow = async () => {
    setBackupLoading(true);
    setBackupMsg(null);
    try {
      const res = await onBackup();
      setBackupMsg(`Database snapshot successfully backed up to ${res.backupPath}`);
    } catch {
      setBackupMsg('Failed to backup database.');
    } finally {
      setBackupLoading(false);
    }
  };

  return (
    <div className="space-y-6 pb-20">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-extrabold text-white tracking-tight">Google Drive Storage &amp; Ingest</h2>
        <p className="text-xs text-zinc-400">
          Auto-summarize notes, extract key revision points, and generate practice questions
        </p>
      </div>

      {/* Drive Status & Backup Card */}
      <Card className="space-y-3.5 bg-surface-card/80">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-xl bg-blue-500/10 text-blue-400 border border-blue-500/20 flex items-center justify-center">
              <FolderGit2 className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white">Drive Folder: GATE_Sensei_Notes/</h3>
              <p className="text-[11px] text-zinc-500">Live sync folder for revision documents and database snapshots</p>
            </div>
          </div>
          <Badge variant="green">Active</Badge>
        </div>

        <div className="flex items-center justify-between pt-2 border-t border-zinc-800">
          <div className="text-xs text-zinc-400">
            <span>Durable persistence: </span>
            <span className="text-zinc-200 font-mono">SQLite WAL + Drive Backup</span>
          </div>
          <Button onClick={handleBackupNow} variant="secondary" size="sm" disabled={backupLoading}>
            <Database className="w-3.5 h-3.5 text-accent" />
            {backupLoading ? 'Backing up...' : 'Backup DB Now'}
          </Button>
        </div>

        {backupMsg && (
          <p className="text-xs text-emerald-400 p-2.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20">
            {backupMsg}
          </p>
        )}
      </Card>

      {/* Ingest Note Box */}
      <Card className="space-y-4">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2">
            <Upload className="w-4 h-4 text-accent" />
            <h3 className="text-sm font-bold text-white">Ingest Study Note into Google Drive</h3>
          </div>
          <label className="flex items-center gap-2 cursor-pointer text-xs text-zinc-400 hover:text-zinc-200 transition select-none">
            <input
              type="checkbox"
              checked={autoIngestOnDrop}
              onChange={(e) => setAutoIngestOnDrop(e.target.checked)}
              className="rounded accent-purple-600 cursor-pointer"
            />
            <span className="flex items-center gap-1">
              <Zap className="w-3 h-3 text-amber-400" />
              Auto-ingest on file drop
            </span>
          </label>
        </div>

        {/* Hidden File Input */}
        <input
          ref={fileInputRef}
          type="file"
          accept=".txt,.md,.markdown,.json,text/plain,text/markdown,application/json"
          onChange={handleFileInputChange}
          className="hidden"
        />

        {/* Drag and Drop Zone */}
        <div
          onDragOver={handleDragOver}
          onDragEnter={handleDragEnter}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          className={`border-2 border-dashed rounded-2xl p-6 text-center cursor-pointer transition-all duration-200 ${
            isDragging
              ? 'border-accent bg-accent/15 scale-[1.01] shadow-lg shadow-accent/10'
              : 'border-zinc-700/80 hover:border-accent/60 bg-zinc-900/40 hover:bg-zinc-900/70'
          }`}
        >
          <div className="flex flex-col items-center justify-center gap-2">
            <div
              className={`w-12 h-12 rounded-2xl flex items-center justify-center transition-all ${
                isDragging ? 'bg-accent text-white scale-110' : 'bg-accent/10 text-accent border border-accent/20'
              }`}
            >
              <FileUp className="w-6 h-6" />
            </div>
            <div>
              <p className="text-sm font-semibold text-zinc-200">
                {isDragging ? 'Release to drop your note here' : 'Drop your study note here, or click to browse'}
              </p>
              <p className="text-[11px] text-zinc-500 mt-0.5">
                Supports <span className="text-zinc-400 font-mono">.txt</span>,{' '}
                <span className="text-zinc-400 font-mono">.md</span>, and{' '}
                <span className="text-zinc-400 font-mono">.json</span> files
              </p>
            </div>
          </div>
        </div>

        {/* Selected file status banner */}
        {loadedFileInfo && (
          <div className="p-3 rounded-xl bg-purple-500/10 border border-purple-500/20 flex items-center justify-between gap-3 text-xs animate-fade-in">
            <div className="flex items-center gap-2.5 min-w-0">
              <FileCode className="w-4 h-4 text-accent shrink-0" />
              <div className="truncate">
                <span className="font-semibold text-zinc-200 truncate">{loadedFileInfo.name}</span>
                <span className="text-zinc-400 ml-2">
                  ({loadedFileInfo.size} &bull; {loadedFileInfo.lines} lines)
                </span>
              </div>
            </div>
            <div className="flex items-center gap-2 shrink-0">
              <Badge variant="purple">Ready to Ingest</Badge>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  handleClear();
                }}
                className="text-zinc-400 hover:text-rose-400 p-1 rounded-lg hover:bg-zinc-800/80 transition"
                title="Clear selected file"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}

        {/* Error message */}
        {errorMsg && (
          <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-300 text-xs flex items-center gap-2 animate-fade-in">
            <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
            <span>{errorMsg}</span>
          </div>
        )}

        {/* Divider for manual entry */}
        <div className="relative flex py-1 items-center">
          <div className="flex-grow border-t border-zinc-800"></div>
          <span className="flex-shrink mx-3 text-[11px] uppercase tracking-wider text-zinc-500 font-semibold">
            Or review / paste text manually
          </span>
          <div className="flex-grow border-t border-zinc-800"></div>
        </div>

        {/* Ingest Form */}
        <form onSubmit={handleFormSubmit} className="space-y-3">
          <div>
            <label className="text-xs font-semibold text-zinc-300">File Name (with topic)</label>
            <input
              type="text"
              value={fileName}
              onChange={(e) => setFileName(e.target.value)}
              placeholder="e.g. Computer_Networks_TCP_Congestion.txt"
              className="w-full mt-1 px-3.5 py-2 rounded-xl bg-zinc-900 border border-surface-border text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div>
            <label className="text-xs font-semibold text-zinc-300">Note Content / Transcript</label>
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Paste summary, lecture notes, textbook excerpt, or formulas..."
              rows={4}
              className="w-full mt-1 p-3 rounded-xl bg-zinc-900 border border-surface-border text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="flex items-center gap-2 pt-1">
            <Button
              type="submit"
              disabled={loading || !fileName.trim() || !content.trim()}
              className="flex-1"
            >
              <Sparkles className="w-4 h-4" />
              {loading ? 'Processing & Generating Questions...' : 'Ingest File & Generate Questions'}
            </Button>
            {(fileName || content) && (
              <Button type="button" variant="secondary" onClick={handleClear} disabled={loading}>
                Clear
              </Button>
            )}
          </div>
        </form>
      </Card>

      {/* Extracted Result Preview */}
      {lastResult && (
        <Card className="space-y-4 border-emerald-500/40 bg-emerald-500/5 animate-fade-in">
          <div className="flex items-center gap-2 text-emerald-400 font-bold text-sm">
            <CheckCircle2 className="w-4 h-4" />
            Extraction Complete for {lastResult.fileName}
          </div>

          <div className="space-y-2 text-xs">
            <p className="font-semibold text-zinc-300">Quick Summary:</p>
            <p className="text-zinc-300 bg-zinc-900/60 p-3 rounded-xl border border-surface-border">
              {lastResult.summary}
            </p>

            <p className="font-semibold text-zinc-300 pt-2">Key Revision Takeaways:</p>
            <ul className="list-disc list-inside space-y-1 text-zinc-300 bg-zinc-900/60 p-3 rounded-xl border border-surface-border">
              {lastResult.revisionPoints.map((pt, idx) => (
                <li key={idx}>{pt}</li>
              ))}
            </ul>

            <div className="p-3 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-300 text-xs flex items-center justify-between">
              <span>{lastResult.generatedQuestions.length} Practice questions generated &amp; added to your question bank!</span>
              <BookOpen className="w-4 h-4 text-accent" />
            </div>
          </div>
        </Card>
      )}

      {/* Ingested Files List */}
      <div className="space-y-2.5">
        <h3 className="text-sm font-bold text-zinc-300">Previously Ingested Drive Documents</h3>
        {files.length === 0 ? (
          <p className="text-xs text-zinc-500 py-3 text-center">No files ingested yet.</p>
        ) : (
          files.map((file) => (
            <div
              key={file.id}
              className="p-3.5 rounded-xl border border-surface-border bg-surface-card/60 flex items-center justify-between gap-3 text-xs"
            >
              <div className="flex items-center gap-2.5 min-w-0 flex-1">
                <FileText className="w-4 h-4 text-accent shrink-0" />
                <div className="truncate">
                  <p className="font-semibold text-zinc-200 truncate">{file.fileName}</p>
                  <p className="text-[10px] text-zinc-500">{file.folderPath}</p>
                </div>
              </div>
              <Badge variant="purple">{file.generatedQuestionsCount} Qs Added</Badge>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
