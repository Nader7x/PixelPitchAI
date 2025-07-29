import * as signalR from '@microsoft/signalr';
import authService from './AuthenticationService';
import { perfMonitor } from '@/lib/performanceMonitor';

export interface SignalREvent {
  simulationId: string;
  eventType:
    | 'match-start'
    | 'match-event'
    | 'match-end'
    | 'simulation-progress';
  data: any;
  timestamp: string;
}

export interface Score {
  home: number;
  away: number;
}

export interface MatchEventData {
  timestamp: string;
  time_seconds: number;
  minute: number;
  second: number;
  team: string;
  player: string;
  action: string;
  position: [number, number];
  outcome: string;
  height: string;
  card: string;
  pass_target: [number, number];
  shot_target: [number, number];
  body_part: string;
  event_type: string;
  type: string;
  event_index: number;
  match_id: string;
  home_team?: string;
  away_team?: string;
  long_pass?: boolean;
  pass_length?: number;
  Score?: {
    home: number;
    away: number;
  };
}
export interface SimulationProgressData {
  simulationId: string;
  matchId: number;
  progress: number;
  status: 'running' | 'completed' | 'failed';
  currentEvent?: number;
  totalEvents?: number;
}

export interface NotificationData {
  id: string;
  title: string;
  content: string;
  type: NotificationType;
  time: string;
  isRead: boolean;
  userId: string;
}

export enum NotificationType {
  MatchStart = 'MatchStart',
  MatchEnd = 'MatchEnd',
  SimulationStart = 'SimulationStart',
  SimulationEnd = 'SimulationEnd',
  MatchUpdate = 'MatchUpdate',
  SimulationUpdate = 'SimulationUpdate',
  SystemAlert = 'SystemAlert',
  UserMessage = 'UserMessage',
  Info = 'Info',
  Warning = 'Warning',
  Error = 'Error',
  Success = 'Success',
}

export interface MatchStatistics {
  matchId: number;
  timeStamp: string;
  homeTeam: {
    name: string;
    score: number;
    shots: number;
    shotsOnTarget: number;
    possession: number;
    passes: number;
    passAccuracy: number;
    corners: number;
    fouls: number;
    yellowCards: number;
    redCards: number;
    offsides: number;
  };
  awayTeam: {
    name: string;
    score: number;
    shots: number;
    shotsOnTarget: number;
    possession: number;
    passes: number;
    passAccuracy: number;
    corners: number;
    fouls: number;
    yellowCards: number;
    redCards: number;
    offsides: number;
  };
  matchInfo: {
    status: string;
    isLive: boolean;
    currentMinute: number;
    lastEventTime: number;
    eventType: string;
    eventTeam: string;
  };
  lastUpdated: string;
}

class SignalRService {
  private matchSimulationConnection: signalR.HubConnection | null = null;
  private notificationConnection: signalR.HubConnection | null = null;
  private readonly baseUrl =
    process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:7082';
  private isMatchSimulationConnected = false;
  private isNotificationConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000; // Start with 1 second
  private pageSpecificMatchEventHandler:
    | ((method: string, match_id: string, data: MatchEventData) => void)
    | null = null;
  private globalMatchEventHandler:
    | ((method: string, match_id: string, data: MatchEventData) => void)
    | null = null;

  constructor() {
    // Listen for logout events to disconnect SignalR
    authService.onLogout(() => {
      this.disconnectDueToAuth().then();
    });
  }

  /**
   * Initialize and start match simulation SignalR connection
   */
  public async connectMatchSimulation(): Promise<boolean> {
    try {
      if (this.matchSimulationConnection && this.isMatchSimulationConnected) {
        console.log('Match simulation SignalR already connected');
        return true;
      }

      // Get valid auth token for connection
      const token = await this.getAuthToken();
      if (!token) {
        console.warn(
          'No valid authentication token available for match simulation SignalR connection'
        );
        return false;
      }

      this.matchSimulationConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${this.baseUrl}/matchSimulationHub`, {
          accessTokenFactory: async () => {
            // Always get a fresh, valid token for each request
            const freshToken = await this.getAuthToken();
            return freshToken || '';
          },
          transport: signalR.HttpTransportType.WebSockets,
          skipNegotiation: true,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
              return Math.min(
                1000 * Math.pow(2, retryContext.previousRetryCount),
                30000
              );
            }
            return null; // Stop retrying
          },
        })
        .configureLogging(signalR.LogLevel.Information)
        .build(); // Set up event handlers for match simulation
      this.setupMatchSimulationEventHandlers();

      // Start connection
      await this.matchSimulationConnection.start();
      this.isMatchSimulationConnected = true;

      // Ensure global handler is active after connection
      this.ensureGlobalMatchEventHandler();

      console.log('Match simulation SignalR connected successfully');
      return true;
    } catch (error) {
      console.error('Match simulation SignalR connection failed:', error);
      this.isMatchSimulationConnected = false;
      return false;
    }
  }

  /**
   * Initialize and start notification SignalR connection
   */
  public async connectNotifications(): Promise<boolean> {
    try {
      if (this.notificationConnection && this.isNotificationConnected) {
        console.log('Notification SignalR already connected');
        return true;
      }

      // Get valid auth token for connection
      const token = await this.getAuthToken();
      if (!token) {
        console.warn(
          'No valid authentication token available for notification SignalR connection'
        );
        return false;
      }

      this.notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${this.baseUrl}/Notify`, {
          accessTokenFactory: async () => {
            // Always get a fresh, valid token for each request
            const freshToken = await this.getAuthToken();
            return freshToken || '';
          },
          transport: signalR.HttpTransportType.WebSockets,
          skipNegotiation: true,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
              return Math.min(
                1000 * Math.pow(2, retryContext.previousRetryCount),
                30000
              );
            }
            return null; // Stop retrying
          },
        })
        .configureLogging(signalR.LogLevel.Information)
        .build(); // Set up event handlers for notifications
      this.setupNotificationEventHandlers();

      // Add immediate universal handlers right after setup
      console.log(
        '🌐 [SignalR] Adding immediate universal handlers after connection setup'
      );
      this.notificationConnection.on('sendmessageasync', (...args: any[]) => {
        console.log(
          '🎯 [SignalR IMMEDIATE] Caught sendmessageasync immediately after connection:',
          args
        );
      });

      // Start connection
      await this.notificationConnection.start();
      this.isNotificationConnected = true;

      console.log('Notification SignalR connected successfully');
      console.log(
        `🔗 [SignalR] Notification connection established, state: ${this.notificationConnection.state}`
      );

      return true;
    } catch (error) {
      console.error('Notification SignalR connection failed:', error);
      this.isNotificationConnected = false;
      return false;
    }
  }

  /**
   * Initialize and start both SignalR connections
   */
  public async connect(): Promise<boolean> {
    const matchSimulationConnected = await this.connectMatchSimulation();
    const notificationConnected = await this.connectNotifications();

    // Reset reconnect attempts if at least one connection succeeded
    if (matchSimulationConnected || notificationConnected) {
      this.reconnectAttempts = 0;
    }

    return matchSimulationConnected && notificationConnected;
  }

  /**
   * Disconnect from match simulation SignalR hub
   */
  public async disconnectMatchSimulation(): Promise<void> {
    try {
      if (this.matchSimulationConnection) {
        await this.matchSimulationConnection.stop();
        this.matchSimulationConnection = null;
        this.isMatchSimulationConnected = false;
        console.log('Match simulation SignalR disconnected');
      }
    } catch (error) {
      console.error('Error disconnecting match simulation SignalR:', error);
    }
  }

  /**
   * Disconnect from notification SignalR hub
   */
  public async disconnectNotifications(): Promise<void> {
    try {
      if (this.notificationConnection) {
        await this.notificationConnection.stop();
        this.notificationConnection = null;
        this.isNotificationConnected = false;
        console.log('Notification SignalR disconnected');
      }
    } catch (error) {
      console.error('Error disconnecting notification SignalR:', error);
    }
  }

  /**
   * Disconnect from all SignalR hubs
   */
  public async disconnect(): Promise<void> {
    this.reconnectAttempts = 0; // Reset reconnect attempts on manual disconnect
    await Promise.all([
      this.disconnectMatchSimulation(),
      this.disconnectNotifications(),
    ]);
    console.log('All SignalR connections disconnected');
  }

  /**
   * Disconnect due to authentication failure
   */
  public async disconnectDueToAuth(): Promise<void> {
    console.log('Disconnecting SignalR due to authentication failure');
    this.reconnectAttempts = this.maxReconnectAttempts; // Prevent auto-reconnection
    await this.disconnect();
  }

  /**
   * Join a simulation room to receive real-time updates
   */
  public async joinSimulation(matchId: number): Promise<boolean> {
    try {
      // Ensure we have an active match simulation connection
      if (!this.isMatchSimulationConnected || !this.matchSimulationConnection) {
        const connected = await this.connectMatchSimulation();
        if (!connected) return false;
      }

      await this.matchSimulationConnection!.invoke('JoinMatchGroup', matchId);
      console.log(`Joined simulation: ${matchId.toString()}`);
      return true;
    } catch (error) {
      console.error('Error joining simulation:', error);
      // If it's an auth error, handle it appropriately
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      if (
        errorMessage.includes('401') ||
        errorMessage.includes('Unauthorized')
      ) {
        await this.disconnectDueToAuth();
      }
      return false;
    }
  }

  /**
   * Leave a simulation room
   */
  public async leaveSimulation(matchId: number): Promise<boolean> {
    try {
      if (!this.matchSimulationConnection || !this.isMatchSimulationConnected) {
        return false;
      }

      await this.matchSimulationConnection.invoke('LeaveMatchGroup', matchId);
      console.log(`Left simulation: ${matchId.toString()}`);
      return true;
    } catch (error) {
      console.error('Error leaving simulation:', error);
      return false;
    }
  } /**
   * Subscribe to match events for a specific simulation
   */
  public onMatchEvent(
    callback: (method: string, match_id: string, data: MatchEventData) => void
  ): void {
    if (this.matchSimulationConnection) {
      // Remove existing page-specific listener if it exists
      if (this.pageSpecificMatchEventHandler) {
        this.matchSimulationConnection.off(
          'SendMatchEventAsync',
          this.pageSpecificMatchEventHandler
        );
      }

      // Store reference to new page-specific handler
      this.pageSpecificMatchEventHandler = callback;

      // Add the new page-specific listener
      this.matchSimulationConnection.on('SendMatchEventAsync', callback);

      console.log('[SignalR] Page-specific match event listener added');
    }
  }

  /**
   * Subscribe to simulation progress updates
   */
  public onSimulationProgress(
    callback: (data: SimulationProgressData) => void
  ): void {
    if (this.matchSimulationConnection) {
      // Remove existing listener to prevent duplicates
      this.matchSimulationConnection.off('SimulationProgress');
      this.matchSimulationConnection.on('SimulationProgress', callback);
    }
  }

  /**
   * Subscribe to simulation completion
   */
  public onSimulationComplete(
    callback: (
      simulationId: string,
      finalScore: { home: number; away: number }
    ) => void
  ): void {
    if (this.matchSimulationConnection) {
      // Remove existing listener to prevent duplicates
      this.matchSimulationConnection.off('SimulationComplete');
      this.matchSimulationConnection.on('SimulationComplete', callback);
    }
  }

  /**
   * Subscribe to simulation errors
   */
  public onSimulationError(
    callback: (matchId: number, error: string) => void
  ): void {
    if (this.matchSimulationConnection) {
      // Remove existing listener to prevent duplicates
      this.matchSimulationConnection.off('SimulationError');
      this.matchSimulationConnection.on('SimulationError', callback);
    }
  }

  /**
   * Subscribe to notification events
   */ public onNotification(callback: (data: NotificationData) => void): void {
    if (this.notificationConnection) {
      // Remove any existing listeners before adding new one to prevent duplicates
      this.notificationConnection.off('SendNotificationAsync');
      this.notificationConnection.off('sendnotificationasync');

      // Register both casing variants to handle backend inconsistency
      this.notificationConnection.on('SendNotificationAsync', callback);
      this.notificationConnection.on('sendnotificationasync', callback);
    }
  }
  public onSimulationStartNotification(
    callback: (notification: NotificationData, simulationId: string) => void
  ): void {
    if (this.notificationConnection) {
      // Remove any existing listeners before adding new one to prevent duplicates
      this.notificationConnection.off('SendSimulationUpdateNotificationAsync');
      this.notificationConnection.off('sendsimulationupdatenotificationasync');

      // Register both casing variants to handle backend inconsistency
      this.notificationConnection.on(
        'SendSimulationUpdateNotificationAsync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Simulation update notification received (PascalCase)'
          );
          callback(notification, simulationId);
        }
      );
      this.notificationConnection.on(
        'sendsimulationupdatenotificationasync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Simulation update notification received (lowercase)'
          );
          callback(notification, simulationId);
        }
      );
    }
  }
  public onMatchStartNotificationAsync(
    callback: (notification: NotificationData, simulationId: string) => void
  ): void {
    if (this.notificationConnection) {
      // Remove any existing listeners before adding new one to prevent duplicates
      this.notificationConnection.off('SendMatchStartNotificationAsync');
      this.notificationConnection.off('sendmatchstartnotificationasync');

      // Register both casing variants to handle backend inconsistency
      this.notificationConnection.on(
        'SendMatchStartNotificationAsync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Match start notification received (PascalCase)'
          );
          callback(notification, simulationId);
        }
      );
      this.notificationConnection.on(
        'sendmatchstartnotificationasync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Match start notification received (lowercase)'
          );
          callback(notification, simulationId);
        }
      );
    }
  }
  public onMatchEndNotificationAsync(
    callback: (notification: NotificationData, simulationId: string) => void
  ): void {
    if (this.notificationConnection) {
      // Remove any existing listeners before adding new one to prevent duplicates
      this.notificationConnection.off('SendMatchEndNotificationAsync');
      this.notificationConnection.off('sendmatchendnotificationasync');

      // Register both casing variants to handle backend inconsistency
      this.notificationConnection.on(
        'SendMatchEndNotificationAsync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Match end notification received (PascalCase)'
          );
          callback(notification, simulationId);
        }
      );
      this.notificationConnection.on(
        'sendmatchendnotificationasync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Match end notification received (lowercase)'
          );
          callback(notification, simulationId);
        }
      );
    }
  }
  public onMatchUpdateNotificationAsync(
    callback: (notification: NotificationData, simulationId: string) => void
  ): void {
    if (this.notificationConnection) {
      // Remove any existing listeners before adding new one to prevent duplicates
      this.notificationConnection.off('SendMatchUpdateNotificationAsync');
      this.notificationConnection.off('sendmatchupdatenotificationasync');

      // Register both casing variants to handle backend inconsistency
      this.notificationConnection.on(
        'SendMatchUpdateNotificationAsync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Match update notification received (PascalCase)'
          );
          callback(notification, simulationId);
        }
      );
      this.notificationConnection.on(
        'sendmatchupdatenotificationasync',
        (notification: NotificationData, simulationId: string) => {
          console.log(
            '🎯 [SignalR] Match update notification received (lowercase)'
          );
          callback(notification, simulationId);
        }
      );
    }
  }
  public onMessageAsync(callback: (message: string) => void): void {
    if (this.notificationConnection) {
      console.log(
        '🔔 [SignalR] Registering onMessageAsync handlers for both casing variants'
      );

      // Remove any existing listeners before adding new one to prevent duplicates
      this.notificationConnection.off('SendMessageAsync');
      this.notificationConnection.off('sendmessageasync');

      // Register for both PascalCase and lowercase variants
      // this.notificationConnection.on('SendMessageAsync', (message: string) => {
      //   console.log('🎯 [SignalR] SendMessageAsync (PascalCase) received:', message);
      //   callback(message);
      // });
      this.notificationConnection.on('sendmessageasync', (message: string) => {
        console.log(
          '🎯 [SignalR] sendmessageasync (lowercase) received:',
          message
        );
        callback(message);
      });

      console.log(
        '✅ [SignalR] onMessageAsync handlers registered for both variants'
      );
    } else {
      console.warn(
        '⚠️ [SignalR] Cannot register onMessageAsync - notification connection not available'
      );
    }
  }

  /**
   * Subscribe to generic send events (for custom method names)
   */
  public onSendAsync(method: string, callback: (...args: any[]) => void): void {
    if (this.notificationConnection) {
      // Remove any existing listeners before adding new one to prevent duplicates
      this.notificationConnection.off(method);
      this.notificationConnection.on(method, callback);
    }
  }

  /**
   * Subscribe to real-time match statistics updates
   */
  public onMatchStatisticsUpdate(
    callback: (
      method: string,
      matchId: number,
      matchStatistics: MatchStatistics
    ) => void
  ): void {
    if (this.matchSimulationConnection) {
      // Remove existing listener to prevent duplicates
      this.matchSimulationConnection.off('SendMatchStatisticsAsync');
      this.matchSimulationConnection.on('SendMatchStatisticsAsync', callback);
    }
  }

  /**
   * Join match statistics group to receive real-time updates
   */
  public async joinMatchStatistics(matchId: number): Promise<boolean> {
    try {
      if (!this.isMatchSimulationConnected || !this.matchSimulationConnection) {
        const connected = await this.connectMatchSimulation();
        if (!connected) return false;
      }

      await this.matchSimulationConnection!.invoke(
        'JoinMatchStatistics',
        matchId
      );
      console.log(`Joined match statistics for match: ${matchId}`);
      return true;
    } catch (error) {
      console.error('Error joining match statistics:', error);
      return false;
    }
  }

  /**
   * Leave match statistics group
   */
  public async leaveMatchStatistics(matchId: number): Promise<boolean> {
    try {
      if (this.isMatchSimulationConnected && this.matchSimulationConnection) {
        await this.matchSimulationConnection.invoke(
          'LeaveMatchStatistics',
          matchId
        );
        console.log(`Left match statistics for match: ${matchId}`);
      }
      return true;
    } catch (error) {
      console.error('Error leaving match statistics:', error);
      return false;
    }
  }
  /**
   * Remove notification event listeners
   */
  public removeNotificationListener(): void {
    if (this.notificationConnection) {
      this.notificationConnection.off('SendNotificationAsync');
      this.notificationConnection.off('SendSimulationUpdateNotificationAsync');
      this.notificationConnection.off('SendMatchStartNotificationAsync');
      this.notificationConnection.off('SendMatchEndNotificationAsync');
      this.notificationConnection.off('SendMatchUpdateNotificationAsync');
      this.notificationConnection.off('SendMessageAsync');

      // Also remove lowercase variants
      this.notificationConnection.off('sendnotificationasync');
      this.notificationConnection.off('sendsimulationupdatenotificationasync');
      this.notificationConnection.off('sendmatchstartnotificationasync');
      this.notificationConnection.off('sendmatchendnotificationasync');
      this.notificationConnection.off('sendmatchupdatenotificationasync');
      this.notificationConnection.off('sendmessageasync');
    }
  }
  /**
   * Remove all event listeners
   */
  public removeAllListeners(): void {
    // Remove match simulation event listeners
    if (this.matchSimulationConnection) {
      this.matchSimulationConnection.off('MatchEvent');
      this.matchSimulationConnection.off('SimulationProgress');
      this.matchSimulationConnection.off('SimulationComplete');
      this.matchSimulationConnection.off('SimulationError');
      this.matchSimulationConnection.off('matchevent');
      this.matchSimulationConnection.off('simulationprogress');
      this.matchSimulationConnection.off('simulationcomplete');
      this.matchSimulationConnection.off('simulationerror');

      // Remove page-specific listener if it exists
      if (this.pageSpecificMatchEventHandler) {
        this.matchSimulationConnection.off(
          'SendMatchEventAsync',
          this.pageSpecificMatchEventHandler
        );
        this.pageSpecificMatchEventHandler = null;
      }

      // Remove global listener if it exists
      if (this.globalMatchEventHandler) {
        this.matchSimulationConnection.off(
          'SendMatchEventAsync',
          this.globalMatchEventHandler
        );
        this.globalMatchEventHandler = null;
      }
    } // Remove notification event listeners
    if (this.notificationConnection) {
      this.notificationConnection.off('SendNotificationAsync');
      this.notificationConnection.off('SendSimulationUpdateNotificationAsync');
      this.notificationConnection.off('SendMatchStartNotificationAsync');
      this.notificationConnection.off('SendMatchEndNotificationAsync');
      this.notificationConnection.off('SendMatchUpdateNotificationAsync');
      this.notificationConnection.off('SendMessageAsync');

      // Also remove lowercase variants
      this.notificationConnection.off('sendnotificationasync');
      this.notificationConnection.off('sendsimulationupdatenotificationasync');
      this.notificationConnection.off('sendmatchstartnotificationasync');
      this.notificationConnection.off('sendmatchendnotificationasync');
      this.notificationConnection.off('sendmatchupdatenotificationasync');
      this.notificationConnection.off('sendmessageasync');
    }
  }

  /**
   * Check if currently connected to both hubs
   */
  public isConnectionActive(): boolean {
    return this.isMatchSimulationConnected && this.isNotificationConnected;
  }

  /**
   * Check if match simulation connection is active
   */
  public isMatchSimulationActive(): boolean {
    return (
      this.isMatchSimulationConnected &&
      this.matchSimulationConnection !== null &&
      this.matchSimulationConnection.state ===
        signalR.HubConnectionState.Connected
    );
  }

  /**
   * Check if notification connection is active
   */
  public isNotificationActive(): boolean {
    return (
      this.isNotificationConnected &&
      this.notificationConnection !== null &&
      this.notificationConnection.state === signalR.HubConnectionState.Connected
    );
  }

  /**
   * Get connection state for match simulation
   */
  public getMatchSimulationConnectionState(): string {
    if (!this.matchSimulationConnection) return 'Disconnected';

    switch (this.matchSimulationConnection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected';
      case signalR.HubConnectionState.Disconnecting:
        return 'Disconnecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      default:
        return 'Unknown';
    }
  }

  /**
   * Get connection state for notifications
   */
  public getNotificationConnectionState(): string {
    if (!this.notificationConnection) return 'Disconnected';

    switch (this.notificationConnection.state) {
      case signalR.HubConnectionState.Connected:
        return 'Connected';
      case signalR.HubConnectionState.Connecting:
        return 'Connecting';
      case signalR.HubConnectionState.Disconnected:
        return 'Disconnected';
      case signalR.HubConnectionState.Disconnecting:
        return 'Disconnecting';
      case signalR.HubConnectionState.Reconnecting:
        return 'Reconnecting';
      default:
        return 'Unknown';
    }
  }
  /**
   * Set up match simulation SignalR event handlers
   */ private setupMatchSimulationEventHandlers(): void {
    if (!this.matchSimulationConnection) return;

    // Add universal method handlers to catch any unregistered method calls
    this.addUniversalMethodHandlersForConnection(
      this.matchSimulationConnection,
      'match-simulation'
    );

    this.matchSimulationConnection.onreconnecting((error: any) => {
      console.log('Match simulation SignalR attempting to reconnect:', error);
      this.isMatchSimulationConnected = false;
    });

    this.matchSimulationConnection.onreconnected(async (connectionId: any) => {
      console.log(
        'Match simulation SignalR reconnected with ID:',
        connectionId
      );
      this.isMatchSimulationConnected = true;
      this.reconnectAttempts = 0;

      // Re-add universal method handlers after reconnection
      this.addUniversalMethodHandlersForConnection(
        this.matchSimulationConnection!,
        'match-simulation'
      );

      // Verify authentication after reconnection
      if (!authService.isAuthenticated()) {
        console.warn(
          'User not authenticated after match simulation reconnection, disconnecting'
        );
        await this.disconnectMatchSimulation();
      } else {
        // Ensure global handler is reattached after reconnection
        this.ensureGlobalMatchEventHandler();
        console.log('[SignalR] Global handler reattached after reconnection');
      }
    });

    this.matchSimulationConnection.onclose(async (error: any) => {
      console.log('Match simulation SignalR connection closed:', error);
      this.isMatchSimulationConnected = false;

      // Check if the connection was closed due to authentication issues
      const errorMessage =
        error instanceof Error ? error.message : String(error || '');
      if (
        errorMessage.includes('401') ||
        errorMessage.includes('Unauthorized')
      ) {
        console.log(
          'Match simulation connection closed due to authentication failure'
        );
        return; // Don't attempt reconnection for auth failures
      }

      // Only attempt reconnection if user is still authenticated
      if (
        authService.isAuthenticated() &&
        this.reconnectAttempts < this.maxReconnectAttempts
      ) {
        setTimeout(
          () => {
            this.attemptMatchSimulationReconnection();
          },
          this.reconnectDelay * Math.pow(2, this.reconnectAttempts)
        );
      }
    }); // Global listener for match_start events to update localStorage regardless of page
    this.globalMatchEventHandler = (
      method: string,
      match_id: string,
      eventData: MatchEventData
    ) => {
      const startTime = performance.now();

      try {
        console.log(
          `[SignalR Global] Received match event: ${eventData.event_type} for match ${match_id}`
        );
        console.log(`[SignalR Global] Event data:`, eventData);

        if (eventData.event_type === 'match_start') {
          console.log(
            '[SignalR Global] Processing match_start event for global localStorage update'
          );
          console.log('[SignalR Global] Team names from event:', {
            home: eventData.home_team,
            away: eventData.away_team,
          });

          // Import teamStorage functions dynamically to avoid circular dependencies
          import('../lib/teamStorage')
            .then((teamStorage) => {
              // Store match ID
              if (match_id) {
                teamStorage.storeMatchId(match_id);
                console.log(`[SignalR Global] ✅ Stored match ID: ${match_id}`);
              }

              // Store team names
              if (eventData.home_team && eventData.away_team) {
                teamStorage.storeTeamNames(
                  eventData.home_team,
                  eventData.away_team
                );
                console.log(
                  `[SignalR Global] ✅ Stored team names - Home: ${eventData.home_team}, Away: ${eventData.away_team}`
                );
              } else {
                console.warn(
                  `[SignalR Global] ⚠️ Team names missing - Home: ${eventData.home_team}, Away: ${eventData.away_team}`
                );
              }

              // Store initial scores
              if (eventData.Score) {
                teamStorage.storeScores(
                  eventData.Score.home,
                  eventData.Score.away
                );
                console.log(
                  `[SignalR Global] ✅ Stored initial scores - Home: ${eventData.Score.home}, Away: ${eventData.Score.away}`
                );
              } else {
                // Default to 0-0 for match start
                teamStorage.storeScores(0, 0);
                console.log(
                  `[SignalR Global] ✅ Stored default scores - Home: 0, Away: 0`
                );
              }

              // Store match time
              if (eventData.time_seconds !== undefined) {
                teamStorage.storeMatchTime(eventData.time_seconds);
                console.log(
                  `[SignalR Global] ✅ Stored match time: ${eventData.time_seconds} seconds`
                );
              }

              console.log(
                '[SignalR Global] 🎉 Global localStorage update completed for match_start event'
              );

              // Debug: Log localStorage contents
              if (typeof window !== 'undefined') {
                console.log('[SignalR Global] 📊 localStorage after update:', {
                  matchId: localStorage.getItem('matchId'),
                  homeTeamName: localStorage.getItem('homeTeamName'),
                  awayTeamName: localStorage.getItem('awayTeamName'),
                  homeScore: localStorage.getItem('homeScore'),
                  awayScore: localStorage.getItem('awayScore'),
                  matchTime: localStorage.getItem('matchTime'),
                });
              }
            })
            .catch((error) => {
              console.error(
                '[SignalR Global] ❌ Failed to import teamStorage:',
                error
              );
            });
        } else {
          // Log other events for debugging
          console.log(
            `[SignalR Global] 📋 Other event received: ${eventData.event_type}`
          );
        }

        // Record performance metrics
        const processingTime = performance.now() - startTime;
        perfMonitor.recordSignalREvent(eventData.event_type, processingTime);
      } catch (error) {
        console.error(
          '[SignalR Global] ❌ Error in global match event handler:',
          error
        );
        const processingTime = performance.now() - startTime;
        perfMonitor.recordSignalREvent(
          `${eventData.event_type}_ERROR`,
          processingTime
        );
      }
    };

    // Add the global listener
    this.matchSimulationConnection.on(
      'SendMatchEventAsync',
      this.globalMatchEventHandler
    );
  }

  /**
   * Set up notification SignalR event handlers
   */ private setupNotificationEventHandlers(): void {
    if (!this.notificationConnection) return;

    // Add universal method handlers to catch any unregistered method calls
    this.addUniversalMethodHandlersForConnection(
      this.notificationConnection,
      'notification'
    );

    this.notificationConnection.onreconnecting((error: any) => {
      console.log('Notification SignalR attempting to reconnect:', error);
      this.isNotificationConnected = false;
    });

    this.notificationConnection.onreconnected(async (connectionId: any) => {
      console.log('Notification SignalR reconnected with ID:', connectionId);
      this.isNotificationConnected = true;
      this.reconnectAttempts = 0;

      // Re-add universal method handlers after reconnection
      this.addUniversalMethodHandlersForConnection(
        this.notificationConnection!,
        'notification'
      );

      // Verify authentication after reconnection
      if (!authService.isAuthenticated()) {
        console.warn(
          'User not authenticated after notification reconnection, disconnecting'
        );
        await this.disconnectNotifications();
      }
    });

    this.notificationConnection.onclose(async (error: any) => {
      console.log('Notification SignalR connection closed:', error);
      this.isNotificationConnected = false;

      // Check if the connection was closed due to authentication issues
      const errorMessage =
        error instanceof Error ? error.message : String(error || '');
      if (
        errorMessage.includes('401') ||
        errorMessage.includes('Unauthorized')
      ) {
        console.log(
          'Notification connection closed due to authentication failure'
        );
        return; // Don't attempt reconnection for auth failures
      }

      // Only attempt reconnection if user is still authenticated
      if (
        authService.isAuthenticated() &&
        this.reconnectAttempts < this.maxReconnectAttempts
      ) {
        setTimeout(
          () => {
            this.attemptNotificationReconnection();
          },
          this.reconnectDelay * Math.pow(2, this.reconnectAttempts)
        );
      }
    });
  }

  /**
   * Attempt manual reconnection for match simulation with authentication check
   */
  private async attemptMatchSimulationReconnection(): Promise<void> {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.log('Max match simulation reconnection attempts reached');
      return;
    }

    // Check if user is still authenticated before attempting reconnection
    if (!authService.isAuthenticated()) {
      console.log(
        'User not authenticated, skipping match simulation reconnection'
      );
      return;
    }

    this.reconnectAttempts++;
    console.log(
      `Attempting match simulation reconnection ${this.reconnectAttempts}/${this.maxReconnectAttempts}`
    );

    try {
      await this.connectMatchSimulation();
    } catch (error) {
      console.error(
        `Match simulation reconnection attempt ${this.reconnectAttempts} failed:`,
        error
      );
    }
  }

  /**
   * Attempt manual reconnection for notifications with authentication check
   */
  private async attemptNotificationReconnection(): Promise<void> {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.log('Max notification reconnection attempts reached');
      return;
    }

    // Check if user is still authenticated before attempting reconnection
    if (!authService.isAuthenticated()) {
      console.log('User not authenticated, skipping notification reconnection');
      return;
    }

    this.reconnectAttempts++;
    console.log(
      `Attempting notification reconnection ${this.reconnectAttempts}/${this.maxReconnectAttempts}`
    );

    try {
      await this.connectNotifications();
    } catch (error) {
      console.error(
        `Notification reconnection attempt ${this.reconnectAttempts} failed:`,
        error
      );
    }
  }

  /**
   * Get authentication token for SignalR connection with automatic refresh
   */
  private async getAuthToken(): Promise<string | null> {
    try {
      // Use authentication service to get a valid token with automatic refresh
      return await authService.getValidAccessToken();
    } catch (error) {
      console.error('Error getting auth token for SignalR:', error);
      return null;
    }
  }

  /**
   * Send a message to the match simulation hub (for testing purposes)
   */
  public async sendMatchSimulationMessage(
    method: string,
    ...args: any[]
  ): Promise<any> {
    try {
      if (!this.matchSimulationConnection || !this.isMatchSimulationConnected) {
        throw new Error('Match simulation SignalR not connected');
      }

      return await this.matchSimulationConnection.invoke(method, ...args);
    } catch (error) {
      console.error('Error sending match simulation SignalR message:', error);
      throw error;
    }
  }

  /**
   * Send a message to the notification hub (for testing purposes)
   */
  public async sendNotificationMessage(
    method: string,
    ...args: any[]
  ): Promise<any> {
    try {
      if (!this.notificationConnection || !this.isNotificationConnected) {
        throw new Error('Notification SignalR not connected');
      }

      return await this.notificationConnection.invoke(method, ...args);
    } catch (error) {
      console.error('Error sending notification SignalR message:', error);
      throw error;
    }
  }

  /**
   * Get connection statistics for debugging
   */
  public getConnectionStats(): any {
    return {
      matchSimulation: {
        isConnected: this.isMatchSimulationConnected,
        connectionState: this.getMatchSimulationConnectionState(),
        hasConnection: this.matchSimulationConnection !== null,
      },
      notification: {
        isConnected: this.isNotificationConnected,
        connectionState: this.getNotificationConnectionState(),
        hasConnection: this.notificationConnection !== null,
      },
      reconnectAttempts: this.reconnectAttempts,
      maxReconnectAttempts: this.maxReconnectAttempts,
      isAuthenticated: authService.isAuthenticated(),
    };
  }

  /**
   * Ensure both connections are active and authenticated
   */
  public async ensureConnection(): Promise<boolean> {
    // Check if user is authenticated
    if (!authService.isAuthenticated()) {
      console.warn(
        'User not authenticated, cannot establish SignalR connections'
      );
      await this.disconnectDueToAuth();
      return false;
    }

    // Check if connections are active
    const matchSimulationActive = this.isMatchSimulationActive();
    const notificationActive = this.isNotificationActive();

    if (matchSimulationActive && notificationActive) {
      return true;
    }

    // Try to connect
    return await this.connect();
  }

  /**
   * Reset connection state and force reconnection
   */
  public async resetConnection(): Promise<boolean> {
    console.log('Resetting SignalR connections');
    this.reconnectAttempts = 0;
    await this.disconnect();
    return await this.connect();
  }

  /**
   * Clean up SignalR service (remove event listeners, disconnect)
   */
  public async cleanup(): Promise<void> {
    console.log('Cleaning up SignalR service');
    await this.disconnect();
    this.removeAllListeners();
    // Note: In a real cleanup scenario, you'd want to remove the logout listener
    // but since we're using a singleton, we'll keep it for the lifetime of the app
  }
  /**
   * Ensure global match event handler is active
   * Call this after establishing connection to guarantee global listener is working
   */
  public ensureGlobalMatchEventHandler(): boolean {
    if (!this.matchSimulationConnection) {
      console.warn(
        '[SignalR] No match simulation connection available for global handler'
      );
      return false;
    }

    // Check if global handler exists
    if (this.globalMatchEventHandler) {
      // Re-attach the global handler to ensure it's active
      console.log('[SignalR] Ensuring global match event handler is active');
      this.matchSimulationConnection.on(
        'SendMatchEventAsync',
        this.globalMatchEventHandler
      );
      return true;
    }

    console.warn('[SignalR] Global match event handler not initialized');
    return false;
  }

  /**
   * Check if SignalR is globally initialized and ready
   * Use this to prevent duplicate connections from individual pages
   */
  public isGloballyConnected(): boolean {
    return this.isMatchSimulationActive() && this.isNotificationActive();
  }

  /**
   * Ensure connection for page-specific needs (smart connection)
   * Only connects if not already globally connected
   */
  public async ensurePageConnection(): Promise<boolean> {
    if (this.isGloballyConnected()) {
      console.log(
        '[SignalR] Already globally connected, skipping page-specific connection'
      );
      return true;
    }

    console.log(
      '[SignalR] Not globally connected, establishing page-specific connection'
    );
    return await this.connect();
  }

  /**
   * ULTRA-FAST connection with aggressive optimization for minimum latency
   * This bypasses normal checks and connects immediately for fastest possible event reception
   */
  public async connectUltraFast(): Promise<boolean> {
    console.log(
      '[SignalR ULTRA-FAST] ⚡ Attempting ZERO-LATENCY connection...'
    );

    try {
      // Try to connect match simulation with aggressive settings
      if (!this.matchSimulationConnection || !this.isMatchSimulationConnected) {
        this.matchSimulationConnection = new signalR.HubConnectionBuilder()
          .withUrl(`${this.baseUrl}/matchSimulationHub`, {
            accessTokenFactory: async () => {
              const token = await this.getAuthToken();
              return token || '';
            },
            transport: signalR.HttpTransportType.WebSockets,
            skipNegotiation: true,
            timeout: 5000, // Reduced timeout for faster failure detection
          })
          .withAutomaticReconnect([0, 100, 500, 1000, 2000]) // Aggressive reconnection
          .configureLogging(signalR.LogLevel.Warning) // Reduced logging for performance
          .build();

        // Set up event handlers immediately
        this.setupMatchSimulationEventHandlers();

        // Start connection with minimum delay
        await this.matchSimulationConnection.start();
        this.isMatchSimulationConnected = true;
        this.ensureGlobalMatchEventHandler();

        console.log(
          '[SignalR ULTRA-FAST] ⚡✅ MATCH SIMULATION - ZERO-LATENCY READY!'
        );
      }

      // Try to connect notifications with aggressive settings
      if (!this.notificationConnection || !this.isNotificationConnected) {
        this.notificationConnection = new signalR.HubConnectionBuilder()
          .withUrl(`${this.baseUrl}/Notify`, {
            accessTokenFactory: async () => {
              const token = await this.getAuthToken();
              return token || '';
            },
            transport: signalR.HttpTransportType.WebSockets,
            skipNegotiation: true,
            timeout: 5000,
          })
          .withAutomaticReconnect([0, 100, 500, 1000, 2000])
          .configureLogging(signalR.LogLevel.Warning)
          .build();

        this.setupNotificationEventHandlers();
        await this.notificationConnection.start();
        this.isNotificationConnected = true;

        console.log(
          '[SignalR ULTRA-FAST] ⚡✅ NOTIFICATIONS - ZERO-LATENCY READY!'
        );
      }

      console.log(
        '[SignalR ULTRA-FAST] 🏎️💨 ULTRA-FAST CONNECTION COMPLETE - READY TO INTERCEPT!'
      );
      return true;
    } catch (error) {
      console.warn(
        '[SignalR ULTRA-FAST] ⚠️ Ultra-fast connection failed, falling back to normal:',
        error
      );
      // Fallback to normal connection
      return await this.connect();
    }
  }

  /**
   * Debug method to log all incoming SignalR method calls and detect missing handlers
   */
  public enableComprehensiveDebugMode(): void {
    console.log(
      '🔍 [SignalR DEBUG] Enabling comprehensive debug mode for method detection'
    );

    if (this.matchSimulationConnection) {
      // Hook into the connection's internal message handler
      const originalOn = this.matchSimulationConnection.on.bind(
        this.matchSimulationConnection
      );

      // Override the 'on' method to log all registrations
      (this.matchSimulationConnection as any).on = function (
        methodName: string,
        handler: any
      ) {
        console.log(
          `🔗 [SignalR DEBUG] Registering handler for: "${methodName}"`
        );
        return originalOn(methodName, handler);
      };

      // Add a catch-all handler for unregistered methods
      this.matchSimulationConnection.onclose((error) => {
        if (error) {
          console.error(
            '🔥 [SignalR DEBUG] Match simulation connection closed with error:',
            error
          );
        }
      });
    }

    if (this.notificationConnection) {
      // Hook into the connection's internal message handler
      const originalOn = this.notificationConnection.on.bind(
        this.notificationConnection
      );

      // Override the 'on' method to log all registrations
      (this.notificationConnection as any).on = function (
        methodName: string,
        handler: any
      ) {
        console.log(
          `🔗 [SignalR DEBUG] Registering notification handler for: "${methodName}"`
        );
        return originalOn(methodName, handler);
      };

      // Add a catch-all handler for unregistered methods
      this.notificationConnection.onclose((error) => {
        if (error) {
          console.error(
            '🔥 [SignalR DEBUG] Notification connection closed with error:',
            error
          );
        }
      });
    }

    // Add window debug function
    (window as any).debugSignalR = () => {
      console.log('🔍 [SignalR DEBUG] Current connection states:');
      console.log('  Match Simulation:', this.matchSimulationConnection?.state);
      console.log('  Notifications:', this.notificationConnection?.state);
      console.log('  Connection IDs:', {
        match: (this.matchSimulationConnection as any)?.connectionId,
        notification: (this.notificationConnection as any)?.connectionId,
      });
    };

    console.log(
      '🎯 [SignalR DEBUG] Debug mode enabled. Use window.debugSignalR() for manual debugging'
    );
  }

  /**
   * Add universal method handlers to catch any unregistered SignalR calls
   */
  public addUniversalMethodHandlers(): void {
    console.log(
      '🌐 [SignalR] Adding universal method handlers for unknown method detection'
    );

    const commonMethods = [
      'sendmatcheventasync',
      'sendmatchstatisticsasync',
      'sendnotificationasync',
      'sendmessageasync',
      'sendmatchstartnotificationasync',
      'sendmatchendnotificationasync',
      'sendmatchupdatenotificationasync',
      'sendsimulationupdatenotificationasync',
      'simulationprogress',
      'simulationcomplete',
      'simulationerror',
      'matchevent',
    ];

    commonMethods.forEach((method) => {
      // Register both on match simulation connection
      if (this.matchSimulationConnection) {
        this.matchSimulationConnection.on(method, (...args: any[]) => {
          console.log(
            `🎯 [SignalR CATCH] Caught method "${method}" on match simulation connection:`,
            args
          );
        });
      }

      // Register both on notification connection
      if (this.notificationConnection) {
        this.notificationConnection.on(method, (...args: any[]) => {
          console.log(
            `🎯 [SignalR CATCH] Caught method "${method}" on notification connection:`,
            args
          );
        });
      }
    });

    console.log('✅ [SignalR] Universal method handlers registered');
  }
  /**
   * Add universal method handlers to a specific connection to catch any unregistered SignalR calls
   */
  private addUniversalMethodHandlersForConnection(
    connection: signalR.HubConnection,
    connectionType: string
  ): void {
    console.log(
      `🌐 [SignalR] Adding universal method handlers for ${connectionType} connection`
    );

    const commonMethods = [
      'sendmatcheventasync',
      'sendmatchstatisticsasync',
      'sendnotificationasync',
      'sendmessageasync',
      'sendmatchstartnotificationasync',
      'sendmatchendnotificationasync',
      'sendmatchupdatenotificationasync',
      'sendsimulationupdatenotificationasync',
      'simulationprogress',
      'simulationcomplete',
      'simulationerror',
      'matchevent',
    ];

    commonMethods.forEach((method) => {
      connection.on(method, (...args: any[]) => {
        console.log(
          `🎯 [SignalR CATCH] Caught method "${method}" on ${connectionType} connection:`,
          args
        );
      });
    });

    // Add a catch-all handler for the most common problematic method
    try {
      connection.on('sendmessageasync', (...args: any[]) => {
        console.log(
          `🎯 [SignalR CATCH] Specific handler for 'sendmessageasync' on ${connectionType} connection:`,
          args
        );
      });
    } catch (error) {
      console.warn(
        `Could not register sendmessageasync handler on ${connectionType}:`,
        error
      );
    }

    console.log(
      `✅ [SignalR] Universal method handlers registered for ${connectionType} connection`
    );
  }
}

// Create and export singleton instance
const signalRService = new SignalRService();
export default signalRService;
