import React, { useState, useEffect } from 'react';
import notificationService from '@/Services/NotificationService';

export const NotificationSettings: React.FC = () => {
  const [autoRedirect, setAutoRedirect] = useState(false);

  useEffect(() => {
    // Load current preference
    const currentPref = notificationService.getAutoRedirectPreference();
    setAutoRedirect(currentPref);
  }, []);

  const handleToggleAutoRedirect = (enabled: boolean) => {
    setAutoRedirect(enabled);
    notificationService.setAutoRedirectPreference(enabled);
  };

  return (
    <div className="rounded-lg border bg-white p-4 shadow">
      <h3 className="mb-4 text-lg font-semibold">Notification Settings</h3>

      <div className="flex items-center justify-between">
        <div>
          <label className="font-medium text-gray-700">
            Auto-redirect to Match Simulation
          </label>
          <p className="text-sm text-gray-500">
            Automatically navigate to simulation view when a match starts
          </p>
        </div>

        <label className="relative inline-flex cursor-pointer items-center">
          <input
            type="checkbox"
            checked={autoRedirect}
            onChange={(e) => handleToggleAutoRedirect(e.target.checked)}
            className="peer sr-only"
          />
          <div className="peer h-6 w-11 rounded-full bg-gray-200 peer-checked:bg-blue-600 peer-focus:ring-4 peer-focus:ring-blue-300 peer-focus:outline-none after:absolute after:top-[2px] after:left-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:after:translate-x-full peer-checked:after:border-white dark:border-gray-600 dark:bg-gray-700 dark:peer-focus:ring-blue-800"></div>
        </label>
      </div>
    </div>
  );
};

export default NotificationSettings;
