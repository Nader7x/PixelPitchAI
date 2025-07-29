'use client';

import SignalRDebugger from '@/app/Components/SignalRDebugger';
import ProtectedRoute from '@/app/Components/ProtectedRoute/ProtectedRoute';

export default function SignalRTestPage() {
  return (
    <ProtectedRoute allowedRoles={['Admin', 'User']}>
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="container mx-auto px-4">
          <h1 className="mb-8 text-center text-3xl font-bold text-gray-800">
            SignalR Real-Time Statistics Test
          </h1>
          <SignalRDebugger />
        </div>
      </div>
    </ProtectedRoute>
  );
}
