'use client';

import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import React, { Suspense, useEffect, useState } from 'react';
import authService from '../../Services/AuthenticationService';

function ResetPasswordForm() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    const qEmail = searchParams.get('email') || '';
    const qToken = searchParams.get('token') || '';
    if (qEmail) setEmail(qEmail);
    if (qToken) setToken(qToken);
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (isLoading) return;

    if (!email.trim() || !token.trim()) {
      setError('Email and reset token are required.');
      return;
    }
    if (password.length < 8) {
      setError('Password must be at least 8 characters long.');
      return;
    }
    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await authService.resetPassword({
        email: email.trim(),
        token: token.trim(),
        password,
        confirmPassword,
      });
      setSuccess(true);
      setTimeout(() => router.push('/login'), 1500);
    } catch (err: any) {
      const msg =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        'Password reset failed. Verify your token and try again.';
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
          <h1 className="mt-2 text-2xl font-bold text-white">Reset Password</h1>
          <p className="mt-2 text-sm text-slate-400">
            Choose a new password for your PixelPitchAI account.
          </p>
        </div>

        {success ? (
          <div
            role="status"
            className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-4 text-center text-sm text-emerald-200"
          >
            Password reset succeeded. Redirecting to Sign In...
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label
                htmlFor="reset-email"
                className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-slate-300"
              >
                Email Address
              </label>
              <input
                id="reset-email"
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="coach@club.com"
                className="w-full rounded-lg border border-slate-700 bg-slate-950/70 px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:border-emerald-400 focus:outline-none"
              />
            </div>

            <div>
              <label
                htmlFor="reset-token"
                className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-slate-300"
              >
                Reset Token
              </label>
              <input
                id="reset-token"
                type="text"
                required
                value={token}
                onChange={(e) => setToken(e.target.value)}
                placeholder="Paste token from reset email"
                className="w-full rounded-lg border border-slate-700 bg-slate-950/70 px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:border-emerald-400 focus:outline-none"
              />
            </div>

            <div>
              <label
                htmlFor="reset-password"
                className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-slate-300"
              >
                New Password
              </label>
              <input
                id="reset-password"
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full rounded-lg border border-slate-700 bg-slate-950/70 px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:border-emerald-400 focus:outline-none"
              />
            </div>

            <div>
              <label
                htmlFor="reset-confirm-password"
                className="mb-1.5 block text-xs font-medium uppercase tracking-wider text-slate-300"
              >
                Confirm New Password
              </label>
              <input
                id="reset-confirm-password"
                type="password"
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
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
              {isLoading ? 'Resetting Password...' : 'Reset Password'}
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

export default function ResetPasswordPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-[#070b14] text-slate-400 flex items-center justify-center text-sm">
          Loading reset form...
        </div>
      }
    >
      <ResetPasswordForm />
    </Suspense>
  );
}
