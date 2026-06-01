'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import authService from '@/Services/AuthenticationService';

interface ProtectedRouteProps {
  children: React.ReactNode;
  allowedRoles: string[];
}
interface LoggedUser {
  accessToken: string;
  roles: string[];
  username: string;
  userId: string;
  email: string;
  tokenExpires: string;
  refreshToken: string;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  allowedRoles,
}) => {
  const router = useRouter();
  const pathname = usePathname();
  const [isAuthorized, setIsAuthorized] = useState<boolean | null>(null);

  useEffect(() => {
    const userString = localStorage.getItem('user');
    const accessToken = localStorage.getItem('accessToken');
    // If accessToken is present, we can assume the user is logged in
    let user: LoggedUser | null = null;

    if (userString) {
      try {
        user = JSON.parse(userString);
      } catch (error) {
        console.error('Error parsing user data from localStorage:', error);
        // Handle the error, e.g., redirect to login or clear corrupted data
        localStorage.removeItem('user');
        router.push('/login');
        return;
      }
    }

    if (!user || !user.accessToken || !accessToken) {
      router.push('/login');
      return;
    }

    // Check if token is expired
    const tokenExpires = new Date(user?.tokenExpires);
    const now = new Date();
    if (tokenExpires <= now || authService.getValidAccessToken() == null) {
      localStorage.removeItem('user');
      setIsAuthorized(false);
      router.push('/login'); // Redirect to login after token expiration
      return;
    }

    if (
      (allowedRoles && !allowedRoles.includes(user?.roles[0])) ||
      authService.hasRoleOptimized(allowedRoles) === false
    ) {
      router.push('/unauthorized');
      return;
    }

    setIsAuthorized(true);
  }, [router, pathname, allowedRoles]);

  // Show loading state while checking authorization
  if (isAuthorized === null) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-lg">Loading...</div>
      </div>
    );
  }

  // Show nothing if not authorized (redirect will happen)
  if (!isAuthorized) {
    return null;
  }

  // Render children if authorized
  return <>{children}</>;
};

export default ProtectedRoute;
