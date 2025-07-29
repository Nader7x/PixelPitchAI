import axios, {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  AxiosError,
  InternalAxiosRequestConfig,
} from 'axios';

// Extend Axios types to include our custom properties
declare module 'axios' {
  interface InternalAxiosRequestConfig {
    metadata?: {
      startTime: number;
      requestId: number;
    };
    skipAuth?: boolean;
  }
}

// Enhanced TypeScript interfaces for better type safety
export interface ApiError {
  message: string;
  status?: number;
  code?: string;
  details?: any;
  isNetworkError?: boolean;
}

export interface ErrorResponseData {
  message?: string;
  error?: string;
  [key: string]: any;
}

export interface RequestConfig extends AxiosRequestConfig {
  skipAuth?: boolean;
  skipRetry?: boolean;
  cacheKey?: string;
  cacheTtl?: number;
}

export interface CacheEntry {
  data: any;
  timestamp: number;
  ttl: number;
}

export interface RateLimitInfo {
  count: number;
  resetTime: number;
}

class ApiService {
  private api: AxiosInstance;
  private readonly MAX_RETRIES = 3;
  private readonly RETRY_DELAY = 1000; // 1 second
  private readonly DEFAULT_TIMEOUT = 30000; // 30 seconds
  private readonly DEFAULT_CACHE_TTL = 5 * 60 * 1000; // 5 minutes

  // Performance and caching
  private cache = new Map<string, CacheEntry>();
  private pendingRequests = new Map<string, Promise<any>>();
  private requestCounter = 0;
  private rateLimitMap = new Map<string, RateLimitInfo>();
  private readonly RATE_LIMIT_WINDOW = 60000; // 1 minute
  private readonly RATE_LIMIT_MAX_REQUESTS = 100;

  constructor() {
    const baseURL =
      process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7082/api';

    this.api = axios.create({
      baseURL,
      timeout: this.DEFAULT_TIMEOUT,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
    this.startCacheCleanup();
  }

  private setupInterceptors(): void {
    // Enhanced request interceptor
    this.api.interceptors.request.use(
      (config) => {
        const requestId = ++this.requestCounter;
        config.metadata = {
          startTime: Date.now(),
          requestId,
          ...config.metadata,
        };

        // Skip authentication if explicitly requested
        if (!config.skipAuth) {
          // Support both localStorage and sessionStorage for token storage
          const token =
            localStorage.getItem('accessToken') ||
            sessionStorage.getItem('accessToken');
          if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
          }
        }

        // Add request ID for tracking
        config.headers['X-Request-ID'] = requestId.toString();

        console.log(
          `[API] Request ${requestId}: ${config.method?.toUpperCase()} ${config.url}`
        );
        return config;
      },
      (error) => {
        console.error('[API] Request interceptor error:', error);
        return Promise.reject(this.handleError(error));
      }
    );

    // Enhanced response interceptor with performance monitoring
    this.api.interceptors.response.use(
      (response) => {
        const requestDuration = response.config.metadata?.startTime
          ? Date.now() - response.config.metadata.startTime
          : 0;
        const requestId = response.config.metadata?.requestId || 'unknown';

        console.log(
          `[API] Response ${requestId}: ${response.status} (${requestDuration}ms)`
        );

        // Log slow requests for performance monitoring
        if (requestDuration > 3000) {
          console.warn(
            `[API] Slow request detected: ${requestDuration}ms for ${response.config.url}`
          );
        }

        return response;
      },
      async (error) => {
        const requestDuration = error.config?.metadata?.startTime
          ? Date.now() - error.config.metadata.startTime
          : 0;
        const requestId = error.config?.metadata?.requestId || 'unknown';

        console.error(
          `[API] Error ${requestId}: ${error.response?.status || 'Network Error'} (${requestDuration}ms)`
        );

        return Promise.reject(this.handleError(error));
      }
    );
  }

  /**
   * Enhanced error handling with proper error formatting
   */
  private handleError(error: AxiosError): ApiError {
    const apiError: ApiError = {
      message: 'An unexpected error occurred',
      status: error.response?.status,
    };

    if (error.response) {
      // Server responded with error status
      const { status, data } = error.response;

      switch (status) {
        case 400:
          apiError.message = 'Invalid request. Please check your input.';
          break;
        case 401:
          apiError.message = 'Authentication required. Please log in.';
          // Clear invalid tokens
          localStorage.removeItem('accessToken');
          sessionStorage.removeItem('accessToken');
          // Redirect to login if needed
          if (typeof window !== 'undefined') {
            window.location.href = '/login';
          }
          break;
        case 403:
          apiError.message =
            'Access denied. You do not have permission to perform this action.';
          break;
        case 404:
          apiError.message = 'The requested resource was not found.';
          break;
        case 409:
          apiError.message =
            'Conflict: The resource already exists or there is a data conflict.';
          break;
        case 422:
          apiError.message = 'Validation failed. Please check your input.';
          break;
        case 429:
          apiError.message = 'Too many requests. Please try again later.';
          break;
        case 500:
          apiError.message = 'Internal server error. Please try again later.';
          break;
        case 502:
        case 503:
        case 504:
          apiError.message =
            'Service temporarily unavailable. Please try again later.';
          break;
        default:
          apiError.message = `Server error: ${status}`;
      }

      // Extract additional error details from response
      if (typeof data === 'string') {
        apiError.details = data;
      } else if (data && typeof data === 'object') {
        apiError.details = data;
        const errorData = data as ErrorResponseData;
        // Override message if server provides one
        const serverMessage = errorData.message || errorData.error;
        if (serverMessage) {
          apiError.message = serverMessage;
        }
      }
    } else if (error.request) {
      // Network error or no response
      apiError.message =
        'Network error. Please check your internet connection.';
      apiError.code = 'NETWORK_ERROR';
    } else {
      // Request setup error
      apiError.message = error.message || 'Request configuration error';
      apiError.code = 'REQUEST_ERROR';
    }

    console.error('API Error:', apiError);
    return apiError;
  }

  // Generic GET method
  public async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.api.get(url, config);
    return response.data;
  }

  // Generic POST method
  public async post<T>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response: AxiosResponse<T> = await this.api.post(url, data, config);
    return response.data;
  }

  // Generic PUT method
  public async put<T>(
    url: string,
    data?: any,
    config?: AxiosRequestConfig
  ): Promise<T> {
    const response: AxiosResponse<T> = await this.api.put(url, data, config);
    return response.data;
  }

  // Generic DELETE method
  public async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<T> = await this.api.delete(url, config);
    return response.data;
  }

  // Method for handling form data (multipart/form-data)
  public async uploadForm<T>(
    url: string,
    formData: FormData,
    method: 'post' | 'put' = 'post',
    config?: AxiosRequestConfig
  ): Promise<T> {
    const uploadConfig: AxiosRequestConfig = {
      ...config,
      headers: {
        ...config?.headers,
        'Content-Type': 'multipart/form-data',
      },
    };

    let response: AxiosResponse<T>;
    if (method === 'put') {
      response = await this.api.put(url, formData, uploadConfig);
    } else {
      response = await this.api.post(url, formData, uploadConfig);
    }

    return response.data;
  }

  /**
   * Enhanced GET method with caching and request deduplication
   */
  public async getWithCache<T>(
    url: string,
    config?: RequestConfig
  ): Promise<T> {
    const cacheKey =
      config?.cacheKey || `GET:${url}:${JSON.stringify(config?.params || {})}`;
    const cacheTtl = config?.cacheTtl || this.DEFAULT_CACHE_TTL;

    // Check cache first
    const cachedData = this.getFromCache<T>(cacheKey);
    if (cachedData !== null) {
      console.log(`[API] Cache hit for: ${cacheKey}`);
      return cachedData;
    }

    // Check for pending request to avoid duplicate calls
    if (this.pendingRequests.has(cacheKey)) {
      console.log(`[API] Request deduplication for: ${cacheKey}`);
      return this.pendingRequests.get(cacheKey);
    }

    // Check rate limit
    if (!this.checkRateLimit(url)) {
      throw new Error('Rate limit exceeded. Please try again later.');
    }

    // Make the request
    const requestPromise = this.get<T>(url, config);
    this.pendingRequests.set(cacheKey, requestPromise);

    try {
      const result = await requestPromise;

      // Cache the result
      this.setCache(cacheKey, result, cacheTtl);

      return result;
    } finally {
      // Clean up pending request
      this.pendingRequests.delete(cacheKey);
    }
  }

  /**
   * POST method with retry logic
   */
  public async postWithRetry<T>(
    url: string,
    data?: any,
    config?: RequestConfig
  ): Promise<T> {
    return this.executeWithRetry(() => this.post<T>(url, data, config), config);
  }

  /**
   * PUT method with retry logic
   */
  public async putWithRetry<T>(
    url: string,
    data?: any,
    config?: RequestConfig
  ): Promise<T> {
    return this.executeWithRetry(() => this.put<T>(url, data, config), config);
  }

  /**
   * DELETE method with retry logic
   */
  public async deleteWithRetry<T>(
    url: string,
    config?: RequestConfig
  ): Promise<T> {
    return this.executeWithRetry(() => this.delete<T>(url, config), config);
  }

  /**
   * PUT method with retry logic for form data
   */
  public async putFormWithRetry<T>(
    url: string,
    data?: any,
    config?: RequestConfig
  ): Promise<T> {
    // Convert data to FormData
    const formData = new FormData();
    if (data) {
      Object.keys(data).forEach((key) => {
        const value = data[key];
        if (value !== null && value !== undefined) {
          // Handle file uploads and regular form fields
          if (value instanceof File || value instanceof Blob) {
            formData.append(key, value);
          } else {
            formData.append(key, String(value));
          }
        }
      });
    }

    return this.executeWithRetry(
      () => this.uploadForm<T>(url, formData, 'put', config),
      config
    );
  }

  /**
   * Execute request with retry logic
   */
  private async executeWithRetry<T>(
    requestFn: () => Promise<T>,
    config?: RequestConfig
  ): Promise<T> {
    if (config?.skipRetry) {
      return requestFn();
    }

    let lastError: any;
    for (let attempt = 1; attempt <= this.MAX_RETRIES; attempt++) {
      try {
        return await requestFn();
      } catch (error: any) {
        lastError = error;

        // Don't retry on client errors (4xx) except 429 (rate limit)
        if (
          error.status &&
          error.status >= 400 &&
          error.status < 500 &&
          error.status !== 429
        ) {
          throw error;
        }

        if (attempt < this.MAX_RETRIES) {
          const delay = this.RETRY_DELAY * Math.pow(2, attempt - 1); // Exponential backoff
          console.log(
            `[API] Retrying request in ${delay}ms (attempt ${attempt}/${this.MAX_RETRIES})`
          );
          await this.delay(delay);
        }
      }
    }

    throw lastError;
  }

  /**
   * Cache management methods
   */
  private getFromCache<T>(key: string): T | null {
    const entry = this.cache.get(key);
    if (!entry) return null;

    if (Date.now() > entry.timestamp + entry.ttl) {
      this.cache.delete(key);
      return null;
    }

    return entry.data as T;
  }

  private setCache(
    key: string,
    data: any,
    ttl: number = this.DEFAULT_CACHE_TTL
  ): void {
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl,
    });
  }

  /**
   * Update cache with fresh data
   */
  public updateCache(
    key: string,
    data: any,
    ttl: number = this.DEFAULT_CACHE_TTL
  ): void {
    this.setCache(key, data, ttl);
    console.log(`[API] Cache updated for key: ${key}`);
  }

  /**
   * Clear cache entries
   */
  public clearCache(pattern?: string): void {
    if (!pattern) {
      this.cache.clear();
      console.log('[API] All cache cleared');
      return;
    }

    const regex = new RegExp(pattern);
    for (const [key] of this.cache) {
      if (regex.test(key)) {
        this.cache.delete(key);
      }
    }
    console.log(`[API] Cache cleared for pattern: ${pattern}`);
  }

  /**
   * Rate limiting implementation
   */
  private checkRateLimit(endpoint: string): boolean {
    const now = Date.now();
    const rateLimitKey = this.extractRateLimitKey(endpoint);
    const rateLimitInfo = this.rateLimitMap.get(rateLimitKey);

    if (!rateLimitInfo) {
      this.rateLimitMap.set(rateLimitKey, {
        count: 1,
        resetTime: now + this.RATE_LIMIT_WINDOW,
      });
      return true;
    }

    // Reset counter if window has passed
    if (now >= rateLimitInfo.resetTime) {
      this.rateLimitMap.set(rateLimitKey, {
        count: 1,
        resetTime: now + this.RATE_LIMIT_WINDOW,
      });
      return true;
    }

    // Check if limit exceeded
    if (rateLimitInfo.count >= this.RATE_LIMIT_MAX_REQUESTS) {
      console.warn(`[API] Rate limit exceeded for ${rateLimitKey}`);
      return false;
    }

    // Increment counter
    rateLimitInfo.count++;
    return true;
  }

  private extractRateLimitKey(endpoint: string): string {
    // Extract base endpoint for rate limiting (remove dynamic parts)
    return endpoint.split('?')[0].replace(/\/\d+/g, '/:id');
  }

  /**
   * Start cache cleanup interval
   */
  private startCacheCleanup(): void {
    // Clean up expired cache entries every 5 minutes
    setInterval(
      () => {
        const now = Date.now();
        let cleanedCount = 0;

        for (const [key, entry] of this.cache.entries()) {
          if (now > entry.timestamp + entry.ttl) {
            this.cache.delete(key);
            cleanedCount++;
          }
        }

        if (cleanedCount > 0) {
          console.log(`[API] Cleaned up ${cleanedCount} expired cache entries`);
        }

        // Also clean up expired rate limit entries
        for (const [key, info] of this.rateLimitMap.entries()) {
          if (now >= info.resetTime) {
            this.rateLimitMap.delete(key);
          }
        }
      },
      5 * 60 * 1000
    ); // 5 minutes
  }

  /**
   * Utility method for delays
   */
  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  /**
   * Get cache statistics
   */
  public getCacheStats() {
    const now = Date.now();
    let validEntries = 0;
    let expiredEntries = 0;

    for (const [, entry] of this.cache.entries()) {
      if (now > entry.timestamp + entry.ttl) {
        expiredEntries++;
      } else {
        validEntries++;
      }
    }

    return {
      totalEntries: this.cache.size,
      validEntries,
      expiredEntries,
      pendingRequests: this.pendingRequests.size,
      rateLimitedEndpoints: this.rateLimitMap.size,
    };
  }

  /**
   * Health check method
   */
  public async healthCheck(): Promise<boolean> {
    try {
      await this.get('/health', { timeout: 5000 });
      return true;
    } catch (error) {
      console.error('[API] Health check failed:', error);
      return false;
    }
  }
}

// Create and export a singleton instance
const apiService = new ApiService();
export default apiService;
