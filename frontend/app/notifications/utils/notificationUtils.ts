import React from 'react';
import {
  Target,
  CheckCircle,
  Play,
  AlertCircle,
  Info,
  AlertTriangle,
  Zap,
} from 'lucide-react';
import { NotificationType } from '@/Services/NotificationService';

export const getNotificationIcon = (type: NotificationType) => {
  const iconClass = 'h-5 w-5';
  switch (type) {
    case NotificationType.MatchStart:
      return React.createElement(Target, {
        className: `${iconClass} text-blue-500`,
      });
    case NotificationType.MatchEnd:
      return React.createElement(CheckCircle, {
        className: `${iconClass} text-green-500`,
      });
    case NotificationType.SimulationStart:
      return React.createElement(Play, {
        className: `${iconClass} text-purple-500`,
      });
    case NotificationType.SimulationEnd:
      return React.createElement(CheckCircle, {
        className: `${iconClass} text-purple-500`,
      });
    case NotificationType.Success:
      return React.createElement(CheckCircle, {
        className: `${iconClass} text-green-500`,
      });
    case NotificationType.Error:
      return React.createElement(AlertCircle, {
        className: `${iconClass} text-red-500`,
      });
    case NotificationType.Warning:
      return React.createElement(AlertTriangle, {
        className: `${iconClass} text-yellow-500`,
      });
    case NotificationType.SystemAlert:
      return React.createElement(Zap, {
        className: `${iconClass} text-orange-500`,
      });
    default:
      return React.createElement(Info, {
        className: `${iconClass} text-blue-500`,
      });
  }
};

export const getNotificationTypeColor = (
  type: NotificationType,
  isDarkMode: boolean
) => {
  const baseClasses = isDarkMode
    ? {
        matchStart: 'bg-blue-900/50 text-blue-300 border-blue-700',
        matchEnd: 'bg-blue-900/50 text-blue-300 border-blue-700',
        simulationStart: 'bg-purple-900/50 text-purple-300 border-purple-700',
        simulationEnd: 'bg-purple-900/50 text-purple-300 border-purple-700',
        success: 'bg-green-900/50 text-green-300 border-green-700',
        error: 'bg-red-900/50 text-red-300 border-red-700',
        warning: 'bg-yellow-900/50 text-yellow-300 border-yellow-700',
        systemAlert: 'bg-orange-900/50 text-orange-300 border-orange-700',
        default: 'bg-gray-700/50 text-gray-300 border-gray-600',
      }
    : {
        matchStart: 'bg-blue-100 text-blue-800 border-blue-200',
        matchEnd: 'bg-blue-100 text-blue-800 border-blue-200',
        simulationStart: 'bg-purple-100 text-purple-800 border-purple-200',
        simulationEnd: 'bg-purple-100 text-purple-800 border-purple-200',
        success: 'bg-green-100 text-green-800 border-green-200',
        error: 'bg-red-100 text-red-800 border-red-200',
        warning: 'bg-yellow-100 text-yellow-800 border-yellow-200',
        systemAlert: 'bg-orange-100 text-orange-800 border-orange-200',
        default: 'bg-gray-100 text-gray-800 border-gray-200',
      };

  switch (type) {
    case NotificationType.MatchStart:
    case NotificationType.MatchEnd:
      return baseClasses.matchStart;
    case NotificationType.SimulationStart:
    case NotificationType.SimulationEnd:
      return baseClasses.simulationStart;
    case NotificationType.Success:
      return baseClasses.success;
    case NotificationType.Error:
      return baseClasses.error;
    case NotificationType.Warning:
      return baseClasses.warning;
    case NotificationType.SystemAlert:
      return baseClasses.systemAlert;
    default:
      return baseClasses.default;
  }
};
