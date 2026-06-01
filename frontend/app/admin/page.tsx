'use client';

import React from 'react';
import AdminDashboard from '../Components/AdminDashboard/AdminDashboard';
import ProtectedRoute from '../Components/ProtectedRoute/ProtectedRoute';
import { motion } from 'framer-motion';

export default function AdminDashboardPage() {
  return (
    <ProtectedRoute allowedRoles={['Admin']}>
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900">
        {/* Background decorative elements */}
        <div className="absolute top-40 left-10 h-80 w-80 animate-pulse rounded-full bg-blue-500 opacity-10 blur-3xl filter"></div>
        <div
          className="absolute right-10 bottom-20 h-96 w-96 animate-pulse rounded-full bg-green-500 opacity-10 blur-3xl filter"
          style={{ animationDelay: '2s' }}
        ></div>

        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.8 }}
          className="relative z-10 container mx-auto px-4 py-10 sm:px-6"
        >
          <AdminDashboard />

          {/* Footer section */}
          <motion.footer
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.5, duration: 0.5 }}
            className="mt-12 text-center text-sm text-gray-400"
          >
            <p>
              © {new Date().getFullYear()} Footex Admin Dashboard. All rights
              reserved.
            </p>
          </motion.footer>
        </motion.div>
      </div>
    </ProtectedRoute>
  );
}
