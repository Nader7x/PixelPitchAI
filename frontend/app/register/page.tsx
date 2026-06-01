'use client';

import Link from 'next/link';
import React, { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import authService, {
  RegisterRequest,
} from '../../Services/AuthenticationService';
import teamService, { Team } from '../../Services/TeamService';
import PhoneInput from 'react-phone-input-2';
import 'react-phone-input-2/lib/style.css';

export default function Register() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    userName: '',
    email: '',
    password: '',
    confirmPassword: '',
    gender: '',
    age: 0,
    image: null as File | null,
    phoneNumber: '',
    FavoriteTeamId: null as number | null, // Ensure FavoriteTeamId can be null or number
  });
  const [errors, setErrors] = useState<{ [key: string]: string }>({});
  const [isLoading, setIsLoading] = useState(false);
  const [serverError, setServerError] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [teams, setTeams] = useState<Team[]>([]);

  useEffect(() => {
    fetchTeams().then();
  }, []);

  const fetchTeams = async () => {
    try {
      const response = await teamService.getAllTeams();
      if (Array.isArray(response)) {
        setTeams(response);
      } else {
        console.error('Unexpected API response format for teams:', response);
        setTeams([]);
      }
    } catch (error) {
      console.error('Error fetching teams:', error);
      setTeams([]);
    }
  };

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { id, value } = e.target;
    // For FavoriteTeamId, parse the value to a number if it's not an empty string
    const processedValue =
      id === 'FavoriteTeamId' ? (value === '' ? null : Number(value)) : value;
    setFormData((prev) => ({ ...prev, [id]: processedValue }));

    if (errors[id]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[id];
        return newErrors;
      });
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFormData((prev) => ({ ...prev, image: e.target.files?.[0] || null }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: { [key: string]: string } = {};

    if (!formData.firstName.trim()) {
      newErrors.firstName = 'FirstName is required';
    } else if (formData.firstName.length < 3) {
      newErrors.firstName = 'FirstName must be at least 3 characters';
    }
    if (!formData.lastName.trim()) {
      newErrors.lastName = 'LastName is required';
    } else if (formData.lastName.length < 2) {
      // Corrected to check lastName length
      newErrors.lastName = 'LastName must be at least 2 characters';
    }
    if (!formData.userName.trim()) {
      newErrors.firstName = 'userName is required';
    } else if (formData.userName.length < 4) {
      newErrors.firstName = 'FirstName must be at least 4 characters';
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!emailRegex.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }

    if (!formData.password) {
      newErrors.password = 'Password is required';
    } else if (formData.password.length < 6) {
      newErrors.password = 'Password must be at least 6 characters';
    }

    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match';
    }

    if (
      formData.age &&
      (Number(formData.age) < 13 || Number(formData.age) > 120)
    ) {
      newErrors.age = 'Please enter a valid age between 13 and 120';
    }

    const phoneNumberRegex =
      /^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$/;
    // Only validate if a phone number is entered, as it's optional
    if (formData.phoneNumber && !phoneNumberRegex.test(formData.phoneNumber)) {
      newErrors.phoneNumber = 'Please enter a valid phone number';
    }
    // No specific validation for FavoriteTeamId here, but you could add one if needed

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  const toggleConfirmPasswordVisibility = () => {
    setShowConfirmPassword(!showConfirmPassword);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setServerError('');

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    try {
      const registerData: RegisterRequest = {
        FirstName: formData.firstName,
        LastName: formData.lastName,
        Username: formData.userName,
        Email: formData.email,
        Password: formData.password,
        confirmPassword: formData.confirmPassword,
        Image: formData.image,
        Gender: formData.gender,
        Age: formData.age > 0 ? formData.age : null, // Send age only if provided
        PhoneNumber: formData.phoneNumber || undefined, // Send phone only if provided
        FavoriteTeamId: formData.FavoriteTeamId, // This will be null or a number
      };

      const response = await authService.register(registerData);
      if (response.succeeded) {
        router.push('/login?registered=true');
      } else {
        setServerError(
          response.error || 'Registration failed. Please try again.'
        );
      }
    } catch (error: any) {
      console.error('Registration error:', error);
      console.log('THIS');
      console.log(error?.message);
      setServerError(
        error?.message || 'Registration failed. Please try again.'
      );
    } finally {
      setIsLoading(false);
    }
  };
  const formatPasswordErrors = (errorMessage: string): string[] => {
    // Check if the error message contains password validation errors
    if (!errorMessage || typeof errorMessage !== 'string') {
      return ['An error occurred. Please try again.'];
    }

    const errorMap: { [key: string]: string } = {
      PasswordTooShort: 'Password must be at least 6 characters long',
      PasswordRequiresNonAlphanumeric:
        'Password must contain at least one special character (!@#$%^&*)',
      PasswordRequiresLower:
        'Password must contain at least one lowercase letter (a-z)',
      PasswordRequiresUpper:
        'Password must contain at least one uppercase letter (A-Z)',
      PasswordRequiresDigit: 'Password must contain at least one number (0-9)',
      DuplicateUserName:
        'This username is already taken. Please choose a different one.',
      DuplicateEmail:
        'This email is already registered. Please use a different email or try logging in.',
      InvalidEmail: 'Please enter a valid email address',
      InvalidUserName:
        'Username contains invalid characters. Use only letters, numbers, and underscores.',
      PasswordMismatch: 'Password and confirm password do not match',
    };

    // Handle comma-separated errors (from server validation)
    if (errorMessage.includes(',')) {
      const errors = errorMessage.split(',').map((error) => error.trim());
      return errors.map((error) => errorMap[error] || error).filter(Boolean);
    }

    // Handle single error or unknown format
    return [errorMap[errorMessage.trim()] || errorMessage];
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center py-6">
      {/* Dynamic football-themed background */}
      <div className="absolute inset-0 -z-10 h-full w-full overflow-hidden bg-[url('/images/football-pattern.png')] bg-cover bg-center opacity-10">
        <div className="absolute inset-0 bg-gradient-to-br from-green-800/90 via-blue-900/80 to-indigo-900/90"></div>
      </div>

      <div className="mx-auto flex w-full max-w-sm overflow-hidden rounded-xl border-t border-green-400/30 bg-white/95 shadow-2xl lg:max-w-4xl dark:bg-gray-800/95">
        {/* Left Side Image with enhanced styling */}
        <div className="relative hidden bg-cover lg:block lg:w-1/2">
          <div className="absolute inset-0 bg-[url('/images/Messi%20shooting.png')] bg-cover bg-center"></div>
          <div className="absolute inset-0 flex items-center justify-center bg-gradient-to-r from-green-800/80 via-emerald-800/70 to-blue-900/80">
            <div className="p-8 text-center text-white">
              <h2 className="text-shadow mb-4 text-3xl font-bold">
                Join the Football Community
              </h2>
              <p className="text-lg">Get access to exclusive features:</p>
              <ul className="mt-4 list-none space-y-2 text-left">
                <li className="flex items-center">
                  <svg
                    className="mr-2 h-5 w-5 text-green-400"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                      clipRule="evenodd"
                    ></path>
                  </svg>
                  Personalized match updates
                </li>
                <li className="flex items-center">
                  <svg
                    className="mr-2 h-5 w-5 text-green-400"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                      clipRule="evenodd"
                    ></path>
                  </svg>
                  Track your favorite teams
                </li>
                <li className="flex items-center">
                  <svg
                    className="mr-2 h-5 w-5 text-green-400"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                      clipRule="evenodd"
                    ></path>
                  </svg>
                  Join fan discussions
                </li>
              </ul>
              <div className="mt-8 flex items-center justify-center space-x-4">
                <img
                  src="/logos/barcelona.png"
                  alt="Team Logo"
                  className="h-14 w-auto animate-pulse"
                />
                <img
                  src="/logos/real madrid.png"
                  alt="Team Logo"
                  className="h-14 w-auto animate-pulse"
                  style={{ animationDelay: '0.5s' }}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Right Side Form with enhanced styling */}
        <div
          className="w-full overflow-y-auto px-6 py-6 md:px-8 lg:w-1/2"
          style={{ maxHeight: '90vh' }}
        >
          <div className="mx-auto flex justify-center">
            <div className="flex items-center text-center">
              <img
                src="/logos/PixelPitch.png"
                alt="PixelPitch Logo"
                className="mr-2 h-30 w-30 object-contain"
              />
              <div>
                <h1 className="text-2xl font-bold text-green-600 dark:text-green-500">
                  PIXELPITCHAI
                </h1>
                <div className="mx-auto mt-1 h-1 w-16 rounded-full bg-gradient-to-r from-blue-600 to-green-500"></div>
              </div>
            </div>
          </div>
          <h2 className="mt-6 text-center text-2xl font-bold text-gray-800 dark:text-gray-200">
            Create Your Account
          </h2>
          <p className="mt-2 text-center text-gray-600 dark:text-gray-400">
            Be part of the action. Join today!
          </p>{' '}
          {serverError && (
            <div className="mt-4 rounded-lg border border-red-400 bg-red-100 p-3 text-sm text-red-700">
              <div className="flex items-start">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  className="mt-0.5 mr-2 h-5 w-5 flex-shrink-0"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                >
                  <path
                    fillRule="evenodd"
                    d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                    clipRule="evenodd"
                  />
                </svg>
                <div className="flex-1">
                  {formatPasswordErrors(serverError).length > 1 ? (
                    <div>
                      <div className="mb-1 font-medium">
                        Please fix the following issues:
                      </div>
                      <ul className="list-inside list-disc space-y-1">
                        {formatPasswordErrors(serverError).map(
                          (error, index) => (
                            <li key={index}>{error}</li>
                          )
                        )}
                      </ul>
                    </div>
                  ) : (
                    <div>{formatPasswordErrors(serverError)[0]}</div>
                  )}
                </div>
              </div>
            </div>
          )}
          <form onSubmit={handleSubmit} className="mt-6 space-y-5">
            {/* FirstName Input */}
            <div>
              <label
                className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                htmlFor="firstName"
              >
                FirstName
              </label>
              <div className="relative">
                <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                  <svg
                    className="h-5 w-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                    ></path>
                  </svg>
                </div>
                <input
                  id="firstName"
                  className={`focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pr-4 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500 ${errors.firstName ? 'border-red-500' : ''}`}
                  type="text"
                  value={formData.firstName}
                  onChange={handleInputChange}
                  placeholder="Your FirstName"
                />
              </div>
              {errors.firstName && (
                <p className="mt-1 text-xs text-red-500">{errors.firstName}</p>
              )}
            </div>
            {/* LastName Input */}
            <div>
              <label
                className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                htmlFor="lastName"
              >
                LastName
              </label>
              <div className="relative">
                <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                  <svg
                    className="h-5 w-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                    ></path>
                  </svg>
                </div>
                <input
                  id="lastName"
                  className={`focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pr-4 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500 ${errors.lastName ? 'border-red-500' : ''}`}
                  type="text"
                  value={formData.lastName}
                  onChange={handleInputChange}
                  placeholder="Your LastName"
                />
              </div>
              {errors.lastName && (
                <p className="mt-1 text-xs text-red-500">{errors.lastName}</p>
              )}
            </div>
            {/* UserName Input */}
            <div>
              <label
                className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                htmlFor="userName"
              >
                UserName
              </label>
              <div className="relative">
                <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                  <svg
                    className="h-5 w-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                    ></path>
                  </svg>
                </div>
                <input
                  id="userName"
                  className={`focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pr-4 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500 ${errors.userName ? 'border-red-500' : ''}`}
                  type="text"
                  value={formData.userName}
                  onChange={handleInputChange}
                  placeholder="Your UserName"
                />
              </div>
              {errors.userName && (
                <p className="mt-1 text-xs text-red-500">{errors.userName}</p>
              )}
            </div>

            {/* Email Input */}
            <div>
              <label
                className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                htmlFor="email"
              >
                Email Address
              </label>
              <div className="relative">
                <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                  <svg
                    className="h-5 w-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                    ></path>
                  </svg>
                </div>
                <input
                  id="email"
                  className={`focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pr-4 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500 ${errors.email ? 'border-red-500' : ''}`}
                  type="email"
                  value={formData.email}
                  onChange={handleInputChange}
                  placeholder="Your email address"
                />
              </div>
              {errors.email && (
                <p className="mt-1 text-xs text-red-500">{errors.email}</p>
              )}
            </div>

            {/* Password Input */}
            <div>
              <label
                className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                htmlFor="password"
              >
                Password
              </label>
              <div className="relative">
                <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                  <svg
                    className="h-5 w-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                    ></path>
                  </svg>
                </div>
                <input
                  id="password"
                  className={`focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pr-12 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500 ${errors.password ? 'border-red-500' : ''}`}
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={handleInputChange}
                  placeholder="Create a secure password"
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 flex items-center pr-3"
                  onClick={togglePasswordVisibility}
                >
                  {showPassword ? (
                    <svg
                      className="h-5 w-5 text-gray-400 hover:text-gray-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7A9.97 9.97 0 014.02 8.971m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18"
                      ></path>
                    </svg>
                  ) : (
                    <svg
                      className="h-5 w-5 text-gray-400 hover:text-gray-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                      ></path>
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                      ></path>
                    </svg>
                  )}
                </button>
              </div>{' '}
              {errors.password && (
                <p className="mt-1 text-xs text-red-500">{errors.password}</p>
              )}
              {/* Password Requirements */}
              {formData.password && (
                <div className="mt-2 rounded-lg border bg-gray-50 p-3 dark:bg-gray-700">
                  <p className="mb-2 text-xs font-medium text-gray-600 dark:text-gray-300">
                    Password Requirements:
                  </p>
                  <div className="space-y-1">
                    <div
                      className={`flex items-center text-xs ${formData.password.length >= 6 ? 'text-green-600' : 'text-gray-500'}`}
                    >
                      <svg
                        className={`mr-2 h-3 w-3 ${formData.password.length >= 6 ? 'text-green-600' : 'text-gray-400'}`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                      At least 6 characters
                    </div>
                    <div
                      className={`flex items-center text-xs ${/[A-Z]/.test(formData.password) ? 'text-green-600' : 'text-gray-500'}`}
                    >
                      <svg
                        className={`mr-2 h-3 w-3 ${/[A-Z]/.test(formData.password) ? 'text-green-600' : 'text-gray-400'}`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                      One uppercase letter
                    </div>
                    <div
                      className={`flex items-center text-xs ${/[a-z]/.test(formData.password) ? 'text-green-600' : 'text-gray-500'}`}
                    >
                      <svg
                        className={`mr-2 h-3 w-3 ${/[a-z]/.test(formData.password) ? 'text-green-600' : 'text-gray-400'}`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                      One lowercase letter
                    </div>
                    <div
                      className={`flex items-center text-xs ${/\d/.test(formData.password) ? 'text-green-600' : 'text-gray-500'}`}
                    >
                      <svg
                        className={`mr-2 h-3 w-3 ${/\d/.test(formData.password) ? 'text-green-600' : 'text-gray-400'}`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                      One number
                    </div>
                    <div
                      className={`flex items-center text-xs ${/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(formData.password) ? 'text-green-600' : 'text-gray-500'}`}
                    >
                      <svg
                        className={`mr-2 h-3 w-3 ${/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(formData.password) ? 'text-green-600' : 'text-gray-400'}`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                      One special character (!@#$%^&*)
                    </div>
                    <div
                      className={`flex items-center text-xs ${formData.confirmPassword && formData.password === formData.confirmPassword ? 'text-green-600' : 'text-gray-500'}`}
                    >
                      <svg
                        className={`mr-2 h-3 w-3 ${formData.confirmPassword && formData.password === formData.confirmPassword ? 'text-green-600' : 'text-gray-400'}`}
                        fill="currentColor"
                        viewBox="0 0 20 20"
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                      Passwords match
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Confirm Password Input */}
            <div>
              <label
                className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                htmlFor="confirmPassword"
              >
                Confirm Password
              </label>
              <div className="relative">
                <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                  <svg
                    className="h-5 w-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                    ></path>
                  </svg>
                </div>
                <input
                  id="confirmPassword"
                  className={`focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pr-12 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500 ${errors.confirmPassword ? 'border-red-500' : ''}`}
                  type={showConfirmPassword ? 'text' : 'password'}
                  value={formData.confirmPassword}
                  onChange={handleInputChange}
                  placeholder="Confirm your password"
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 flex items-center pr-3"
                  onClick={toggleConfirmPasswordVisibility}
                >
                  {showConfirmPassword ? (
                    <svg
                      className="h-5 w-5 text-gray-400 hover:text-gray-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7A9.97 9.97 0 014.02 8.971m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18"
                      ></path>
                    </svg>
                  ) : (
                    <svg
                      className="h-5 w-5 text-gray-400 hover:text-gray-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                      ></path>
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                      ></path>
                    </svg>
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="mt-1 text-xs text-red-500">
                  {errors.confirmPassword}
                </p>
              )}
            </div>

            {/* Optional Fields Section */}
            <div className="mt-8 border-t border-gray-200 pt-6 dark:border-gray-700">
              <div className="mb-4 flex items-center">
                <div className="flex-shrink-0">
                  <svg
                    className="h-6 w-6 text-green-600 dark:text-green-500"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-8.707l-3-3a1 1 0 00-1.414 0l-3 3a1 1 0 001.414 1.414L9 9.414V13a1 1 0 102 0V9.414l1.293 1.293a1 1 0 001.414-1.414z"
                      clipRule="evenodd"
                    ></path>
                  </svg>
                </div>
                <h3 className="ml-2 text-lg font-medium text-gray-700 dark:text-gray-300">
                  Additional Information
                </h3>
              </div>
              <div className="space-y-4 rounded-lg bg-gray-50 p-4 dark:bg-gray-800/50">
                {' '}
                {/* Added space-y-4 for consistent spacing */}
                {/* Profile Image Upload */}
                <div className="mb-4">
                  <label
                    className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                    htmlFor="image"
                  >
                    Profile Image
                  </label>
                  <div className="flex items-center space-x-4">
                    <div className="relative flex h-20 w-20 items-center justify-center overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700">
                      {formData.image ? (
                        <img
                          src={URL.createObjectURL(formData.image)}
                          alt="Profile preview"
                          className="h-full w-full object-cover"
                        />
                      ) : (
                        <svg
                          className="h-10 w-10 text-gray-400"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                          xmlns="http://www.w3.org/2000/svg"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth="2"
                            d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                          ></path>
                        </svg>
                      )}
                    </div>
                    <div className="flex-1">
                      <input
                        id="image"
                        className="block w-full cursor-pointer rounded-lg border border-gray-300 bg-white text-sm text-gray-700 focus:outline-none dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300"
                        type="file"
                        accept="image/*"
                        onChange={handleFileChange}
                      />
                      <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">
                        PNG, JPG or GIF (Max. 2MB)
                      </p>
                    </div>
                  </div>
                </div>
                {/* Gender Select */}
                <div>
                  {' '}
                  {/* Removed mb-4 as space-y-4 on parent handles it */}
                  <label
                    className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                    htmlFor="gender"
                  >
                    Gender
                  </label>
                  <div className="relative">
                    <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                      <svg
                        className="h-5 w-5 text-gray-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        xmlns="http://www.w3.org/2000/svg"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth="2"
                          d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                        ></path>
                      </svg>
                    </div>
                    <select
                      id="gender"
                      className="focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500"
                      value={formData.gender}
                      onChange={handleInputChange}
                    >
                      <option value="">Select gender</option>
                      <option value="male">Male</option>
                      <option value="female">Female</option>
                      <option value="other">Other</option>
                    </select>
                  </div>
                </div>
                {/* Favorite Team Select */}
                <div>
                  <label
                    className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                    htmlFor="FavoriteTeamId"
                  >
                    Favorite Team
                  </label>
                  <div className="relative">
                    <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                      {/* You can use a football or team related icon here */}
                      <svg
                        className="h-5 w-5 text-gray-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        xmlns="http://www.w3.org/2000/svg"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth="2"
                          d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                        ></path>
                        {/* Example icon, change as needed */}
                      </svg>
                    </div>
                    <select
                      id="FavoriteTeamId"
                      className="focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500"
                      value={
                        formData.FavoriteTeamId === null
                          ? ''
                          : formData.FavoriteTeamId
                      } // Handle null for default option
                      onChange={handleInputChange}
                    >
                      <option value="">Select your favorite team</option>
                      {teams.map((team) => (
                        <option key={team.id} value={team.id}>
                          {team.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  {/* You can add error display for FavoriteTeamId if needed */}
                  {/* {errors.FavoriteTeamId && (<p className="text-red-500 text-xs mt-1">{errors.FavoriteTeamId}</p>)} */}
                </div>
                {/* Phone Number Input */}
                <div>
                  <label
                    className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                    htmlFor="phoneNumber"
                  >
                    Phone Number
                  </label>
                  <PhoneInput
                    country={'us'}
                    value={formData.phoneNumber}
                    onChange={(phone) => {
                      setFormData((prev) => ({ ...prev, phoneNumber: phone }));
                      if (errors.phoneNumber) {
                        setErrors((prev) => {
                          const newErrors = { ...prev };
                          delete newErrors.phoneNumber;
                          return newErrors;
                        });
                      }
                    }}
                    inputProps={{
                      name: 'phoneNumber',
                      id: 'phoneNumber',
                    }} // Removed required: true to make it optional
                    containerClass="w-full"
                    inputClass={`!w-full block pr-4 py-3 text-gray-700 bg-white border rounded-lg dark:bg-gray-800 dark:text-gray-300 dark:border-gray-600 focus:border-green-500 focus:ring-opacity-40 dark:focus:border-green-500 focus:outline-none focus:ring focus:ring-green-300 ${errors.phoneNumber ? '!border-red-500' : ''}`}
                    buttonClass="dark:bg-gray-700 dark:hover:bg-gray-600 dark:border-gray-600"
                    dropdownClass="dark:bg-gray-700 dark:text-gray-200"
                    searchClass="dark:bg-gray-800 dark:text-gray-200"
                  />
                  {errors.phoneNumber && (
                    <p className="mt-1 text-xs text-red-500">
                      {errors.phoneNumber}
                    </p>
                  )}
                </div>
                {/* Age Input */}
                <div>
                  <label
                    className="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
                    htmlFor="age"
                  >
                    Age
                  </label>
                  <div className="relative">
                    <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
                      <svg
                        className="h-5 w-5 text-gray-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        xmlns="http://www.w3.org/2000/svg"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth="2"
                          d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                        ></path>
                      </svg>
                    </div>
                    <input
                      id="age"
                      className={`focus:ring-opacity-40 block w-full rounded-lg border bg-white py-3 pr-4 pl-10 text-gray-700 focus:border-green-500 focus:ring focus:ring-green-300 focus:outline-none dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:focus:border-green-500 ${errors.age ? 'border-red-500' : ''}`}
                      type="number"
                      value={formData.age === 0 ? '' : formData.age}
                      onChange={handleInputChange}
                      min="13"
                      max="120"
                      placeholder="Your age"
                    />
                  </div>
                  {errors.age && (
                    <p className="mt-1 text-xs text-red-500">{errors.age}</p>
                  )}
                </div>
              </div>
            </div>

            {/* Register Button */}
            <button
              type="submit"
              disabled={isLoading}
              className={`focus:ring-opacity-50 mt-4 flex w-full transform items-center justify-center rounded-lg px-6 py-3 text-sm font-medium tracking-wide text-white capitalize transition-colors duration-300 focus:ring focus:ring-green-300 focus:outline-none ${isLoading ? 'bg-gray-500' : 'bg-gradient-to-r from-green-600 to-blue-600 hover:from-green-700 hover:to-blue-700'}`}
            >
              {isLoading ? (
                <>
                  <div className="mr-3 h-5 w-5 animate-spin rounded-full border-b-2 border-white"></div>
                  Creating Your Account...
                </>
              ) : (
                <>
                  <svg
                    className="mr-2 h-5 w-5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                    ></path>
                  </svg>
                  Join the League
                </>
              )}
            </button>

            {/* Sign In Link */}
            <div className="mt-6 flex items-center justify-center">
              <div className="h-0.5 w-1/5 rounded bg-gradient-to-r from-transparent via-gray-300 to-transparent md:w-1/4 dark:via-gray-600"></div>
              <Link
                href="/login"
                className="group relative mx-3 px-4 py-2 text-sm font-medium text-green-600 transition-all duration-300 hover:text-white dark:text-green-400"
              >
                <span className="absolute inset-0 h-full w-full translate-x-0 -skew-x-12 transform rounded-lg bg-gradient-to-r from-green-600 to-blue-600 opacity-0 transition-all duration-300 ease-out group-hover:skew-x-12 group-hover:opacity-100"></span>
                <span className="relative flex items-center justify-center">
                  <svg
                    className="mr-2 h-4 w-4 transform transition-transform duration-300 group-hover:rotate-12"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1"
                    ></path>
                  </svg>
                  Already in the team? Sign In
                </span>
              </Link>
              <div className="h-0.5 w-1/5 rounded bg-gradient-to-r from-transparent via-gray-300 to-transparent md:w-1/4 dark:via-gray-600"></div>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
