import React, { useState, useRef, useEffect } from 'react';
import { Bot, Send, Sparkles, User } from 'lucide-react';
import { ChatMessage } from '@/types';
import { Button } from '@/components/ui/Button';

interface AiTutorViewProps {
  messages: ChatMessage[];
  onSendMessage: (content: string) => Promise<void>;
  loading: boolean;
}

export const AiTutorView: React.FC<AiTutorViewProps> = ({
  messages,
  onSendMessage,
  loading,
}) => {
  const [input, setInput] = useState('');
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, loading]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || loading) return;

    const text = input;
    setInput('');
    await onSendMessage(text);
  };

  const quickPrompts = [
    'What are high-yield OS topics?',
    'Explain Banker\'s algorithm simply',
    'How should I structure my daily schedule?',
    'Give me a 30-day GATE plan',
  ];

  return (
    <div className="flex flex-col h-[calc(100vh-140px)] space-y-4 pb-16">
      {/* Header */}
      <div>
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 rounded-xl bg-accent/20 text-accent flex items-center justify-center">
            <Bot className="w-4 h-4" />
          </div>
          <h2 className="text-xl font-bold text-white tracking-tight">Sensei AI Tutor</h2>
        </div>
        <p className="text-xs text-zinc-400 mt-0.5">
          Context-aware study coach for GATE CS &bull; Hinglish-friendly
        </p>
      </div>

      {/* Messages Scroll Area */}
      <div className="flex-1 overflow-y-auto space-y-3 p-4 rounded-2xl border border-surface-border bg-surface-card/40 backdrop-blur-md">
        {messages.map((msg, idx) => {
          const isUser = msg.role === 'user';
          return (
            <div
              key={idx}
              className={`flex items-start gap-2.5 ${isUser ? 'flex-row-reverse' : 'flex-row'}`}
            >
              <div
                className={`w-7 h-7 rounded-xl flex items-center justify-center shrink-0 text-xs ${
                  isUser
                    ? 'bg-purple-600 text-white'
                    : 'bg-zinc-800 text-accent border border-zinc-700'
                }`}
              >
                {isUser ? <User className="w-3.5 h-3.5" /> : <Bot className="w-3.5 h-3.5" />}
              </div>

              <div
                className={`max-w-[82%] px-4 py-2.5 rounded-2xl text-xs sm:text-sm leading-relaxed ${
                  isUser
                    ? 'bg-accent text-white rounded-tr-sm shadow-md'
                    : 'bg-zinc-900 border border-surface-border text-zinc-200 rounded-tl-sm'
                }`}
              >
                {msg.content}
              </div>
            </div>
          );
        })}

        {loading && (
          <div className="flex items-center gap-2.5">
            <div className="w-7 h-7 rounded-xl bg-zinc-800 text-accent border border-zinc-700 flex items-center justify-center shrink-0">
              <Bot className="w-3.5 h-3.5" />
            </div>
            <div className="px-4 py-3 rounded-2xl bg-zinc-900 border border-surface-border flex items-center gap-1.5">
              <div className="w-2 h-2 rounded-full bg-accent animate-bounce" style={{ animationDelay: '0ms' }} />
              <div className="w-2 h-2 rounded-full bg-accent animate-bounce" style={{ animationDelay: '150ms' }} />
              <div className="w-2 h-2 rounded-full bg-accent animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          </div>
        )}
        <div ref={scrollRef} />
      </div>

      {/* Quick Prompts */}
      <div className="flex gap-2 overflow-x-auto pb-1 scrollbar-none">
        {quickPrompts.map((prompt, idx) => (
          <button
            key={idx}
            disabled={loading}
            onClick={() => onSendMessage(prompt)}
            className="shrink-0 px-3 py-1.5 rounded-xl border border-surface-border bg-zinc-900/60 hover:border-accent/40 text-xs text-zinc-400 hover:text-zinc-200 transition"
          >
            {prompt}
          </button>
        ))}
      </div>

      {/* Input Field */}
      <form onSubmit={handleSubmit} className="flex gap-2">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Ask Sensei about topics, concepts, or strategy..."
          className="flex-1 px-4 py-2.5 rounded-xl bg-zinc-900 border border-surface-border text-white placeholder-zinc-500 focus:outline-none focus:border-accent text-sm"
        />
        <Button type="submit" disabled={!input.trim() || loading} size="md">
          <Send className="w-4 h-4" />
        </Button>
      </form>
    </div>
  );
};
