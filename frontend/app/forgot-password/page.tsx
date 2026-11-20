'use client';

import Link from 'next/link';
import React, { useState } from 'react';
import authService from '../../Services/AuthenticationService';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (isLoading) return;

    const trimmed = email.trim();
    if (!trimmed || !trimmed.includes('@')) {
      setError('Please enter a valid email address.');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await authService.forgotPassword(trimmed);
      setSent(true);
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Unable to process password reset request. Please try again.';
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#070b14] text-slate-100 flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md rounded-2xl border border-emerald-500/20 bg-slate-900/80 p-8 shadow-2xl backdrop-blur">
        <div className="mb-6 text-center">
          <Link
            href="/"
            className="text-xs font-semibold uppercase tracking-[0.25em] text-emerald-400"
          >
            PIXELPITCHAI
          </Link>
          <h1 className="mt-2 text-2xl font-bold text-white">
            Forgot Password
          </h1>
          <p className="mt-2 text-sm text-slate-400">
            Enter your account email and we will send you a password reset link.
          </p>
        </div>

        {sent ? (
          <div className="space-y-4">
            <div
              role="status"
              className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-4 text-sm text-emerald-200"
            >
              If an account with a confirmed email exists for{' '}
              <span className="font-semibold text-white">{email}</span>, a
              password reset link has been sent.
            </div>
            <div className="flex flex-col gap-2 text-center text-sm">
              <Link
                href="/reset-password"
                className="rounded-lg bg-emerald-500 px-4 py-2.5 font-semibold text-slate-950 transition hover:bg-emerald-400"
              >
                Have a Reset Token? Reset Password
              </Link>
              <Link
                href="/login"
                className="py-2 text-slate-400 transition hover:text-emerald-400"
              >
                Back to Sign In
              </Link>
            </div>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label
                htmlFor="forgot-email"
                className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-slate-300"
              >
                Email Address
              </label>
              <input
                id="forgot-email"
                type="email"
                required
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="coach@club.com"
                className="w-full rounded-lg border border-slate-700 bg-slate-950/70 px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:border-emerald-400 focus:outline-none"
              />
            </div>

            {error && (
              <div
                role="alert"
                className="rounded-lg border border-red-500/30 bg-red-500/10 px-3.5 py-2.5 text-xs text-red-300"
              >
                {error}
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading}
              className="w-full rounded-lg bg-emerald-500 px-4 py-2.5 text-sm font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isLoading ? 'Sending Reset Link...' : 'Send Reset Link'}
            </button>

            <div className="pt-2 text-center text-sm text-slate-400">
              <Link
                href="/login"
                className="transition hover:text-emerald-400"
              >
                Back to Sign In
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
