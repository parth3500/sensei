import React from 'react';

interface CardProps {
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
}

export const Card: React.FC<CardProps> = ({ children, className = '', onClick }) => {
  return (
    <div
      onClick={onClick}
      className={`glass-card p-5 rounded-2xl border border-surface-border bg-surface-card/60 backdrop-blur-md shadow-xl transition-all duration-200 ${className}`}
    >
      {children}
    </div>
  );
};
