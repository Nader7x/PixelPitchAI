'use client';
import { useState, ChangeEvent, FC, useEffect } from 'react';
import {
  PencilIcon,
  CheckIcon,
  XMarkIcon,
  ArrowPathIcon,
} from '@heroicons/react/24/outline';
import { motion, AnimatePresence } from 'framer-motion';

interface Profile {
  avatarUrl: string;
  username: string;
  email: string;
  age: number;
  gender: string;
  favoriteTeam: string | null;
  favoriteTeamId?: number | null;
}

type SaveResult = { success: boolean; message?: string };
type ProfileCardProps = {
  initialProfile: Profile;
  onProfileSave?: (profile: Profile, avatarFile?: File) => Promise<SaveResult>;
  isLoading?: boolean;
  isDarkMode?: boolean;
};

export const ProfileCard: FC<ProfileCardProps> = ({
  initialProfile,
  onProfileSave,
  isLoading = false,
  isDarkMode = false,
}) => {
  const [profile, setProfile] = useState<Profile>(initialProfile);
  const [editField, setEditField] = useState<keyof Profile | null>(null);
  const [draftValue, setDraftValue] = useState<string>('');
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<{
    type: 'success' | 'error';
    message: string;
  } | null>(null);
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [highlighted, setHighlighted] = useState<keyof Profile | null>(null);

  // Highlight field when starting to edit
  useEffect(() => {
    if (editField) {
      setHighlighted(editField);
      const timer = setTimeout(() => setHighlighted(null), 700);
      return () => clearTimeout(timer);
    }
  }, [editField]);

  const startEdit = (field: keyof Profile) => {
    setDraftValue(String(profile[field] || ''));
    setEditField(field);
  };

  const cancelEdit = () => setEditField(null);

  const saveEdit = async () => {
    if (editField) {
      const updated = {
        ...profile,
        [editField]: editField === 'age' ? Number(draftValue) : draftValue,
      } as Profile;
      setProfile(updated);
      setEditField(null);

      if (onProfileSave) {
        await saveProfile(updated);
      }
    }
  };

  const saveProfile = async (updatedProfile: Profile, newAvatarFile?: File) => {
    if (onProfileSave) {
      setSaving(true);
      setFeedback(null);

      try {
        const result = await onProfileSave(
          updatedProfile,
          newAvatarFile || avatarFile || undefined
        );

        setFeedback(
          result.success
            ? { type: 'success', message: 'Profile updated!' }
            : { type: 'error', message: result.message || 'Update failed.' }
        );

        // Clear avatar file after successful save
        if (result.success && avatarFile) {
          setAvatarFile(null);
        }
      } catch (error) {
        setFeedback({
          type: 'error',
          message: 'An unexpected error occurred.',
        });
      } finally {
        setSaving(false);
        // Clear feedback after a delay
        setTimeout(() => setFeedback(null), 3000);
      }
    }
  };

  const handleAvatarChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Store the file for later upload
      setAvatarFile(file);

      // Show preview image
      const reader = new FileReader();
      reader.onload = () => {
        setProfile((prev) => ({
          ...prev,
          avatarUrl: reader.result as string,
        }));
      };
      reader.readAsDataURL(file);

      // Automatically trigger save if onProfileSave is provided
      if (onProfileSave) {
        saveProfile(profile, file).then();
      }
    }
  };

  return (
    <motion.div
      className={`w-full overflow-hidden rounded-3xl border shadow-2xl transition-colors duration-300 ${
        isDarkMode ? 'border-gray-700 bg-gray-800' : 'border-amber-100 bg-white'
      }`}
      initial={{ scale: 0.95, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      whileHover={{
        scale: 1.02,
        rotateX: 2,
        rotateY: 2,
        boxShadow: isDarkMode
          ? '0 25px 50px -12px rgba(59, 130, 246, 0.5)'
          : '0 25px 50px -12px rgba(245, 158, 11, 0.5)',
      }}
      transition={{ duration: 0.5 }}
    >
      {/* Header with wave animation */}
      <motion.div
        className={`relative h-24 overflow-hidden transition-colors duration-300 ${
          isDarkMode
            ? 'bg-gradient-to-r from-blue-600 to-blue-400'
            : 'bg-gradient-to-r from-amber-300 to-amber-100'
        }`}
        initial={{ backgroundPosition: '0% 50%' }}
        animate={{
          backgroundPosition: ['0% 50%', '100% 50%', '0% 50%'],
        }}
        transition={{
          repeat: Infinity,
          duration: 15,
          ease: 'linear',
        }}
      >
        <div className="bg-pattern absolute inset-0 opacity-20"></div>
      </motion.div>

      {/* Avatar section - separated from header for proper positioning */}
      <div className="relative flex justify-center">
        <motion.div
          className={`group absolute -top-14 z-10 h-28 w-28 rounded-full border-4 shadow-lg transition-colors duration-300 ${
            isDarkMode ? 'border-gray-700 bg-gray-700' : 'border-white bg-white'
          }`}
          initial={{ y: -10, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          transition={{ delay: 0.3, type: 'spring', stiffness: 200 }}
        >
          {isLoading ? (
            <div className="flex h-full w-full animate-pulse items-center justify-center rounded-full bg-gray-200">
              <ArrowPathIcon className="h-8 w-8 animate-spin text-gray-400" />
            </div>
          ) : (
            <>
              <motion.img
                className="h-full w-full rounded-full object-cover"
                src={profile.avatarUrl || '/default-avatar.png'}
                alt={`${profile.username}'s avatar`}
                whileHover={{ scale: 1.05 }}
              />
              <motion.label
                className="bg-opacity-30 absolute inset-0 flex cursor-pointer items-center justify-center rounded-full bg-black opacity-0 transition group-hover:opacity-100"
                aria-label="Change profile picture"
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
              >
                <PencilIcon className="h-6 w-6 text-white" />
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleAvatarChange}
                  className="hidden"
                  disabled={saving}
                />
              </motion.label>
            </>
          )}
        </motion.div>
      </div>

      {/* Content section - adjusted padding to accommodate avatar */}
      <motion.div
        className="px-8 pt-16 pb-8" // Changed from pt-24 to pt-16
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.3 }}
      >
        <motion.div className="mb-4 text-center">
          <motion.h2
            className={`text-2xl font-bold transition-colors duration-300 ${
              isDarkMode ? 'text-white' : 'text-gray-800'
            }`}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.4 }}
          >
            {profile.username}
          </motion.h2>
          <motion.p
            className={`text-sm transition-colors duration-300 ${
              isDarkMode ? 'text-gray-300' : 'text-gray-500'
            }`}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.5 }}
          >
            {profile.email}
          </motion.p>
        </motion.div>
        {profile.favoriteTeam && (
          <motion.div
            className="mb-6 text-center"
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.6 }}
          >
            <span
              className={`inline-block rounded-full px-4 py-1 text-xs font-semibold transition-colors duration-300 ${
                isDarkMode
                  ? 'bg-gradient-to-r from-blue-800 to-blue-700 text-blue-200'
                  : 'bg-gradient-to-r from-amber-100 to-amber-200 text-amber-800'
              }`}
            >
              Favorite Team: {profile.favoriteTeam}
            </span>
          </motion.div>
        )}
        <div
          className={`divide-y transition-colors duration-300 ${
            isDarkMode ? 'divide-gray-600' : 'divide-gray-200'
          }`}
        >
          {(['email', 'username', 'age'] as (keyof Profile)[]).map(
            (field, index) => {
              const label = field.charAt(0).toUpperCase() + field.slice(1);
              const isEditing = editField === field;
              const value = isEditing
                ? draftValue
                : String(profile[field] || '');
              const isHighlighted = highlighted === field;

              return (
                <motion.div
                  key={field}
                  className={`flex items-center justify-between rounded-lg py-3 ${isHighlighted ? 'highlight-field' : ''}`}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.2 + index * 0.1 }}
                >
                  <div
                    className={`w-28 font-medium transition-colors duration-300 ${
                      isDarkMode ? 'text-gray-300' : 'text-gray-600'
                    }`}
                  >
                    {label}
                  </div>
                  <AnimatePresence mode="wait">
                    {isEditing ? (
                      <motion.div
                        className="flex-1"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                        key="editing"
                      >
                        <input
                          type={field === 'age' ? 'number' : 'text'}
                          value={draftValue}
                          onChange={(e: ChangeEvent<HTMLInputElement>) =>
                            setDraftValue(e.target.value)
                          }
                          className={`w-full rounded border px-2 py-1 transition-all ${
                            isDarkMode
                              ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400'
                              : 'border-gray-300 bg-white text-gray-900 focus:border-amber-400 focus:ring-amber-400'
                          }`}
                          aria-label={`Edit ${label}`}
                          autoFocus
                        />
                      </motion.div>
                    ) : (
                      <motion.div
                        className="flex-1"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                        key="viewing"
                      >
                        <span
                          className={`transition-colors duration-300 ${
                            isDarkMode ? 'text-gray-200' : 'text-gray-800'
                          }`}
                        >
                          {value}
                        </span>
                      </motion.div>
                    )}
                  </AnimatePresence>
                  <div className="ml-4 flex-shrink-0">
                    <AnimatePresence mode="wait">
                      {isEditing ? (
                        <motion.div
                          className="flex space-x-2"
                          initial={{ opacity: 0, scale: 0.8 }}
                          animate={{ opacity: 1, scale: 1 }}
                          exit={{ opacity: 0, scale: 0.8 }}
                          key="edit-buttons"
                        >
                          <motion.button
                            onClick={saveEdit}
                            disabled={saving}
                            className={`cursor-pointer rounded-full p-1 transition-colors ${
                              isDarkMode
                                ? 'bg-green-900 text-green-400 hover:bg-green-800'
                                : 'bg-green-50 text-green-600 hover:bg-green-100'
                            }`}
                            aria-label="Save changes"
                            whileHover={{ scale: 1.1 }}
                            whileTap={{ scale: 0.9 }}
                          >
                            <CheckIcon className="h-5 w-5" />
                          </motion.button>
                          <motion.button
                            onClick={cancelEdit}
                            disabled={saving}
                            className={`cursor-pointer rounded-full p-1 transition-colors ${
                              isDarkMode
                                ? 'bg-red-900 text-red-400 hover:bg-red-800'
                                : 'bg-red-50 text-red-600 hover:bg-red-100'
                            }`}
                            aria-label="Cancel editing"
                            whileHover={{ scale: 1.1 }}
                            whileTap={{ scale: 0.9 }}
                          >
                            <XMarkIcon className="h-5 w-5" />
                          </motion.button>
                        </motion.div>
                      ) : (
                        <motion.button
                          onClick={() => startEdit(field)}
                          className={`cursor-pointer rounded-full p-1 transition-colors ${
                            isDarkMode
                              ? 'bg-blue-900 text-blue-400 hover:bg-blue-800'
                              : 'bg-blue-50 text-blue-600 hover:bg-blue-100'
                          }`}
                          aria-label={`Edit ${label}`}
                          whileHover={{ scale: 1.1 }}
                          whileTap={{ scale: 0.9 }}
                          initial={{ opacity: 0 }}
                          animate={{ opacity: 1 }}
                          exit={{ opacity: 0 }}
                          key="edit-button"
                        >
                          <PencilIcon className="h-5 w-5" />
                        </motion.button>
                      )}
                    </AnimatePresence>
                  </div>
                </motion.div>
              );
            }
          )}

          {/* Gender Selection with Dropdown */}
          <motion.div
            key="gender-field"
            className={`flex items-center justify-between rounded-lg py-3 ${highlighted === 'gender' ? 'highlight-field' : ''} `}
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.5 }}
          >
            <div
              className={`w-28 font-medium transition-colors duration-300 ${
                isDarkMode ? 'text-gray-300' : 'text-gray-600'
              }`}
            >
              Gender
            </div>
            <AnimatePresence mode="wait">
              {editField === 'gender' ? (
                <motion.div
                  className="flex-1"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  key="editing-gender"
                >
                  <select
                    value={draftValue}
                    onChange={(e) => setDraftValue(e.target.value)}
                    className={`w-full rounded border px-2 py-1 transition-all ${
                      isDarkMode
                        ? 'border-gray-600 bg-gray-700 text-white focus:border-blue-400 focus:ring-blue-400'
                        : 'border-gray-300 bg-white text-gray-900 focus:border-amber-400 focus:ring-amber-400'
                    }`}
                    aria-label="Edit Gender"
                    autoFocus
                  >
                    <option value="">Select Gender</option>
                    <option value="Male">Male</option>
                    <option value="Female">Female</option>
                    <option value="Other">Other</option>
                    <option value="Prefer not to say">Prefer not to say</option>
                  </select>
                </motion.div>
              ) : (
                <motion.div
                  className="flex-1"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  key="viewing-gender"
                >
                  <span
                    className={`transition-colors duration-300 ${
                      isDarkMode ? 'text-gray-200' : 'text-gray-800'
                    }`}
                  >
                    {profile.gender || 'Not specified'}
                  </span>
                </motion.div>
              )}
            </AnimatePresence>
            <div className="ml-4 flex-shrink-0">
              <AnimatePresence mode="wait">
                {editField === 'gender' ? (
                  <motion.div
                    className="flex space-x-2"
                    initial={{ opacity: 0, scale: 0.8 }}
                    animate={{ opacity: 1, scale: 1 }}
                    exit={{ opacity: 0, scale: 0.8 }}
                    key="edit-gender-buttons"
                  >
                    <motion.button
                      onClick={saveEdit}
                      disabled={saving}
                      className={`cursor-pointer rounded-full p-1 transition-colors ${
                        isDarkMode
                          ? 'bg-green-900 text-green-400 hover:bg-green-800'
                          : 'bg-green-50 text-green-600 hover:bg-green-100'
                      }`}
                      aria-label="Save changes"
                      whileHover={{ scale: 1.1 }}
                      whileTap={{ scale: 0.9 }}
                    >
                      <CheckIcon className="h-5 w-5" />
                    </motion.button>
                    <motion.button
                      onClick={cancelEdit}
                      disabled={saving}
                      className={`cursor-pointer rounded-full p-1 transition-colors ${
                        isDarkMode
                          ? 'bg-red-900 text-red-400 hover:bg-red-800'
                          : 'bg-red-50 text-red-600 hover:bg-red-100'
                      }`}
                      aria-label="Cancel editing"
                      whileHover={{ scale: 1.1 }}
                      whileTap={{ scale: 0.9 }}
                    >
                      <XMarkIcon className="h-5 w-5" />
                    </motion.button>
                  </motion.div>
                ) : (
                  <motion.button
                    onClick={() => startEdit('gender')}
                    className={`cursor-pointer rounded-full p-1 transition-colors ${
                      isDarkMode
                        ? 'bg-blue-900 text-blue-400 hover:bg-blue-800'
                        : 'bg-blue-50 text-blue-600 hover:bg-blue-100'
                    }`}
                    aria-label="Edit Gender"
                    whileHover={{ scale: 1.1 }}
                    whileTap={{ scale: 0.9 }}
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    key="edit-gender-button"
                  >
                    <PencilIcon className="h-5 w-5" />
                  </motion.button>
                )}
              </AnimatePresence>
            </div>
          </motion.div>

          {/* Feedback message for save operations */}
          <AnimatePresence>
            {feedback && (
              <motion.div
                className={`mt-6 rounded-lg p-3 ${
                  feedback.type === 'success'
                    ? 'bg-green-50 text-green-700'
                    : 'bg-red-50 text-red-700'
                }`}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
              >
                {feedback.message}
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </motion.div>
    </motion.div>
  );
};
