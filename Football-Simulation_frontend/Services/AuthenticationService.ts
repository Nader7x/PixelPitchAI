import apiService from './ApiService';
import { jwtDecode } from 'jwt-decode';
import { RequestConfig } from './ApiService';

// User interfaces
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  Email: string;
  FirstName: string;
  LastName: string;
  Username: string;
  Password: string;
  confirmPassword: string;
  FavoriteTeamId?: number | null;
  Image?: File | null;
  Age: number | null;
  Gender: string;
  PhoneNumber?: string | null;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  password: string;
  confirmPassword: string;
}

export interface UpdateUserRequest {
  Id: string;
  username?: string;
  email?: string;
  currentPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
  favoriteTeamId?: number | null;
  age?: number;
  PhoneNumber?: number | null;
  Image?: File;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  username?: string;
  roles?: string[];
}

export interface RegisterResponse {
  succeeded: boolean;
  userId: string;
  error: string;
}

export interface DecodedToken {
  sub: string; // subject (user id)
  email: string;
  role?: string | string[];
  name?: string;
  exp: number; // expiration timestamp
  iat: number; // issued at timestamp
  iss?: string; // issuer
  aud?: string; // audience
  jti?: string; // JWT ID
  favoriteTeamId?: string | null; // custom claim
  claimNameId?: string; // original long-form claim name identifier
  claimEmail?: string; // original long-form claim email address
  claimName?: string; // original long-form claim name
  claimRole?: string | string[]; // original long-form claim role
  // Add the long-form claims as optional indexable properties
  [key: string]: any;
}

export interface UserProfile {
  userId: string;
  email: string;
  username: string;
  roles: string[];
  favoriteTeamName: string;
  emailConfirmed: boolean;
  imageUrl: string;
  age: number;
  gender: string;
  favoriteTeamId: number | null;
}
export interface StorageUser {
  userId: string;
  email: string;
  username: string;
  roles: string[];
  accessToken: string;
}

class AuthenticationService {
  private readonly TOKEN_KEY = 'accessToken';
  private readonly USER_KEY = 'user';

  // Cache for user roles to avoid repeated localStorage parsing
  private userRolesCache: string[] | null = null;
  private cacheTimestamp: number = 0;
  private readonly CACHE_DURATION = 5 * 60 * 1000; // 5 minutes in milliseconds

  /**
   * Login user and store token with retry logic
   */
  public async login(credentials: LoginRequest): Promise<AuthResponse> {
    try {
      const response = await apiService.postWithRetry<AuthResponse>(
        '/auth/login',
        credentials
      );
      this.storeToken(response.accessToken);
      this.storeUser(response);

      // Clear any cached data after successful login
      apiService.clearCache('user');

      return response;
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  }

  /**
   * Register a new user with retry logic and optimized form handling
   */
  public async register(userData: RegisterRequest): Promise<RegisterResponse> {
    try {
      // Create a FormData object to handle file uploads
      const formData = new FormData();
      formData.append('FirstName', userData.FirstName);
      formData.append('LastName', userData.LastName);
      formData.append('Username', userData.Username);
      formData.append('Gender', userData.Gender);
      formData.append('email', userData.Email);
      formData.append('password', userData.Password);
      formData.append('confirmPassword', userData.confirmPassword);
      formData.append(
        'favoriteTeamId',
        userData.FavoriteTeamId?.toString() || ''
      );
      if (userData.Image) {
        formData.append('Image', userData.Image);
      }
      formData.append('Age', userData.Age?.toString() || '');
      formData.append('PhoneNumber', userData.PhoneNumber || '');

      return await apiService.uploadForm<RegisterResponse>(
        '/auth/register',
        formData,
        'post'
      );
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    }
  }

  /**
   * Logout the user by removing stored tokens and data with enhanced cleanup
   */
  public logout(): void {
    // Call the API to revoke the token if the user is authenticated
    if (this.isAuthenticated()) {
      try {
        // Use fire-and-forget approach for token revocation
        apiService.postWithRetry('/auth/revoke-token', {}).catch((error) => {
          console.error('Token revocation failed:', error);
        });
      } catch (error) {
        console.error('Token revocation failed:', error);
      }
    }

    // Clear all storage
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    sessionStorage.removeItem(this.TOKEN_KEY);
    sessionStorage.removeItem(this.USER_KEY);

    // Clear roles cache
    this.clearRolesCache();

    // Clear all API cache related to user data
    apiService.clearCache('user|auth|profile');

    // Notify any listeners that the user has logged out
    this.notifyLogout();
  }

  // Logout event listeners
  private logoutListeners: (() => void)[] = [];

  /**
   * Add a listener for logout events
   */
  public onLogout(callback: () => void): void {
    this.logoutListeners.push(callback);
  }

  /**
   * Remove a logout event listener
   */
  public removeLogoutListener(callback: () => void): void {
    const index = this.logoutListeners.indexOf(callback);
    if (index > -1) {
      this.logoutListeners.splice(index, 1);
    }
  }

  /**
   * Notify all logout listeners
   */
  private notifyLogout(): void {
    this.logoutListeners.forEach((callback) => {
      try {
        callback();
      } catch (error) {
        console.error('Error in logout listener:', error);
      }
    });
  }

  /**
   * Check if the user is logged in
   */
  public isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;

    // Check if token is expired
    try {
      const decoded = this.decodeToken(token);
      const currentTime = Date.now() / 1000;
      return decoded.exp > currentTime;
    } catch (error) {
      this.clearTokens();
      return false;
    }
  }

  /**
   * Get the current user from stored token
   */
  public getCurrentUser(): DecodedToken | null {
    try {
      const token = this.getToken();
      if (!token) return null;

      const decoded = this.decodeToken(token);

      // Extract user ID from various possible claim locations
      const nameIdentifierClaim =
        decoded[
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
        ];
      const emailClaim =
        decoded[
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
        ];
      const nameClaim =
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
      const roleClaim =
        decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      // Create a standardized user object with properly mapped claims
      return {
        // Core standard claims
        sub: decoded.sub || nameIdentifierClaim || '',
        email: decoded.email || emailClaim || '',
        name: decoded.name || nameClaim,
        role: decoded.role || roleClaim,
        exp: decoded.exp,
        iat: decoded.iat || Math.floor(Date.now() / 1000),

        // Additional custom claims
        favoriteTeamId: decoded.favoriteTeamId,

        // Include original JWT standard claims that we haven't explicitly mapped
        iss: decoded.iss,
        aud: decoded.aud,
        jti: decoded.jti,

        // Add direct accessors for the original long-form claim names
        claimNameId: nameIdentifierClaim,
        claimEmail: emailClaim,
        claimName: nameClaim,
        claimRole: roleClaim,
      };
    } catch (error) {
      console.error('Error getting current user:', error);
      return null;
    }
  }

  /**
   * Get the current user ID from the stored token
   */
  public getCurrentUserId(): string | null {
    try {
      const currentUser = this.getCurrentUser();
      if (!currentUser) {
        console.error('No current user found');
        return null;
      }

      // Try multiple possible fields for user ID
      const userId =
        currentUser.claimNameId ||
        currentUser.sub ||
        currentUser[
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
        ] ||
        null;

      if (!userId) {
        console.error(
          'User ID not found in token. Available fields:',
          Object.keys(currentUser)
        );
      }

      return userId;
    } catch (error) {
      console.error('Error getting current user ID:', error);
      return null;
    }
  }

  /**
   * Get user profile from the API with caching
   */
  public async getUserProfile(id: string): Promise<UserProfile> {
    try {
      return await apiService.getWithCache<UserProfile>(`/auth/profile/${id}`, {
        cacheKey: `user-profile-${id}`,
        cacheTtl: 10 * 60 * 1000, // Cache for 10 minutes
      });
    } catch (error) {
      console.error('Error fetching user profile:', error);
      throw error;
    }
  }

  /**
   * Update user profile with retry logic and cache invalidation
   */
  public async updateProfile(
    updateData: UpdateUserRequest
  ): Promise<UserProfile> {
    try {
      const result = await apiService.putFormWithRetry<UserProfile>(
        'auth/update',
        updateData
      );

      // Update user profile cache after successful update
      apiService.updateCache(
        `user-profile-${updateData.Id}`,
        result,
        10 * 60 * 1000
      ); // Cache for 10 minutes

      // Update stored user data if username or email changed
      if (updateData.username || updateData.email) {
        const currentUser = JSON.parse(
          localStorage.getItem(this.USER_KEY) || '{}'
        );
        if (currentUser.userId === updateData.Id) {
          currentUser.username = updateData.username || currentUser.username;
          currentUser.email = updateData.email || currentUser.email;
          localStorage.setItem(this.USER_KEY, JSON.stringify(currentUser));
          this.clearRolesCache(); // Clear cache since user data changed
        }
      }
      console.log('✅ Profile updated successfully and cache refreshed');
      return result;
    } catch (error) {
      console.error('Profile update failed:', error);
      throw error;
    }
  }

  /**
   * Get user roles with caching for better performance
   * @returns Array of user roles or null if not available
   */
  private getUserRoles(): string[] | null {
    // Check if cache is valid
    const now = Date.now();
    if (
      this.userRolesCache &&
      now - this.cacheTimestamp < this.CACHE_DURATION
    ) {
      return this.userRolesCache;
    }

    try {
      const userDataStr = localStorage.getItem(this.USER_KEY);
      if (!userDataStr || userDataStr === '{}') {
        this.clearRolesCache();
        return null;
      }

      const user: StorageUser = JSON.parse(userDataStr);

      if (!user || typeof user !== 'object' || !user.roles) {
        this.clearRolesCache();
        return null;
      }

      // Normalize roles to array and cache
      const roles = Array.isArray(user.roles) ? user.roles : [user.roles];
      this.userRolesCache = roles;
      this.cacheTimestamp = now;

      return roles;
    } catch (error) {
      console.error('Error getting user roles:', error);
      this.clearRolesCache();
      return null;
    }
  }

  /**
   * Clear the roles cache
   */
  private clearRolesCache(): void {
    this.userRolesCache = null;
    this.cacheTimestamp = 0;
  }

  /**
   * Check if the current user has any of the specified roles (optimized with caching)
   * @param role - Single role string or array of roles to check
   * @returns true if user has at least one of the required roles, false otherwise
   */
  public hasRoleOptimized(role: string | string[]): boolean {
    // Early return if not authenticated
    if (!this.isAuthenticated()) {
      return false;
    }

    const userRoles = this.getUserRoles();
    if (!userRoles || userRoles.length === 0) {
      return false;
    }

    // Normalize required roles to array
    const requiredRoles: string[] = Array.isArray(role) ? role : [role];

    if (requiredRoles.length === 0) {
      return false;
    }

    // Check if user has at least one of the required roles (case-insensitive)
    return requiredRoles.some((requiredRole) =>
      userRoles.some(
        (userRole) => userRole.toLowerCase() === requiredRole.toLowerCase()
      )
    );
  }

  /**
   * Check if the current user has the required role(s)
   * @param role - Single role string or array of roles to check
   * @returns true if user has at least one of the required roles, false otherwise
   */
  public hasRole(role: string | string[]): boolean {
    // Early return if not authenticated
    if (!this.isAuthenticated()) {
      return false;
    }

    try {
      // Get user data from storage
      const userDataStr = localStorage.getItem(this.USER_KEY);
      if (!userDataStr || userDataStr === '{}') {
        return false;
      }

      const user: StorageUser = JSON.parse(userDataStr);

      // Validate user object structure
      if (!user || typeof user !== 'object' || !user.roles) {
        return false;
      }

      // Ensure user roles is an array
      const userRoles: string[] = Array.isArray(user.roles)
        ? user.roles
        : [user.roles];

      // Handle empty roles array
      if (userRoles.length === 0) {
        return false;
      }

      // Normalize required roles to array
      const requiredRoles: string[] = Array.isArray(role) ? role : [role];

      // Handle empty required roles
      if (requiredRoles.length === 0) {
        return false;
      }

      // Check if user has at least one of the required roles (case-insensitive)
      return requiredRoles.some((requiredRole) =>
        userRoles.some(
          (userRole) => userRole.toLowerCase() === requiredRole.toLowerCase()
        )
      );
    } catch (error) {
      console.error('Error checking user role:', error);
      // Clear potentially corrupted user data
      this.clearTokens();
      return false;
    }
  }

  /**
   * Check if the current user has all of the specified roles
   * @param roles - Array of roles that the user must have ALL of
   * @returns true if user has all the required roles, false otherwise
   */
  public hasAllRoles(roles: string[]): boolean {
    if (!this.isAuthenticated() || roles.length === 0) {
      return false;
    }

    const userRoles = this.getUserRoles();
    if (!userRoles || userRoles.length === 0) {
      return false;
    }

    // Check if user has ALL required roles (case-insensitive)
    return roles.every((requiredRole) =>
      userRoles.some(
        (userRole) => userRole.toLowerCase() === requiredRole.toLowerCase()
      )
    );
  }

  /**
   * Refresh the user's token with retry logic
   */
  public async refreshToken(): Promise<boolean> {
    try {
      const response = await apiService.postWithRetry<AuthResponse>(
        '/auth/refresh-token',
        {}
      );
      this.storeToken(response.accessToken);
      this.storeUser(response);

      // Clear user-related cache after token refresh
      apiService.clearCache('user|auth');

      return true;
    } catch (error) {
      console.error('Token refresh failed:', error);
      this.clearTokens();
      return false;
    }
  }

  /**
   * Manual refresh token with retry logic (for Swagger testing)
   */
  public async manualRefreshToken(token: string): Promise<AuthResponse> {
    try {
      const response = await apiService.postWithRetry<AuthResponse>(
        '/auth/manual-refresh',
        token
      );
      this.storeToken(response.accessToken);
      this.storeUser(response);

      // Clear user-related cache after manual token refresh
      apiService.clearCache('user|auth');

      return response;
    } catch (error) {
      console.error('Manual token refresh failed:', error);
      throw error;
    }
  }

  /**
   * Check if token needs refreshing (e.g., if it's about to expire)
   */
  public async checkAndRefreshToken(): Promise<boolean> {
    const token = this.getToken();
    if (!token) return false;

    try {
      const decoded = this.decodeToken(token);
      const currentTime = Date.now() / 1000;

      // If token will expire in less than 5 minutes (300 seconds), refresh it
      if (decoded.exp - currentTime < 300) {
        return await this.refreshToken();
      }

      return true;
    } catch (error) {
      console.error('Token validation failed:', error);
      this.clearTokens();
      return false;
    }
  }

  /**
   * Request password reset with retry logic
   */
  public async forgotPassword(email: string): Promise<boolean> {
    try {
      await apiService.postWithRetry('/auth/forgot-password', { email });
      return true;
    } catch (error) {
      console.error('Password reset request failed:', error);
      throw error;
    }
  }

  /**
   * Reset password with token and retry logic
   */
  public async resetPassword(
    resetData: ResetPasswordRequest
  ): Promise<boolean> {
    try {
      const { email, token, password, confirmPassword } = resetData;
      await apiService.postWithRetry(
        '/auth/reset-password',
        { password, confirmPassword },
        {
          params: { email, token },
        }
      );
      return true;
    } catch (error) {
      console.error('Password reset failed:', error);
      throw error;
    }
  }

  /**
   * Confirm email with retry logic
   */
  public async confirmEmail(userId: string, token: string): Promise<boolean> {
    try {
      await apiService.postWithRetry(
        '/auth/confirm-email',
        {},
        {
          params: { userId, token },
        }
      );

      // Clear user profile cache since email confirmation status changed
      apiService.clearCache(`user-profile-${userId}`);

      return true;
    } catch (error) {
      console.error('Email confirmation failed:', error);
      throw error;
    }
  }

  /**
   * Resend email confirmation with retry logic
   */
  public async resendEmailConfirmation(email: string): Promise<boolean> {
    try {
      await apiService.postWithRetry('/auth/resend-email-confirmation', {
        email,
      });
      return true;
    } catch (error) {
      console.error('Resend email confirmation failed:', error);
      throw error;
    }
  }

  /**
   * Decode a JWT token
   */
  private decodeToken(token: string): DecodedToken {
    return jwtDecode<DecodedToken>(token);
  }

  /**
   * Store token in localStorage
   */
  private storeToken(token: string, remember: boolean = true): void {
    if (remember) {
      localStorage.setItem(this.TOKEN_KEY, token);
    } else {
      sessionStorage.setItem(this.TOKEN_KEY, token);
    }
  }

  /**
   * Store user info in localStorage
   */
  private storeUser(userData: AuthResponse): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(userData));
    // Clear cache when new user data is stored
    this.clearRolesCache();
  }

  /**
   * Get token from storage
   */
  private getToken(): string | null {
    return (
      localStorage.getItem(this.TOKEN_KEY) ||
      sessionStorage.getItem(this.TOKEN_KEY)
    );
  }

  /**
   * Clear all stored tokens with enhanced cleanup
   */
  private clearTokens(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    sessionStorage.removeItem(this.TOKEN_KEY);
    sessionStorage.removeItem(this.USER_KEY);

    // Clear roles cache when tokens are cleared
    this.clearRolesCache();

    // Clear all auth-related API cache
    apiService.clearCache('user|auth|profile');
  }

  /**
   * Check API service health
   */
  public async checkApiHealth(): Promise<boolean> {
    try {
      return await apiService.healthCheck();
    } catch (error) {
      console.error('API health check failed:', error);
      return false;
    }
  }

  /**
   * Get API service cache statistics for debugging
   */
  public getApiCacheStats() {
    return apiService.getCacheStats();
  }

  /**
   * Get the current access token (public method for external services)
   * @returns The access token if available and valid, null otherwise
   */
  public getAccessToken(): string | null {
    const token = this.getToken();
    if (!token) return null;

    // Validate token expiration
    try {
      const decoded = this.decodeToken(token);
      const currentTime = Date.now() / 1000;

      // Return null if token is expired
      if (decoded.exp <= currentTime) {
        this.clearTokens();
        return null;
      }

      return token;
    } catch (error) {
      console.error('Token validation failed:', error);
      this.clearTokens();
      return null;
    }
  }

  /**
   * Get access token with automatic refresh if needed
   * @returns Promise that resolves to the access token or null if refresh fails
   */
  public async getValidAccessToken(): Promise<string | null> {
    const token = this.getToken();
    if (!token) return null;

    try {
      const decoded = this.decodeToken(token);
      const currentTime = Date.now() / 1000;

      // If token is expired, clear it and return null
      if (decoded.exp <= currentTime) {
        this.clearTokens();
        return null;
      }

      // If token will expire in less than 5 minutes, try to refresh it
      if (decoded.exp - currentTime < 300) {
        const refreshSuccess = await this.refreshToken();
        if (refreshSuccess) {
          return this.getToken();
        } else {
          return null;
        }
      }

      return token;
    } catch (error) {
      console.error('Token validation failed:', error);
      this.clearTokens();
      return null;
    }
  }

  /**
   * Clear authentication-related cache manually
   */
  public clearAuthCache(): void {
    apiService.clearCache('user|auth|profile');
    this.clearRolesCache();
  }

  /**
   * Validate current session with server check
   */
  public async validateSession(): Promise<boolean> {
    if (!this.isAuthenticated()) {
      return false;
    }

    try {
      const currentUser = this.getCurrentUser();
      if (!currentUser) {
        return false;
      }

      // Check if we can fetch user profile (validates token with server)
      await this.getUserProfile(currentUser.sub);
      return true;
    } catch (error) {
      console.error('Session validation failed:', error);
      this.clearTokens();
      return false;
    }
  }
}

// Create and export a singleton instance
const authService = new AuthenticationService();
export default authService;
