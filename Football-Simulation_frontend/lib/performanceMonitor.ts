// Performance Monitor for API Requests and SignalR Events
// This helps identify and optimize slow operations that could affect event timing

class PerformanceMonitor {
  private static instance: PerformanceMonitor;
  private apiCallTimes: Map<string, number> = new Map();
  private slowRequestThreshold = 5000; // 5 seconds

  static getInstance(): PerformanceMonitor {
    if (!PerformanceMonitor.instance) {
      PerformanceMonitor.instance = new PerformanceMonitor();
    }
    return PerformanceMonitor.instance;
  }

  // Monitor API request performance
  startApiCall(url: string, requestId: string = Date.now().toString()): string {
    this.apiCallTimes.set(requestId, performance.now());
    console.log(`🚀 [PERF] API Call Started: ${url} [ID: ${requestId}]`);
    return requestId;
  }

  endApiCall(requestId: string, url: string, success: boolean = true): void {
    const startTime = this.apiCallTimes.get(requestId);
    if (!startTime) return;

    const duration = performance.now() - startTime;
    this.apiCallTimes.delete(requestId);

    const status = success ? '✅' : '❌';
    const speed = duration > this.slowRequestThreshold ? '🐌 SLOW' : '⚡ FAST';

    console.log(`${status} [PERF] API Call Complete: ${url}`);
    console.log(`⏱️ [PERF] Duration: ${duration.toFixed(2)}ms ${speed}`);

    if (duration > this.slowRequestThreshold) {
      console.warn(
        `🚨 [PERF] SLOW REQUEST DETECTED: ${url} took ${duration.toFixed(2)}ms`
      );
      console.warn(`🔧 [PERF] This could affect SignalR event timing!`);

      // Suggest optimizations
      this.suggestOptimizations(url, duration);
    }
  }

  // Monitor SignalR event timing
  recordSignalREvent(eventType: string, processingTime: number): void {
    const speed =
      processingTime > 100 ? '🐌' : processingTime > 50 ? '⚠️' : '⚡';
    console.log(
      `${speed} [PERF] SignalR Event: ${eventType} processed in ${processingTime.toFixed(2)}ms`
    );

    if (processingTime > 100) {
      console.warn(
        `🚨 [PERF] SLOW EVENT PROCESSING: ${eventType} took ${processingTime.toFixed(2)}ms`
      );
    }
  }

  // Monitor overall page performance
  recordPageLoad(): void {
    if (typeof window !== 'undefined' && window.performance) {
      const navigation = window.performance.getEntriesByType(
        'navigation'
      )[0] as PerformanceNavigationTiming;
      const loadTime = navigation.loadEventEnd - navigation.fetchStart;

      console.log(`📊 [PERF] Page Load Time: ${loadTime.toFixed(2)}ms`);

      if (loadTime > 3000) {
        console.warn(`🚨 [PERF] SLOW PAGE LOAD: ${loadTime.toFixed(2)}ms`);
        console.warn(`🔧 [PERF] This could delay SignalR connection!`);
      }
    }
  }

  private suggestOptimizations(url: string, duration: number): void {
    console.group(`🔧 [PERF] Optimization Suggestions for: ${url}`);

    if (url.includes('simulatematch')) {
      console.log(`💡 Match Simulation taking ${duration.toFixed(2)}ms:`);
      console.log(`   • Consider using background processing`);
      console.log(`   • Implement progress updates via SignalR`);
      console.log(`   • Use streaming responses for large datasets`);
      console.log(`   • Cache frequently accessed match data`);
    }

    if (url.includes('matches')) {
      console.log(`💡 Match API taking ${duration.toFixed(2)}ms:`);
      console.log(`   • Implement pagination for large match lists`);
      console.log(`   • Use field selection to limit response size`);
      console.log(`   • Consider Redis caching for match data`);
    }

    console.log(`🚀 General optimizations:`);
    console.log(`   • Enable gzip compression on API responses`);
    console.log(`   • Use CDN for static assets`);
    console.log(`   • Implement request debouncing`);
    console.log(`   • Consider GraphQL for efficient data fetching`);

    console.groupEnd();
  }

  // Get performance statistics
  getStats(): any {
    return {
      activeRequests: this.apiCallTimes.size,
      slowRequestThreshold: this.slowRequestThreshold,
      performanceSupported:
        typeof window !== 'undefined' && !!window.performance,
    };
  }
}

// Enhanced API Service wrapper with performance monitoring
export class OptimizedApiService {
  private perfMonitor = PerformanceMonitor.getInstance();
  private baseURL =
    process.env.NEXT_PUBLIC_API_BASE_URL || 'https://100.90.131.37:7082';

  async request<T>(
    endpoint: string,
    options: RequestInit = {},
    timeout: number = 15000 // 15 second timeout
  ): Promise<T> {
    const url = `${this.baseURL}${endpoint}`;
    const requestId = this.perfMonitor.startApiCall(url);

    try {
      // Create timeout promise
      const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(
          () => reject(new Error(`Request timeout after ${timeout}ms`)),
          timeout
        );
      });

      // Create fetch promise with performance monitoring
      const fetchPromise = fetch(url, {
        ...options,
        headers: {
          'Content-Type': 'application/json',
          ...options.headers,
        },
      });

      // Race between fetch and timeout
      const response = await Promise.race([fetchPromise, timeoutPromise]);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const data = await response.json();
      this.perfMonitor.endApiCall(requestId, url, true);

      return data;
    } catch (error) {
      this.perfMonitor.endApiCall(requestId, url, false);
      console.error(`🚨 [API] Request failed:`, error);
      throw error;
    }
  }

  // Optimized match simulation with progress tracking
  async simulateMatchOptimized(matchId: string): Promise<any> {
    console.log(`🎮 [API] Starting optimized match simulation: ${matchId}`);

    try {
      // Use shorter timeout for simulation requests
      const result = await this.request(
        `/api/matches/simulatematch/${matchId}`,
        { method: 'POST' },
        30000 // 30 second timeout for simulations
      );

      console.log(`✅ [API] Match simulation completed successfully`);
      return result;
    } catch (error) {
      console.error(`❌ [API] Match simulation failed:`, error);
      throw error;
    }
  }
}

// Global performance monitor instance
export const perfMonitor = PerformanceMonitor.getInstance();
export const optimizedApiService = new OptimizedApiService();

// Initialize performance monitoring
if (typeof window !== 'undefined') {
  window.addEventListener('load', () => {
    perfMonitor.recordPageLoad();
  });

  // Export to global scope for debugging
  (window as any).perfMonitor = perfMonitor;
  (window as any).optimizedApiService = optimizedApiService;
}
