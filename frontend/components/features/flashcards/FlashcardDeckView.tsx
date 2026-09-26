'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Layers,
  Sparkles,
  RotateCw,
  Plus,
  Trash2,
  CheckCircle,
  HelpCircle,
  ChevronRight,
  ChevronLeft,
  ArrowLeft,
  BookOpen,
  Calendar,
  Zap,
  Award,
  Filter,
  List,
  Flame,
  Clock,
  Check,
  RefreshCw,
  Eye,
} from 'lucide-react';
import confetti from 'canvas-confetti';
import {
  FlashcardDeck,
  FlashcardItem,
  CreateDeckRequest,
  CreateFlashcardRequest,
} from '@/types';
import { api } from '@/services/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface FlashcardDeckViewProps {
  onBack?: () => void;
}

export const FlashcardDeckView: React.FC<FlashcardDeckViewProps> = ({ onBack }) => {
  const [decks, setDecks] = useState<FlashcardDeck[]>([]);
  const [selectedDeckId, setSelectedDeckId] = useState<number | null>(null);
  const [cards, setCards] = useState<FlashcardItem[]>([]);
  const [loadingDecks, setLoadingDecks] = useState(true);
  const [loadingCards, setLoadingCards] = useState(false);

  // Filter: 'all', 'due', or box number 1-5
  const [filterMode, setFilterMode] = useState<'due' | 'all' | 1 | 2 | 3 | 4 | 5>('due');
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFlipped, setIsFlipped] = useState(false);
  const [reviewing, setReviewing] = useState(false);

  // Modals
  const [isAddCardOpen, setIsAddCardOpen] = useState(false);
  const [isAddDeckOpen, setIsAddDeckOpen] = useState(false);
  const [isManageCardsOpen, setIsManageCardsOpen] = useState(false);

  // Form states for adding card
  const [cardFront, setCardFront] = useState('');
  const [cardBack, setCardBack] = useState('');
  const [cardNotes, setCardNotes] = useState('');
  const [cardDeckId, setCardDeckId] = useState<number | null>(null);
  const [submittingCard, setSubmittingCard] = useState(false);

  // Form states for adding deck
  const [newDeckTitle, setNewDeckTitle] = useState('');
  const [newDeckSubject, setNewDeckSubject] = useState('');
  const [newDeckDesc, setNewDeckDesc] = useState('');
  const [newDeckColor, setNewDeckColor] = useState('#8b5cf6');
  const [submittingDeck, setSubmittingDeck] = useState(false);

  // Session summary
  const [sessionReviewedCount, setSessionReviewedCount] = useState(0);
  const [sessionCompleted, setSessionCompleted] = useState(false);

  // Load Decks
  const loadDecks = useCallback(async () => {
    try {
      const data = await api.getFlashcardDecks();
      setDecks(data);
      if (data.length > 0 && selectedDeckId === null) {
        setSelectedDeckId(data[0].id);
        setCardDeckId(data[0].id);
      }
    } catch (err) {
      console.error('Failed to load flashcard decks:', err);
    } finally {
      setLoadingDecks(false);
    }
  }, [selectedDeckId]);

  useEffect(() => {
    loadDecks();
  }, [loadDecks]);

  // Load Cards for Selected Deck
  const loadCards = useCallback(async (deckId: number) => {
    setLoadingCards(true);
    setIsFlipped(false);
    setCurrentIndex(0);
    setSessionCompleted(false);
    try {
      const data = await api.getFlashcards(deckId, false);
      setCards(data);
    } catch (err) {
      console.error('Failed to load flashcards:', err);
    } finally {
      setLoadingCards(false);
    }
  }, []);

  useEffect(() => {
    if (selectedDeckId !== null) {
      loadCards(selectedDeckId);
      setCardDeckId(selectedDeckId);
    }
  }, [selectedDeckId, loadCards]);

  const selectedDeck = decks.find((d) => d.id === selectedDeckId);

  // Filtered Cards Queue for Active Study
  const todayStr = new Date().toISOString().slice(0, 10);
  const filteredCards = cards.filter((card) => {
    if (filterMode === 'due') {
      return card.nextReviewDate <= todayStr;
    }
    if (filterMode === 'all') {
      return true;
    }
    return card.box === filterMode;
  });

  const currentCard: FlashcardItem | undefined = filteredCards[currentIndex];

  // Keyboard navigation & shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ignore if user is inside an input/textarea
      if (['INPUT', 'TEXTAREA', 'SELECT'].includes((e.target as HTMLElement).tagName)) {
        return;
      }

      if (e.code === 'Space') {
        e.preventDefault();
        setIsFlipped((prev) => !prev);
      } else if (isFlipped && !reviewing && currentCard) {
        if (e.key === '1') {
          handleReview('again');
        } else if (e.key === '2') {
          handleReview('hard');
        } else if (e.key === '3') {
          handleReview('good');
        } else if (e.key === '4') {
          handleReview('easy');
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isFlipped, reviewing, currentCard]);

  // Handle Review
  const handleReview = async (rating: 'again' | 'hard' | 'good' | 'easy') => {
    if (!currentCard || reviewing) return;

    setReviewing(true);
    try {
      const updatedCard = await api.reviewFlashcard(currentCard.id, rating);

      // Trigger micro celebration if advanced to Box 5
      if (updatedCard.box === 5 && currentCard.box < 5) {
        confetti({
          particleCount: 40,
          spread: 50,
          origin: { y: 0.7 },
          colors: ['#a855f7', '#38bdf8', '#4ade80'],
        });
      }

      // Update card in local list
      setCards((prev) => prev.map((c) => (c.id === updatedCard.id ? updatedCard : c)));
      setSessionReviewedCount((prev) => prev + 1);

      // Advance queue or finish
      if (currentIndex + 1 < filteredCards.length) {
        setIsFlipped(false);
        setCurrentIndex((prev) => prev + 1);
      } else {
        setSessionCompleted(true);
        confetti({
          particleCount: 80,
          spread: 70,
          origin: { y: 0.6 },
        });
      }

      // Refresh deck stats in background
      if (selectedDeckId) {
        const refreshedDecks = await api.getFlashcardDecks();
        setDecks(refreshedDecks);
      }
    } catch (err: any) {
      alert('Review failed: ' + err.message);
    } finally {
      setReviewing(false);
    }
  };

  // Add Card
  const handleCreateCard = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!cardFront.trim() || !cardBack.trim() || !cardDeckId) return;

    setSubmittingCard(true);
    try {
      const created = await api.createFlashcard({
        deckId: cardDeckId,
        front: cardFront.trim(),
        back: cardBack.trim(),
        notes: cardNotes.trim() || undefined,
      });

      if (cardDeckId === selectedDeckId) {
        setCards((prev) => [...prev, created]);
      }

      setCardFront('');
      setCardBack('');
      setCardNotes('');
      setIsAddCardOpen(false);

      const refreshedDecks = await api.getFlashcardDecks();
      setDecks(refreshedDecks);
    } catch (err: any) {
      alert('Failed to add flashcard: ' + err.message);
    } finally {
      setSubmittingCard(false);
    }
  };

  // Add Deck
  const handleCreateDeck = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newDeckTitle.trim()) return;

    setSubmittingDeck(true);
    try {
      const created = await api.createFlashcardDeck({
        title: newDeckTitle.trim(),
        subject: newDeckSubject.trim() || undefined,
        description: newDeckDesc.trim() || undefined,
        color: newDeckColor,
        icon: 'Layers',
      });

      setDecks((prev) => [...prev, created]);
      setSelectedDeckId(created.id);
      setNewDeckTitle('');
      setNewDeckSubject('');
      setNewDeckDesc('');
      setIsAddDeckOpen(false);
    } catch (err: any) {
      alert('Failed to create deck: ' + err.message);
    } finally {
      setSubmittingDeck(false);
    }
  };

  // Delete Card
  const handleDeleteCard = async (id: number) => {
    if (!confirm('Are you sure you want to delete this flashcard?')) return;
    try {
      await api.deleteFlashcard(id);
      setCards((prev) => prev.filter((c) => c.id !== id));
      if (currentIndex >= filteredCards.length - 1 && currentIndex > 0) {
        setCurrentIndex((prev) => prev - 1);
      }
      const refreshedDecks = await api.getFlashcardDecks();
      setDecks(refreshedDecks);
    } catch (err: any) {
      alert('Failed to delete card: ' + err.message);
    }
  };

  // Delete Deck
  const handleDeleteDeck = async (deckId: number) => {
    if (!confirm('Are you sure you want to delete this deck and all its flashcards?')) return;
    try {
      await api.deleteFlashcardDeck(deckId);
      const remaining = decks.filter((d) => d.id !== deckId);
      setDecks(remaining);
      if (remaining.length > 0) {
        setSelectedDeckId(remaining[0].id);
      } else {
        setSelectedDeckId(null);
        setCards([]);
      }
    } catch (err: any) {
      alert('Failed to delete deck: ' + err.message);
    }
  };

  // Box calculations for visualization
  const box1Count = selectedDeck?.boxCounts?.[1] || 0;
  const box2Count = selectedDeck?.boxCounts?.[2] || 0;
  const box3Count = selectedDeck?.boxCounts?.[3] || 0;
  const box4Count = selectedDeck?.boxCounts?.[4] || 0;
  const box5Count = selectedDeck?.boxCounts?.[5] || 0;
  const totalCardsCount = cards.length;
  const masteredCount = box4Count + box5Count;
  const masteryPercentage =
    totalCardsCount > 0 ? Math.round((masteredCount / totalCardsCount) * 100) : 0;

  const boxConfigs = [
    { box: 1, name: 'Box 1', interval: '1 Day', color: 'from-rose-500 to-red-600', badge: 'red' as const, count: box1Count },
    { box: 2, name: 'Box 2', interval: '3 Days', color: 'from-orange-500 to-amber-600', badge: 'yellow' as const, count: box2Count },
    { box: 3, name: 'Box 3', interval: '1 Week', color: 'from-amber-400 to-yellow-500', badge: 'yellow' as const, count: box3Count },
    { box: 4, name: 'Box 4', interval: '2 Weeks', color: 'from-emerald-400 to-green-600', badge: 'green' as const, count: box4Count },
    { box: 5, name: 'Box 5', interval: '30 Days (Mastered)', color: 'from-indigo-400 to-purple-600', badge: 'purple' as const, count: box5Count },
  ];

  return (
    <div className="space-y-6 pb-20">
      {/* Module Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          {onBack && (
            <button
              onClick={onBack}
              className="p-1.5 rounded-xl border border-surface-border bg-surface-card hover:bg-zinc-800 text-zinc-400 hover:text-white transition"
              title="Back"
            >
              <ArrowLeft className="w-4 h-4" />
            </button>
          )}
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl sm:text-2xl font-extrabold text-white tracking-tight">
                Flashcards & Leitner Box
              </h2>
              <Badge variant="purple">Active Recall</Badge>
            </div>
            <p className="text-xs text-zinc-400">
              5-box spaced repetition memory engine for GATE CS mastery
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button
            size="sm"
            variant="secondary"
            onClick={() => setIsManageCardsOpen(true)}
            disabled={!selectedDeck || cards.length === 0}
          >
            <List className="w-3.5 h-3.5" />
            <span>Cards</span>
          </Button>
          <Button size="sm" variant="secondary" onClick={() => setIsAddDeckOpen(true)}>
            <Plus className="w-3.5 h-3.5" />
            <span>Deck</span>
          </Button>
          <Button size="sm" onClick={() => setIsAddCardOpen(true)} disabled={!selectedDeck}>
            <Plus className="w-3.5 h-3.5" />
            <span>Add Card</span>
          </Button>
        </div>
      </div>

      {/* Deck Selector Row */}
      {loadingDecks ? (
        <div className="h-16 rounded-2xl bg-surface-card animate-pulse border border-surface-border" />
      ) : decks.length === 0 ? (
        <Card className="text-center py-8 space-y-3">
          <Layers className="w-10 h-10 text-zinc-500 mx-auto" />
          <p className="text-sm text-zinc-300 font-medium">No flashcard decks found.</p>
          <Button size="sm" onClick={() => setIsAddDeckOpen(true)}>
            Create Your First Deck
          </Button>
        </Card>
      ) : (
        <div className="flex gap-2.5 overflow-x-auto pb-1 scrollbar-thin">
          {decks.map((deck) => {
            const isSelected = deck.id === selectedDeckId;
            return (
              <button
                key={deck.id}
                onClick={() => {
                  setSelectedDeckId(deck.id);
                  setFilterMode('due');
                }}
                className={`flex-shrink-0 px-3.5 py-2.5 rounded-2xl border transition-all text-left flex items-center gap-3 select-none ${
                  isSelected
                    ? 'bg-accent/20 border-accent text-white shadow-lg shadow-accent/10'
                    : 'bg-surface-card/70 border-surface-border text-zinc-400 hover:text-zinc-200 hover:border-zinc-700'
                }`}
              >
                <div
                  className="w-8 h-8 rounded-xl flex items-center justify-center text-white shrink-0 shadow-inner"
                  style={{ backgroundColor: `${deck.color}40`, border: `1px solid ${deck.color}80` }}
                >
                  <BookOpen className="w-4 h-4" style={{ color: deck.color }} />
                </div>
                <div className="min-w-0 pr-1">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-bold text-white truncate max-w-[130px] sm:max-w-[180px]">
                      {deck.title}
                    </span>
                    {deck.dueCount > 0 && (
                      <span className="px-1.5 py-0.2 rounded-full bg-rose-500/20 text-rose-300 border border-rose-500/30 text-[9px] font-bold">
                        {deck.dueCount} due
                      </span>
                    )}
                  </div>
                  <span className="text-[10px] text-zinc-500 font-medium block truncate max-w-[160px]">
                    {deck.cardCount} cards • {deck.subject || 'CS Core'}
                  </span>
                </div>
              </button>
            );
          })}
        </div>
      )}

      {/* Selected Deck Overview & Leitner Box Distribution */}
      {selectedDeck && (
        <Card className="space-y-4 bg-gradient-to-b from-surface-card/90 to-surface-card/40 border-surface-border">
          {/* Deck Title & Mastery Bar */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-base font-bold text-white">{selectedDeck.title}</h3>
                <span className="text-[10px] text-zinc-400 font-mono">
                  ({cards.length} cards total)
                </span>
              </div>
              {selectedDeck.description && (
                <p className="text-xs text-zinc-400 mt-0.5">{selectedDeck.description}</p>
              )}
            </div>

            {/* Mastery Score Gauge */}
            <div className="flex items-center gap-3 bg-zinc-900/60 px-3 py-1.5 rounded-xl border border-surface-border">
              <Award className="w-4 h-4 text-accent" />
              <div>
                <span className="text-[10px] text-zinc-400 uppercase tracking-wider block font-semibold">
                  Retention / Mastery
                </span>
                <span className="text-xs font-bold text-white">
                  {masteryPercentage}% (Boxes 4-5)
                </span>
              </div>
            </div>
          </div>

          {/* Leitner 5-Box Distribution Visualization */}
          <div className="space-y-2">
            <div className="flex items-center justify-between text-xs text-zinc-400">
              <span className="font-semibold flex items-center gap-1.5">
                <Layers className="w-3.5 h-3.5 text-accent" />
                Leitner Spaced Repetition Progression (Boxes 1 → 5)
              </span>
              <span className="text-[11px] text-zinc-500">Click any box to filter cards</span>
            </div>

            <div className="grid grid-cols-5 gap-1.5 sm:gap-2">
              {boxConfigs.map((b) => {
                const isActive = filterMode === b.box;
                return (
                  <button
                    key={b.box}
                    onClick={() => {
                      setFilterMode(isActive ? 'all' : (b.box as any));
                      setCurrentIndex(0);
                      setIsFlipped(false);
                      setSessionCompleted(false);
                    }}
                    className={`p-2 sm:p-2.5 rounded-xl border transition-all text-center group ${
                      isActive
                        ? 'bg-accent/25 border-accent text-white shadow-md'
                        : 'bg-zinc-900/80 border-surface-border text-zinc-400 hover:border-zinc-700 hover:text-zinc-200'
                    }`}
                  >
                    <div className="text-[10px] sm:text-xs font-bold text-zinc-300">
                      {b.name}
                    </div>
                    <div className="text-sm sm:text-base font-extrabold text-white my-0.5">
                      {b.count}
                    </div>
                    <div className="text-[9px] text-zinc-500 font-medium truncate">
                      {b.interval}
                    </div>
                  </button>
                );
              })}
            </div>
          </div>

          {/* Filter Bar */}
          <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-surface-border/60">
            <div className="flex items-center gap-1.5">
              <span className="text-[11px] font-semibold text-zinc-500 flex items-center gap-1">
                <Filter className="w-3 h-3" /> Queue:
              </span>
              <button
                onClick={() => {
                  setFilterMode('due');
                  setCurrentIndex(0);
                  setIsFlipped(false);
                  setSessionCompleted(false);
                }}
                className={`px-2.5 py-1 rounded-lg text-xs font-semibold transition ${
                  filterMode === 'due'
                    ? 'bg-rose-500/20 text-rose-300 border border-rose-500/40'
                    : 'text-zinc-400 hover:text-white bg-zinc-900 border border-surface-border'
                }`}
              >
                Due Today ({selectedDeck.dueCount})
              </button>
              <button
                onClick={() => {
                  setFilterMode('all');
                  setCurrentIndex(0);
                  setIsFlipped(false);
                  setSessionCompleted(false);
                }}
                className={`px-2.5 py-1 rounded-lg text-xs font-semibold transition ${
                  filterMode === 'all'
                    ? 'bg-accent/20 text-accent border border-accent/40'
                    : 'text-zinc-400 hover:text-white bg-zinc-900 border border-surface-border'
                }`}
              >
                All Cards ({cards.length})
              </button>
            </div>

            <div className="text-[11px] text-zinc-500 flex items-center gap-2">
              <span className="hidden sm:inline">Space: flip card • 1-4: review</span>
              <button
                onClick={() => loadCards(selectedDeck.id)}
                className="hover:text-zinc-300 p-1 rounded-lg hover:bg-zinc-800 transition"
                title="Reload cards"
              >
                <RefreshCw className="w-3 h-3" />
              </button>
            </div>
          </div>
        </Card>
      )}

      {/* Main Flashcard Study Workspace */}
      {loadingCards ? (
        <Card className="h-64 flex items-center justify-center">
          <div className="flex items-center gap-2 text-zinc-500 text-xs">
            <RefreshCw className="w-4 h-4 animate-spin text-accent" />
            <span>Loading Leitner review queue...</span>
          </div>
        </Card>
      ) : filteredCards.length === 0 ? (
        <Card className="text-center py-12 space-y-4">
          <div className="w-12 h-12 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 flex items-center justify-center mx-auto">
            <CheckCircle className="w-6 h-6" />
          </div>
          <div className="space-y-1">
            <h4 className="text-base font-bold text-white">
              {filterMode === 'due' ? 'All Due Flashcards Cleared!' : 'No Cards Found in This Filter'}
            </h4>
            <p className="text-xs text-zinc-400 max-w-sm mx-auto">
              {filterMode === 'due'
                ? 'Great work! No cards due for review today in this deck. Come back tomorrow or review all cards to reinforce memory.'
                : 'There are currently no cards matching this filter mode. Switch to All Cards or add new flashcards.'}
            </p>
          </div>
          <div className="flex items-center justify-center gap-2 pt-2">
            {filterMode === 'due' && cards.length > 0 && (
              <Button size="sm" variant="secondary" onClick={() => setFilterMode('all')}>
                Study All {cards.length} Cards
              </Button>
            )}
            <Button size="sm" onClick={() => setIsAddCardOpen(true)}>
              <Plus className="w-4 h-4" /> Add Flashcard
            </Button>
          </div>
        </Card>
      ) : sessionCompleted ? (
        /* Session Completed Celebration Screen */
        <Card className="text-center py-12 space-y-5 bg-gradient-to-b from-purple-900/20 to-surface-card border-accent/40">
          <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-accent to-purple-400 flex items-center justify-center mx-auto shadow-lg shadow-accent/20">
            <Award className="w-8 h-8 text-white" />
          </div>
          <div className="space-y-1.5">
            <h3 className="text-xl font-black text-white">Review Session Completed!</h3>
            <p className="text-xs text-zinc-300 max-w-md mx-auto">
              You reviewed {filteredCards.length} cards in this queue. Your spaced repetition intervals
              have been recalculated based on your Leitner box ratings.
            </p>
          </div>

          <div className="inline-flex items-center gap-4 px-4 py-2 rounded-xl bg-zinc-900/80 border border-surface-border text-xs text-zinc-300">
            <span className="flex items-center gap-1.5">
              <Flame className="w-4 h-4 text-amber-400" />
              Reviewed: <strong>{sessionReviewedCount}</strong>
            </span>
            <span className="text-zinc-600">|</span>
            <span className="flex items-center gap-1.5">
              <Layers className="w-4 h-4 text-accent" />
              Deck: <strong>{selectedDeck?.title}</strong>
            </span>
          </div>

          <div className="flex items-center justify-center gap-3 pt-2">
            <Button
              size="sm"
              variant="secondary"
              onClick={() => {
                setSessionCompleted(false);
                setCurrentIndex(0);
                setIsFlipped(false);
              }}
            >
              <RotateCw className="w-3.5 h-3.5" />
              Restart Queue
            </Button>
            <Button
              size="sm"
              onClick={() => {
                setFilterMode('all');
                setSessionCompleted(false);
                setCurrentIndex(0);
                setIsFlipped(false);
              }}
            >
              Study All Cards
            </Button>
          </div>
        </Card>
      ) : currentCard ? (
        /* Active Interactive Flip Card */
        <div className="space-y-4">
          {/* Queue Progress Bar & Counter */}
          <div className="flex items-center justify-between text-xs text-zinc-400">
            <div className="flex items-center gap-2 font-medium">
              <span className="text-white font-bold">
                Card {currentIndex + 1} of {filteredCards.length}
              </span>
              <span className="text-zinc-600">•</span>
              <Badge variant={currentCard.box >= 4 ? 'green' : currentCard.box >= 2 ? 'yellow' : 'red'}>
                Box {currentCard.box} ({currentCard.intervalDays}d interval)
              </Badge>
              {currentCard.reps > 0 && (
                <span className="text-[11px] text-zinc-500 font-mono">
                  {currentCard.reps} reps
                </span>
              )}
            </div>

            <div className="flex items-center gap-1">
              <button
                disabled={currentIndex === 0}
                onClick={() => {
                  if (currentIndex > 0) {
                    setCurrentIndex((prev) => prev - 1);
                    setIsFlipped(false);
                  }
                }}
                className="p-1 rounded-lg hover:bg-zinc-800 disabled:opacity-30 disabled:hover:bg-transparent text-zinc-400 hover:text-white transition"
                title="Previous card"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                disabled={currentIndex >= filteredCards.length - 1}
                onClick={() => {
                  if (currentIndex < filteredCards.length - 1) {
                    setCurrentIndex((prev) => prev + 1);
                    setIsFlipped(false);
                  }
                }}
                className="p-1 rounded-lg hover:bg-zinc-800 disabled:opacity-30 disabled:hover:bg-transparent text-zinc-400 hover:text-white transition"
                title="Next card"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Linear Progress Indicator */}
          <div className="w-full h-1.5 rounded-full bg-zinc-800 overflow-hidden">
            <div
              className="h-full bg-gradient-to-r from-accent to-purple-400 transition-all duration-300"
              style={{
                width: `${((currentIndex + 1) / filteredCards.length) * 100}%`,
              }}
            />
          </div>

          {/* Interactive Flip Card Container */}
          <div
            onClick={() => setIsFlipped((prev) => !prev)}
            className="cursor-pointer select-none min-h-[280px] sm:min-h-[320px] rounded-3xl p-6 sm:p-8 border border-surface-border bg-gradient-to-b from-[#121422] to-[#0c0e18] shadow-2xl relative flex flex-col justify-between transition-all duration-300 hover:border-accent/40 group"
          >
            {/* Card Header Info */}
            <div className="flex items-center justify-between text-xs pb-3 border-b border-surface-border/50">
              <div className="flex items-center gap-2">
                <span className="text-[10px] tracking-wider uppercase font-bold text-accent px-2 py-0.5 rounded-md bg-accent/15 border border-accent/30">
                  {isFlipped ? 'Answer & Derivation' : 'Concept / Question'}
                </span>
                {currentCard.notes && (
                  <span className="text-[11px] text-zinc-400 font-medium truncate max-w-[200px]">
                    {currentCard.notes}
                  </span>
                )}
              </div>

              <div className="flex items-center gap-1.5 text-zinc-500 group-hover:text-zinc-300 transition text-[11px]">
                <RotateCw className="w-3 h-3 animate-spin-slow" />
                <span>{isFlipped ? 'Flip to front' : 'Click to flip'}</span>
              </div>
            </div>

            {/* Card Content Area */}
            <div className="py-6 my-auto">
              {!isFlipped ? (
                /* FRONT FACE */
                <div className="space-y-3 animate-fade-in">
                  <h3 className="text-base sm:text-xl font-bold text-white leading-relaxed tracking-wide">
                    {currentCard.front}
                  </h3>
                  <p className="text-xs text-zinc-500 pt-2 flex items-center gap-1">
                    <HelpCircle className="w-3.5 h-3.5" />
                    Recall the definition, formula, or complexity before flipping.
                  </p>
                </div>
              ) : (
                /* BACK FACE */
                <div className="space-y-4 animate-fade-in">
                  <div className="text-sm sm:text-base text-zinc-100 leading-relaxed whitespace-pre-line font-normal">
                    {currentCard.back}
                  </div>
                  {currentCard.notes && (
                    <div className="p-2.5 rounded-xl bg-accent/10 border border-accent/20 text-xs text-accent">
                      <strong>GATE Context:</strong> {currentCard.notes}
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Card Footer / Flip CTA */}
            <div className="pt-3 border-t border-surface-border/50 flex items-center justify-between text-[11px] text-zinc-500">
              <div className="flex items-center gap-2">
                <Calendar className="w-3.5 h-3.5" />
                <span>Next review: {currentCard.nextReviewDate}</span>
              </div>
              <span className="text-zinc-600 font-mono">Ease: {currentCard.ease.toFixed(2)}</span>
            </div>
          </div>

          {/* Leitner Box Rating Buttons (Appear when card is flipped) */}
          {isFlipped ? (
            <div className="space-y-2 animate-fade-in">
              <div className="text-center text-[11px] text-zinc-400 font-medium">
                Rate your recall to update Leitner Box progression:
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                {/* 1. AGAIN */}
                <button
                  disabled={reviewing}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleReview('again');
                  }}
                  className="p-3 rounded-2xl border border-rose-500/40 bg-rose-500/10 hover:bg-rose-500/20 text-rose-300 transition-all flex flex-col items-center gap-1 active:scale-95"
                >
                  <span className="text-xs font-black tracking-wide">AGAIN [1]</span>
                  <span className="text-[10px] text-rose-400/80 font-medium">Reset to Box 1 (1d)</span>
                </button>

                {/* 2. HARD */}
                <button
                  disabled={reviewing}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleReview('hard');
                  }}
                  className="p-3 rounded-2xl border border-amber-500/40 bg-amber-500/10 hover:bg-amber-500/20 text-amber-300 transition-all flex flex-col items-center gap-1 active:scale-95"
                >
                  <span className="text-xs font-black tracking-wide">HARD [2]</span>
                  <span className="text-[10px] text-amber-400/80 font-medium">
                    Same Box ({Math.max(1, Math.floor(currentCard.intervalDays * 0.7))}d)
                  </span>
                </button>

                {/* 3. GOOD */}
                <button
                  disabled={reviewing}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleReview('good');
                  }}
                  className="p-3 rounded-2xl border border-emerald-500/40 bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-300 transition-all flex flex-col items-center gap-1 active:scale-95 shadow-md shadow-emerald-500/5"
                >
                  <span className="text-xs font-black tracking-wide">GOOD [3]</span>
                  <span className="text-[10px] text-emerald-400/80 font-medium">
                    Box {Math.min(5, currentCard.box + 1)} (+1 Box)
                  </span>
                </button>

                {/* 4. EASY */}
                <button
                  disabled={reviewing}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleReview('easy');
                  }}
                  className="p-3 rounded-2xl border border-cyan-500/40 bg-cyan-500/10 hover:bg-cyan-500/20 text-cyan-300 transition-all flex flex-col items-center gap-1 active:scale-95"
                >
                  <span className="text-xs font-black tracking-wide">EASY [4]</span>
                  <span className="text-[10px] text-cyan-400/80 font-medium">
                    Box {Math.min(5, currentCard.box + 1)} + Bonus
                  </span>
                </button>
              </div>
            </div>
          ) : (
            <div className="flex justify-center">
              <Button
                variant="secondary"
                size="md"
                onClick={() => setIsFlipped(true)}
                className="w-full sm:w-auto px-8"
              >
                <Eye className="w-4 h-4" />
                <span>Reveal Answer (or press Space)</span>
              </Button>
            </div>
          )}
        </div>
      ) : null}

      {/* Add Flashcard Modal */}
      <Modal isOpen={isAddCardOpen} onClose={() => setIsAddCardOpen(false)} title="Add New Flashcard">
        <form onSubmit={handleCreateCard} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Target Deck</label>
            <select
              value={cardDeckId || ''}
              onChange={(e) => setCardDeckId(Number(e.target.value))}
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs focus:outline-none focus:border-accent"
            >
              {decks.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.title} ({d.subject || 'CS'})
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">
              Front Side: Concept / Question
            </label>
            <textarea
              value={cardFront}
              onChange={(e) => setCardFront(e.target.value)}
              placeholder="e.g. State the master theorem recurrence condition for T(n) = a T(n/b) + f(n)"
              rows={3}
              required
              autoFocus
              className="w-full p-3 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">
              Back Side: Answer / Derivation / Key Takeaways
            </label>
            <textarea
              value={cardBack}
              onChange={(e) => setCardBack(e.target.value)}
              placeholder="e.g. Compare a with b^k: 1) a > b^k => O(n^(log_b a)), 2) a = b^k => O(n^k log^(p+1) n)..."
              rows={4}
              required
              className="w-full p-3 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">
              GATE Context / Topic Notes (Optional)
            </label>
            <input
              type="text"
              value={cardNotes}
              onChange={(e) => setCardNotes(e.target.value)}
              placeholder="e.g. Algorithms / Divide and Conquer"
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setIsAddCardOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={submittingCard || !cardFront.trim() || !cardBack.trim()}
            >
              {submittingCard ? 'Adding Card...' : 'Add Flashcard'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Add Deck Modal */}
      <Modal isOpen={isAddDeckOpen} onClose={() => setIsAddDeckOpen(false)} title="Create New Deck">
        <form onSubmit={handleCreateDeck} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Deck Title</label>
            <input
              type="text"
              value={newDeckTitle}
              onChange={(e) => setNewDeckTitle(e.target.value)}
              placeholder="e.g. Compiler Design Syntax Trees"
              required
              autoFocus
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">GATE CS Subject</label>
            <input
              type="text"
              value={newDeckSubject}
              onChange={(e) => setNewDeckSubject(e.target.value)}
              placeholder="e.g. Compiler Design"
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Description (Optional)</label>
            <textarea
              value={newDeckDesc}
              onChange={(e) => setNewDeckDesc(e.target.value)}
              placeholder="Brief overview of cards in this deck"
              rows={2}
              className="w-full p-2.5 rounded-xl bg-zinc-900 border border-zinc-800 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Color Theme</label>
            <div className="flex gap-2 pt-1">
              {['#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ec4899'].map((c) => (
                <button
                  key={c}
                  type="button"
                  onClick={() => setNewDeckColor(c)}
                  className={`w-7 h-7 rounded-full border-2 transition ${
                    newDeckColor === c ? 'border-white scale-110' : 'border-transparent'
                  }`}
                  style={{ backgroundColor: c }}
                />
              ))}
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setIsAddDeckOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={submittingDeck || !newDeckTitle.trim()}>
              {submittingDeck ? 'Creating...' : 'Create Deck'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Manage Cards List Modal */}
      <Modal
        isOpen={isManageCardsOpen}
        onClose={() => setIsManageCardsOpen(false)}
        title={`Cards in ${selectedDeck?.title || 'Deck'} (${cards.length})`}
      >
        <div className="space-y-3 max-h-[60vh] overflow-y-auto pr-1">
          {cards.map((c, i) => (
            <div
              key={c.id}
              className="p-3 rounded-xl bg-zinc-900 border border-surface-border flex items-start justify-between gap-3 text-xs"
            >
              <div className="min-w-0 flex-1 space-y-1">
                <div className="flex items-center gap-2">
                  <span className="font-bold text-white">#{i + 1}</span>
                  <Badge variant={c.box >= 4 ? 'green' : c.box >= 2 ? 'yellow' : 'red'}>
                    Box {c.box}
                  </Badge>
                  <span className="text-[10px] text-zinc-500 font-mono">
                    Due: {c.nextReviewDate}
                  </span>
                </div>
                <p className="text-zinc-200 font-medium truncate">{c.front}</p>
                <p className="text-zinc-400 text-[11px] truncate">{c.back}</p>
              </div>

              <button
                onClick={() => handleDeleteCard(c.id)}
                className="p-1.5 rounded-lg text-zinc-500 hover:text-rose-400 hover:bg-rose-500/10 transition shrink-0"
                title="Delete flashcard"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))}

          {selectedDeck && (
            <div className="pt-4 border-t border-surface-border flex justify-between items-center">
              <button
                onClick={() => {
                  setIsManageCardsOpen(false);
                  handleDeleteDeck(selectedDeck.id);
                }}
                className="text-xs text-rose-400 hover:text-rose-300 font-medium flex items-center gap-1.5"
              >
                <Trash2 className="w-3.5 h-3.5" />
                <span>Delete entire deck</span>
              </button>
              <Button size="sm" variant="secondary" onClick={() => setIsManageCardsOpen(false)}>
                Done
              </Button>
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
};
