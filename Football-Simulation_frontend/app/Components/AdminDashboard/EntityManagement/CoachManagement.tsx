'use client';

import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import DataTable from '../DataTable';
import coachService, {
  Coach,
  CreateCoachDto,
  UpdateCoachDto,
  CoachFilter,
} from '@/Services/CoachService';
import teamService from '@/Services/TeamService';
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
import { Textarea } from '@/components/ui/textarea';

const CoachManagement = () => {
  const [coaches, setCoaches] = useState<Coach[]>([]);
  const [teams, setTeams] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedCoach, setSelectedCoach] = useState<Coach | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [showForm, setShowForm] = useState(false);

  // Filter state
  const [filter, setFilter] = useState<CoachFilter>({});

  // Form state
  const [formData, setFormData] = useState<CreateCoachDto | UpdateCoachDto>({
    firstName: '',
    lastName: '',
    nationality: '',
    dateOfBirth: '',
    teamId: undefined,
    preferredFormation: '',
    coachingStyle: '',
    role: '',
    yearsOfExperience: undefined,
    biography: '',
    Photo: undefined,
  });

  // File input state
  const [imageFile, setImageFile] = useState<File | null>(null);

  // Fetch coaches and teams on component mount
  useEffect(() => {
    fetchCoaches().then();
    fetchTeams().then();
  }, []);

  const fetchCoaches = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const fetchedCoaches = await coachService.getCoaches(filter);
      setCoaches(fetchedCoaches);
    } catch (err) {
      setError('Failed to fetch coaches');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const fetchTeams = async () => {
    try {
      const fetchedTeams = await teamService.getAllTeams();
      setTeams(fetchedTeams);
    } catch (err) {
      console.error('Failed to fetch teams:', err);
    }
  };

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value, type } = e.target as HTMLInputElement;

    if (type === 'file') {
      const files = (e.target as HTMLInputElement).files;
      if (files && files.length > 0) {
        setImageFile(files[0]);
        setFormData((prev) => ({
          ...prev,
          Photo: files[0],
        }));
      }
    } else if (type === 'number') {
      setFormData({
        ...formData,
        [name]: value === '_none' ? undefined : Number(value),
      });
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
    }
  };

  const handleSelectChange = (name: string, value: string) => {
    setFormData({
      ...formData,
      [name]: value === '_none' ? undefined : Number(value),
    });
  };

  const handleRoleChange = (value: string) => {
    setFormData({
      ...formData,
      role: value,
    });
  };

  const handleCoachingStyleChange = (value: string) => {
    setFormData({
      ...formData,
      coachingStyle: value,
    });
  };

  const handleFilterChange = (name: string, value: string) => {
    setFilter({
      ...filter,
      [name]:
        name === 'teamId'
          ? value === '_all'
            ? undefined
            : Number(value)
          : value,
    });
  };

  const handleCreateCoach = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await coachService.createCoach(formData as CreateCoachDto);
      setSuccess('Coach created successfully');
      resetForm();
      await fetchCoaches();
    } catch (err) {
      setError('Failed to create coach');
      console.error(err);
    }
  };

  const handleUpdateCoach = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCoach) return;

    setError(null);
    setSuccess(null);

    try {
      await coachService.updateCoach(selectedCoach.id, formData);
      setSuccess('Coach updated successfully');
      resetForm();
      await fetchCoaches();
    } catch (err) {
      setError('Failed to update coach');
      console.error(err);
    }
  };

  const handleDeleteCoach = async (coach: Coach) => {
    if (
      !window.confirm(
        `Are you sure you want to delete ${coach.firstName} ${coach.lastName}?`
      )
    )
      return;

    setError(null);
    setSuccess(null);

    try {
      await coachService.deleteCoach(coach.id);
      setSuccess('Coach deleted successfully');
      await fetchCoaches();
    } catch (err) {
      setError('Failed to delete coach');
      console.error(err);
    }
  };

  const handleEditClick = (coach: Coach) => {
    setSelectedCoach(coach);
    setFormData({
      firstName: coach.firstName || '',
      lastName: coach.lastName || '',
      nationality: coach.nationality || '',
      dateOfBirth: coach.dateOfBirth
        ? new Date(coach.dateOfBirth).toISOString().split('T')[0]
        : '',
      teamId: coach.teamId,
      preferredFormation: coach.preferredFormation || '',
      coachingStyle: coach.coachingStyle || '',
      role: coach.role || '',
      yearsOfExperience: coach.yearsOfExperience,
      biography: coach.biography || '',
    });
    setIsEditing(true);
    setShowForm(true);
  };

  const resetForm = () => {
    setFormData({
      firstName: '',
      lastName: '',
      nationality: '',
      dateOfBirth: '',
      teamId: undefined,
      preferredFormation: '',
      coachingStyle: '',
      role: '',
      yearsOfExperience: undefined,
      biography: '',
      Photo: undefined,
    });
    setImageFile(null);
    setSelectedCoach(null);
    setIsEditing(false);
    setShowForm(false);
  };

  const applyFilters = () => {
    fetchCoaches().then();
  };

  const resetFilters = () => {
    setFilter({});
    fetchCoaches().then();
  };

  // Enhanced button click handler with stopPropagation
  const handleButtonClick = (action: () => void) => (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    action();
  };

  // Enhanced form submission with proper event handling
  const handleFormSubmit =
    (handler: (e: React.FormEvent) => Promise<void>) =>
    (e: React.FormEvent) => {
      e.preventDefault();
      e.stopPropagation();
      handler(e);
    };

  const columns = [
    { key: 'id', label: 'ID' },
    {
      key: 'name',
      label: 'Name',
      render: (coach: Coach) => `${coach.firstName} ${coach.lastName}`,
    },
    { key: 'nationality', label: 'Nationality' },
    {
      key: 'dateOfBirth',
      label: 'Date of Birth',
      render: (coach: Coach) =>
        coach.dateOfBirth
          ? new Date(coach.dateOfBirth).toLocaleDateString()
          : '-',
    },
    { key: 'role', label: 'Role' },
    {
      key: 'teamId',
      label: 'Team',
      render: (coach: Coach) => {
        const team = teams.find((t) => t.id === coach.teamId);
        return team ? team.name : 'No Team';
      },
    },
    {
      key: 'image',
      label: 'Photo',
      render: (coach: Coach) =>
        coach.photoUrl ? (
          <img
            src={coach.photoUrl}
            alt={`${coach.firstName} ${coach.lastName}`}
            className="h-10 w-10 rounded-full object-contain"
          />
        ) : (
          'No photo'
        ),
    },
  ];

  return (
    <div className="space-y-6" onClick={(e) => e.stopPropagation()}>
      <div className="flex items-center justify-between">
        <h2 className="bg-gradient-to-r from-green-400 to-blue-500 bg-clip-text text-2xl font-semibold text-transparent">
          Coach Management
        </h2>
        <Button
          onClick={handleButtonClick(() => {
            resetForm();
            setShowForm(!showForm);
          })}
          className={
            showForm
              ? 'bg-gray-700 hover:bg-gray-600 focus:ring-2 focus:ring-green-500 focus:outline-none'
              : 'bg-gradient-to-r from-green-500 to-blue-500 hover:from-green-600 hover:to-blue-600 focus:ring-2 focus:ring-green-500 focus:outline-none'
          }
        >
          {showForm ? 'Cancel' : 'Add New Coach'}
        </Button>
      </div>

      {error && (
        <Alert
          variant="destructive"
          className="border-red-500 bg-red-500/10 text-red-500"
        >
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {success && (
        <Alert className="border-green-500 bg-green-500/10 text-green-500">
          <AlertDescription>{success}</AlertDescription>
        </Alert>
      )}

      {/* Filters */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 0.5 }}
        className="rounded-xl border border-gray-700 bg-gray-800/70 p-6 shadow-lg backdrop-blur-sm"
      >
        <h3 className="mb-4 bg-gradient-to-r from-green-400 to-blue-500 bg-clip-text text-lg font-medium text-transparent">
          Filter Coaches
        </h3>
        <div className="grid grid-cols-3 gap-4">
          <div>
            <Label htmlFor="filter-nationality" className="text-gray-300">
              Nationality
            </Label>
            <Input
              id="filter-nationality"
              value={filter.nationality || ''}
              onChange={(e) =>
                handleFilterChange('nationality', e.target.value)
              }
              placeholder="Filter by nationality"
              className="border-gray-600 bg-gray-700/50 text-white placeholder-gray-400 focus:border-green-500 focus:ring-green-500"
            />
          </div>

          <div>
            <Label htmlFor="filter-team" className="text-gray-300">
              Team
            </Label>
            <Select
              value={filter.teamId?.toString() || '_all'}
              onValueChange={(value) => handleFilterChange('teamId', value)}
            >
              <SelectTrigger className="border-gray-600 bg-gray-700/50 text-white focus:ring-green-500">
                <SelectValue placeholder="Select a team" />
              </SelectTrigger>
              <SelectContent className="border-gray-700 bg-gray-800 text-white">
                <SelectItem value="_all">All Teams</SelectItem>
                {teams.map((team) => (
                  <SelectItem key={team.id} value={team.id.toString()}>
                    {team.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div>
            <Label htmlFor="filter-role" className="text-gray-300">
              Role
            </Label>
            <Input
              id="filter-role"
              value={filter.role || ''}
              onChange={(e) => handleFilterChange('role', e.target.value)}
              placeholder="Filter by role"
              className="border-gray-600 bg-gray-700/50 text-white placeholder-gray-400 focus:border-green-500 focus:ring-green-500"
            />
          </div>
        </div>

        <div className="mt-4 flex justify-end space-x-2">
          <Button
            variant="outline"
            onClick={resetFilters}
            className="border-gray-600 text-gray-300 hover:bg-gray-700"
          >
            Reset Filters
          </Button>
          <Button
            onClick={applyFilters}
            className="bg-gradient-to-r from-green-500 to-blue-500 hover:from-green-600 hover:to-blue-600"
          >
            Apply Filters
          </Button>
        </div>
      </motion.div>

      {showForm && (
        <motion.form
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          onSubmit={handleFormSubmit(
            isEditing ? handleUpdateCoach : handleCreateCoach
          )}
          className="space-y-4 rounded-xl border border-gray-700 bg-gray-800/70 p-6 shadow-lg backdrop-blur-sm"
        >
          <h3 className="mb-2 bg-gradient-to-r from-green-400 to-blue-500 bg-clip-text text-lg font-medium text-transparent">
            {isEditing ? 'Edit Coach' : 'Create New Coach'}
          </h3>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="firstName" className="text-gray-300">
                First Name *
              </Label>
              <Input
                id="firstName"
                name="firstName"
                value={formData.firstName}
                onChange={handleInputChange}
                required
                className="border-gray-600 bg-gray-700/50 text-white focus:border-green-500 focus:ring-green-500"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="lastName" className="text-gray-300">
                Last Name *
              </Label>
              <Input
                id="lastName"
                name="lastName"
                value={formData.lastName}
                onChange={handleInputChange}
                required
                className="border-gray-600 bg-gray-700/50 text-white focus:border-green-500 focus:ring-green-500"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="nationality" className="text-gray-300">
                Nationality
              </Label>
              <Input
                id="nationality"
                name="nationality"
                value={formData.nationality}
                onChange={handleInputChange}
                className="border-gray-600 bg-gray-700/50 text-white focus:border-green-500 focus:ring-green-500"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="dateOfBirth" className="text-gray-300">
                Date of Birth
              </Label>
              <Input
                id="dateOfBirth"
                name="dateOfBirth"
                type="date"
                value={formData.dateOfBirth}
                onChange={handleInputChange}
                className="border-gray-600 bg-gray-700/50 text-white focus:border-green-500 focus:ring-green-500"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="role" className="text-gray-300">
                Role
              </Label>
              <Select
                value={formData.role || '_none'}
                onValueChange={handleRoleChange}
              >
                <SelectTrigger className="border-gray-600 bg-gray-700/50 text-white focus:ring-green-500">
                  <SelectValue placeholder="Select role" />
                </SelectTrigger>
                <SelectContent className="border-gray-700 bg-gray-800 text-white">
                  <SelectItem value="_none">Select Role</SelectItem>
                  <SelectItem value="Head Coach">Head Coach</SelectItem>
                  <SelectItem value="Assistant Coach">
                    Assistant Coach
                  </SelectItem>
                  <SelectItem value="Goalkeeper Coach">
                    Goalkeeper Coach
                  </SelectItem>
                  <SelectItem value="Fitness Coach">Fitness Coach</SelectItem>
                  <SelectItem value="Technical Director">
                    Technical Director
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="yearsOfExperience" className="text-gray-300">
                Years of Experience
              </Label>
              <Input
                id="yearsOfExperience"
                name="yearsOfExperience"
                type="number"
                value={formData.yearsOfExperience || ''}
                onChange={handleInputChange}
                className="border-gray-600 bg-gray-700/50 text-white focus:border-green-500 focus:ring-green-500"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="teamId" className="text-gray-300">
                Team
              </Label>
              <Select
                value={formData.teamId?.toString() || '_none'}
                onValueChange={(value) => handleSelectChange('teamId', value)}
              >
                <SelectTrigger className="border-gray-600 bg-gray-700/50 text-white focus:ring-green-500">
                  <SelectValue placeholder="Select team" />
                </SelectTrigger>
                <SelectContent className="border-gray-700 bg-gray-800 text-white">
                  <SelectItem value="_none">No Team</SelectItem>
                  {teams.map((team) => (
                    <SelectItem key={team.id} value={team.id.toString()}>
                      {team.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="preferredFormation" className="text-gray-300">
                Preferred Formation
              </Label>
              <Input
                id="preferredFormation"
                name="preferredFormation"
                value={formData.preferredFormation}
                onChange={handleInputChange}
                placeholder="e.g., 4-3-3, 4-4-2"
                className="border-gray-600 bg-gray-700/50 text-white placeholder-gray-400 focus:border-green-500 focus:ring-green-500"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="coachingStyle" className="text-gray-300">
                Coaching Style
              </Label>
              <Select
                value={formData.coachingStyle || '_none'}
                onValueChange={handleCoachingStyleChange}
              >
                <SelectTrigger className="border-gray-600 bg-gray-700/50 text-white focus:ring-green-500">
                  <SelectValue placeholder="Select coaching style" />
                </SelectTrigger>
                <SelectContent className="border-gray-700 bg-gray-800 text-white">
                  <SelectItem value="_none">Select Style</SelectItem>
                  <SelectItem value="Attacking">Attacking</SelectItem>
                  <SelectItem value="Defensive">Defensive</SelectItem>
                  <SelectItem value="Possession">Possession</SelectItem>
                  <SelectItem value="Counter-attacking">
                    Counter-attacking
                  </SelectItem>
                  <SelectItem value="Pressing">Pressing</SelectItem>
                  <SelectItem value="Total Football">Total Football</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="col-span-2 space-y-2">
              <Label htmlFor="biography" className="text-gray-300">
                Biography
              </Label>
              <Textarea
                id="biography"
                name="biography"
                value={formData.biography}
                onChange={handleInputChange}
                rows={4}
                className="border-gray-600 bg-gray-700/50 text-white focus:border-green-500 focus:ring-green-500"
              />
            </div>

            <div className="col-span-2 space-y-2">
              <Label htmlFor="image" className="text-gray-300">
                Coach Photo
              </Label>
              <Input
                id="image"
                name="image"
                type="file"
                accept="image/*"
                onChange={handleInputChange}
                className="border-gray-600 bg-gray-700/50 text-white file:border-0 file:bg-gray-600 file:text-white focus:border-green-500"
              />
              {imageFile && (
                <p className="mt-1 text-sm text-gray-400">
                  Selected file: {imageFile.name}
                </p>
              )}
            </div>
          </div>

          <div className="mt-4 flex justify-end space-x-2">
            <Button
              type="button"
              variant="outline"
              onClick={resetForm}
              className="border-gray-600 text-gray-300 hover:bg-gray-700"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              className="bg-gradient-to-r from-green-500 to-blue-500 hover:from-green-600 hover:to-blue-600"
            >
              {isEditing ? 'Update Coach' : 'Create Coach'}
            </Button>
          </div>
        </motion.form>
      )}

      {isLoading ? (
        <div className="flex h-40 items-center justify-center">
          <div className="h-12 w-12 animate-spin rounded-full border-t-2 border-b-2 border-green-500"></div>
        </div>
      ) : (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.5 }}
        >
          <DataTable
            data={coaches}
            columns={columns}
            onEdit={handleEditClick}
            onDelete={handleDeleteCoach}
          />
        </motion.div>
      )}
    </div>
  );
};

export default CoachManagement;
