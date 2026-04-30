// Shape of the JWT payload after decoding the token
export interface JwtPayload {
  sub?: string;
  nameid?: string;
  userId?: string;
  username?: string;
  email: string;
  role: string;
  exp: number;
  iat?: number;
}

// Response from POST /api/auth/login and POST /api/auth/signup
export interface AuthResponse {
  token: string;
  refreshToken: string;
  userName: string;
  email: string;
  role: string;
}

// Response from GET /api/users/profile
export interface UserProfile {
  id: string;
  userName: string;
  email: string;
  phoneNumber: string;
  role: string;
  isVerified: boolean;
  isActive: boolean;
}

export interface UpdateProfileRequest {
  userName: string;
  email: string;
  phoneNumber: string;
}

// Request body for POST /api/auth/login
export interface LoginRequest {
  email: string;
  password: string;
}

// Request body for POST /api/auth/signup
export interface SignupRequest {
  userName: string;
  email: string;
  password: string;
  phoneNumber: string;
}

export interface SendOtpRequest {
  email: string;
}

export interface VerifyOtpRequest {
  email: string;
  code: string;
}

export interface PasswordResetSendOtpRequest {
  email: string;
}

export interface PasswordResetVerifyOtpRequest {
  email: string;
  code: string;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
}
