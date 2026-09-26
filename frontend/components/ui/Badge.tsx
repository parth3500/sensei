import React from 'react';

interface BadgeProps {
  children: React.ReactNode;
  variant?: 'purple' | 'green' | 'yellow' | 'red' | 'zinc' | 'blue';
  size?: 'sm' | 'md';
}

export const Badge: React.FC<BadgeProps> = ({ children, variant = 'purple', size = 'sm' }) => {
  const variantStyles = {
    purple: 'bg-purple-500/10 text-purple-300 border-purple-500/30',
    green: 'bg-emerald-500/10 text-emerald-300 border-emerald-500/30',
    yellow: 'bg-amber-500/10 text-amber-300 border-amber-500/30',
    red: 'bg-rose-500/10 text-rose-300 border-rose-500/30',
    zinc: 'bg-zinc-800 text-zinc-400 border-zinc-700',
    blue: 'bg-blue-500/10 text-blue-300 border-blue-500/30',
  }[variant];

  const sizeStyles = {
    sm: 'text-[10px] px-2 py-0.5 tracking-wider uppercase font-semibold',
    md: 'text-xs px-2.5 py-1 font-medium',
  }[size];

  return (
    <span className={`inline-flex items-center rounded-lg border ${variantStyles} ${sizeStyles}`}>
      {children}
    </span>
  );
};
