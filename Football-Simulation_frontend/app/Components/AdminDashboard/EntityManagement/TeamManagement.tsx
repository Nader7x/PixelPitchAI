'use client';

import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import DataTable from '../DataTable';
import teamService, {
  Team,
  CreateTeamRequest,
  UpdateTeamRequest,
} from '@/Services/TeamService';
import stadiumService from '@/Services/StadiumService';
import coachService from '@/Services/CoachService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const TeamManagement = () => {
  const [teams, setTeams] = useState<Team[]>([]);
  const [stadiums, setStadiums] = useState<any[]>([]);
  const [coaches, setCoaches] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedTeam, setSelectedTeam] = useState<Team | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [imageFile, setImageFile] = useState<File | null>(null);

  // Form state
  const [formData, setFormData] = useState<
    CreateTeamRequest | UpdateTeamRequest
  >({
    name: '',
    shortName: '',
    primaryColor: '',
    secondaryColor: '',
    city: '',
    country: '',
    league: '',
    FoundationDate: undefined,
    stadiumId: undefined,
    coachId: undefined,
  });

  // Fetch teams, stadiums, and coaches on component mount
  useEffect(() => {
    fetchTeams().then();
    fetchStadiums().then();
    fetchCoaches().then();
  }, []);

  const fetchTeams = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const fetchedTeams = await teamService.getAllTeams();
      setTeams(fetchedTeams);
    } catch (err) {
      setError('Failed to fetch teams');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const fetchStadiums = async () => {
    try {
      const response = await stadiumService.getStadiums();
      setStadiums(Array.isArray(response) ? response : []);
    } catch (err) {
      console.error('Failed to fetch stadiums:', err);
    }
  };

  const fetchCoaches = async () => {
    try {
      const response = await coachService.getCoaches({});
      setCoaches(Array.isArray(response) ? response : []);
    } catch (err) {
      console.error('Failed to fetch coaches:', err);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, files } = e.target;

    if (type === 'file' && files && files.length > 0) {
      setImageFile(files[0]);
    } else {
      setFormData({
        ...formData,
        [name]:
          name === 'FoundationDate'
            ? value === ''
              ? undefined
              : value
            : value,
      });
    }
  };

  const handleSelectChange = (name: string, value: string) => {
    setFormData({
      ...formData,
      [name]: value === '_none' ? undefined : Number(value),
    });
  };

  const handleCreateTeam = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      // Create FormData object for multipart/form-data
      const formDataObj = new FormData();

      // Append all form fields
      Object.entries(formData).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          formDataObj.append(key, String(value));
        }
      });

      // Append image file if selected
      if (imageFile) {
        formDataObj.append('Image', imageFile);
      }

      await teamService.createTeamWithImage(formDataObj);
      setSuccess('Team created successfully');
      resetForm();
      await fetchTeams();
    } catch (err) {
      setError('Failed to create team');
      console.error(err);
    }
  };

  const handleUpdateTeam = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTeam) return;

    setError(null);
    setSuccess(null);

    try {
      // Create FormData object for multipart/form-data
      const formDataObj = new FormData();

      // Append all form fields that have changed
      Object.entries(formData).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          formDataObj.append(key, String(value));
        }
      });

      // Append image file if selected
      if (imageFile) {
        formDataObj.append('Image', imageFile);
      }

      await teamService.updateTeamWithImage(selectedTeam.id, formDataObj);
      setSuccess('Team updated successfully');
      resetForm();
      await fetchTeams();
    } catch (err) {
      setError('Failed to update team');
      console.error(err);
    }
  };

  const handleDeleteTeam = async (team: Team) => {
    if (!window.confirm(`Are you sure you want to delete ${team.name}?`))
      return;

    setError(null);
    setSuccess(null);

    try {
      await teamService.deleteTeam(team.id);
      setSuccess('Team deleted successfully');
      await fetchTeams();
    } catch (err) {
      setError('Failed to delete team');
      console.error(err);
    }
  };

  const handleEditClick = (team: Team) => {
    setSelectedTeam(team);
    setFormData({
      name: team.name,
      shortName: team.shortName || '',
      primaryColor: team.primaryColor || '',
      secondaryColor: team.secondaryColor || '',
      city: team.city || '',
      country: team.country || '',
      league: team.league || '',
      FoundationDate: team.foundationDate,
      stadiumId: team.stadiumId,
      coachId: team.coachId,
    });
    setIsEditing(true);
    setShowForm(true);
    setImageFile(null); // Reset image file when editing
  };

  const resetForm = () => {
    setFormData({
      name: '',
      shortName: '',
      primaryColor: '',
      secondaryColor: '',
      city: '',
      country: '',
      league: '',
      FoundationDate: undefined,
      stadiumId: undefined,
      coachId: undefined,
    });
    setSelectedTeam(null);
    setIsEditing(false);
    setShowForm(false);
    setImageFile(null);
  };

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'name', label: 'Name' },
    { key: 'shortName', label: 'Short Name' },
    { key: 'country', label: 'Country' },
    { key: 'league', label: 'League' },
    { key: 'city', label: 'City' },
    {
      key: 'logo',
      label: 'Logo',
      render: (team: Team) =>
        team.logo ? (
          <img
            src={team.logo}
            alt={`${team.name} logo`}
            className="h-10 w-10 object-contain"
          />
        ) : (
          'No logo'
        ),
    },
    {
      key: 'FoundationDate',
      label: 'Founded',
      render: (team: Team) =>
        team.foundationDate
          ? new Date(team.foundationDate).toLocaleDateString()
          : '-',
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <motion.h2
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.5 }}
          className="bg-gradient-to-r from-green-400 to-blue-500 bg-clip-text text-2xl font-bold text-transparent"
        >
          Team Management
        </motion.h2>
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.5 }}
        >
          <Button
            onClick={() => {
              resetForm();
              setShowForm(!showForm);
            }}
            className="bg-gradient-to-r from-green-600 to-blue-600 transition-all duration-300 hover:from-green-500 hover:to-blue-500"
          >
            <svg
              className="mr-2 h-5 w-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 6v6m0 0v6m0-6h6m-6 0H6"
              />
            </svg>
            {showForm ? 'Cancel' : 'Add New Team'}
          </Button>
        </motion.div>
      </div>

      {error && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
        >
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        </motion.div>
      )}

      {success && (
        <motion.div
          initial={{ opacity: 0, y: -10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
        >
          <Alert>
            <AlertDescription>{success}</AlertDescription>
          </Alert>
        </motion.div>
      )}

      {showForm && (
        <motion.div
          initial={{ opacity: 0, height: 0, overflow: 'hidden' }}
          animate={{ opacity: 1, height: 'auto', overflow: 'visible' }}
          exit={{ opacity: 0, height: 0, overflow: 'hidden' }}
          transition={{ duration: 0.4 }}
          className="overflow-hidden rounded-xl border border-gray-700 bg-gray-800/50 shadow-lg backdrop-blur-sm"
        >
          <div className="p-6">
            <h3 className="mb-6 bg-gradient-to-r from-green-400 to-blue-500 bg-clip-text text-xl font-medium text-transparent">
              {isEditing ? 'Edit Team' : 'Create New Team'}
            </h3>
            <form
              onSubmit={isEditing ? handleUpdateTeam : handleCreateTeam}
              className="space-y-6"
            >
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.1 }}
                  className="space-y-2"
                >
                  <Label htmlFor="name" className="text-gray-300">
                    Team Name *
                  </Label>
                  <Input
                    id="name"
                    name="name"
                    value={formData.name}
                    onChange={handleInputChange}
                    required
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.15 }}
                  className="space-y-2"
                >
                  <Label htmlFor="shortName" className="text-gray-300">
                    Short Name
                  </Label>
                  <Input
                    id="shortName"
                    name="shortName"
                    value={formData.shortName}
                    onChange={handleInputChange}
                    placeholder="e.g., FCB, RM"
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.2 }}
                  className="space-y-2"
                >
                  <Label htmlFor="country" className="text-gray-300">
                    Country
                  </Label>
                  <Input
                    id="country"
                    name="country"
                    value={formData.country}
                    onChange={handleInputChange}
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.25 }}
                  className="space-y-2"
                >
                  <Label htmlFor="league" className="text-gray-300">
                    League
                  </Label>
                  <Input
                    id="league"
                    name="league"
                    value={formData.league || ''}
                    onChange={handleInputChange}
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.3 }}
                  className="space-y-2"
                >
                  <Label htmlFor="city" className="text-gray-300">
                    City
                  </Label>
                  <Input
                    id="city"
                    name="city"
                    value={formData.city}
                    onChange={handleInputChange}
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.35 }}
                  className="space-y-2"
                >
                  <Label htmlFor="FoundationDate" className="text-gray-300">
                    Foundation Date
                  </Label>
                  <Input
                    id="FoundationDate"
                    name="FoundationDate"
                    type="date"
                    value={formData.FoundationDate || ''}
                    onChange={handleInputChange}
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.4 }}
                  className="space-y-2"
                >
                  <Label htmlFor="primaryColor" className="text-gray-300">
                    Primary Color
                  </Label>
                  <div className="flex space-x-2">
                    <Input
                      id="primaryColor"
                      name="primaryColor"
                      value={formData.primaryColor}
                      onChange={handleInputChange}
                      className="border-gray-600 bg-gray-700/50 text-gray-200"
                    />
                    {formData.primaryColor && (
                      <div
                        className="h-10 w-10 rounded-md border border-gray-600"
                        style={{ backgroundColor: formData.primaryColor }}
                      />
                    )}
                  </div>
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.45 }}
                  className="space-y-2"
                >
                  <Label htmlFor="secondaryColor" className="text-gray-300">
                    Secondary Color
                  </Label>
                  <div className="flex space-x-2">
                    <Input
                      id="secondaryColor"
                      name="secondaryColor"
                      value={formData.secondaryColor}
                      onChange={handleInputChange}
                      className="border-gray-600 bg-gray-700/50 text-gray-200"
                    />
                    {formData.secondaryColor && (
                      <div
                        className="h-10 w-10 rounded-md border border-gray-600"
                        style={{ backgroundColor: formData.secondaryColor }}
                      />
                    )}
                  </div>
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.5 }}
                  className="space-y-2"
                >
                  <Label htmlFor="stadiumId" className="text-gray-300">
                    Stadium
                  </Label>
                  <Select
                    value={formData.stadiumId?.toString() || '_none'}
                    onValueChange={(value) =>
                      handleSelectChange('stadiumId', value)
                    }
                  >
                    <SelectTrigger className="border-gray-600 bg-gray-700/50 text-gray-200">
                      <SelectValue placeholder="Select stadium" />
                    </SelectTrigger>
                    <SelectContent className="border-gray-700 bg-gray-800">
                      <SelectItem value="_none">Select Stadium</SelectItem>
                      {stadiums.map((stadium) => (
                        <SelectItem
                          key={stadium.id}
                          value={stadium.id.toString()}
                        >
                          {stadium.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.55 }}
                  className="space-y-2"
                >
                  <Label htmlFor="coachId" className="text-gray-300">
                    Coach
                  </Label>
                  <Select
                    value={formData.coachId?.toString() || '_none'}
                    onValueChange={(value) =>
                      handleSelectChange('coachId', value)
                    }
                  >
                    <SelectTrigger className="border-gray-600 bg-gray-700/50 text-gray-200">
                      <SelectValue placeholder="Select coach" />
                    </SelectTrigger>
                    <SelectContent className="border-gray-700 bg-gray-800">
                      <SelectItem value="_none">Select Coach</SelectItem>
                      {coaches.map((coach) => (
                        <SelectItem key={coach.id} value={coach.id.toString()}>
                          {coach.firstName} {coach.lastName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.6 }}
                  className="col-span-2 space-y-2"
                >
                  <Label htmlFor="image" className="text-gray-300">
                    Team Logo
                  </Label>
                  <div className="relative">
                    <Input
                      id="image"
                      name="image"
                      type="file"
                      accept="image/*"
                      onChange={handleInputChange}
                      className="border-gray-600 bg-gray-700/50 text-gray-200"
                    />
                    {imageFile && (
                      <div className="mt-2 inline-flex items-center text-sm text-green-400">
                        <svg
                          className="mr-1 h-5 w-5"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M5 13l4 4L19 7"
                          />
                        </svg>
                        {imageFile.name}
                      </div>
                    )}
                  </div>
                </motion.div>
              </div>

              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 0.3, delay: 0.65 }}
                className="flex justify-end space-x-3 pt-4"
              >
                <Button
                  type="button"
                  variant="outline"
                  onClick={resetForm}
                  className="border-gray-600 text-gray-300 transition-all duration-300 hover:bg-gray-700"
                >
                  <svg
                    className="mr-2 h-5 w-5"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                  Cancel
                </Button>
                <Button
                  type="submit"
                  className="bg-gradient-to-r from-green-600 to-blue-600 transition-all duration-300 hover:from-green-500 hover:to-blue-500"
                >
                  {isLoading ? (
                    <>
                      <svg
                        className="mr-2 -ml-1 h-4 w-4 animate-spin text-white"
                        xmlns="http://www.w3.org/2000/svg"
                        fill="none"
                        viewBox="0 0 24 24"
                      >
                        <circle
                          className="opacity-25"
                          cx="12"
                          cy="12"
                          r="10"
                          stroke="currentColor"
                          strokeWidth="4"
                        ></circle>
                        <path
                          className="opacity-75"
                          fill="currentColor"
                          d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                        ></path>
                      </svg>
                      Processing...
                    </>
                  ) : isEditing ? (
                    <>
                      <svg
                        className="mr-2 h-5 w-5"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"
                        />
                      </svg>
                      Update Team
                    </>
                  ) : (
                    <>
                      <svg
                        className="mr-2 h-5 w-5"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                        />
                      </svg>
                      Create Team
                    </>
                  )}
                </Button>
              </motion.div>
            </form>
          </div>
        </motion.div>
      )}

      <DataTable
        columns={columns}
        data={teams}
        isLoading={isLoading}
        pagination
        onEdit={handleEditClick}
        onDelete={handleDeleteTeam}
      />
    </div>
  );
};

export default TeamManagement;
