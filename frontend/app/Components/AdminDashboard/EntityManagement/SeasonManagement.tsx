'use client';

import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import DataTable from '../DataTable';
import seasonService, {
  Season,
  CreateSeasonDto,
  UpdateSeasonDto,
  SeasonFilter,
} from '@/Services/SeasonService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Switch } from '@/components/ui/switch';

const SeasonManagement = () => {
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedSeason, setSelectedSeason] = useState<Season | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [showForm, setShowForm] = useState(false);

  // Filter state
  const [filter, setFilter] = useState<SeasonFilter>({});

  // Form state
  const [formData, setFormData] = useState<CreateSeasonDto | UpdateSeasonDto>({
    name: '',
    leagueName: '',
    country: '',
    startDate: '',
    endDate: '',
    isActive: false,
  });

  // Fetch seasons on component mount
  useEffect(() => {
    fetchSeasons().then();
  }, []);

  const fetchSeasons = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const fetchedSeasons = await seasonService.getSeasons(filter);
      setSeasons(fetchedSeasons);
    } catch (err) {
      setError('Failed to fetch seasons');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;

    if (type === 'checkbox') {
      setFormData({
        ...formData,
        [name]: checked,
      });
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
    }
  };

  const handleSwitchChange = (checked: boolean) => {
    setFormData({
      ...formData,
      isActive: checked,
    });
  };

  const handleFilterChange = (name: string, value: any) => {
    setFilter({
      ...filter,
      [name]: value,
    });
  };

  const handleCreateSeason = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await seasonService.createSeason(formData as CreateSeasonDto);
      setSuccess('Season created successfully');
      resetForm();
      await fetchSeasons();
    } catch (err) {
      setError('Failed to create season');
      console.error(err);
    }
  };

  const handleUpdateSeason = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSeason) return;

    setError(null);
    setSuccess(null);

    try {
      await seasonService.updateSeason(selectedSeason.id, formData);
      setSuccess('Season updated successfully');
      resetForm();
      await fetchSeasons();
    } catch (err) {
      setError('Failed to update season');
      console.error(err);
    }
  };

  const handleDeleteSeason = async (season: Season) => {
    if (!window.confirm(`Are you sure you want to delete ${season.name}?`))
      return;

    setError(null);
    setSuccess(null);

    try {
      await seasonService.deleteSeason(season.id);
      setSuccess('Season deleted successfully');
      await fetchSeasons();
    } catch (err) {
      setError('Failed to delete season');
      console.error(err);
    }
  };

  const handleEditClick = (season: Season) => {
    setSelectedSeason(season);
    setFormData({
      name: season.name,
      leagueName: season.leagueName || '',
      country: season.country || '',
      startDate: season.startDate
        ? new Date(season.startDate).toISOString().split('T')[0]
        : '',
      endDate: season.endDate
        ? new Date(season.endDate).toISOString().split('T')[0]
        : '',
      isActive: season.isActive || false,
    });
    setIsEditing(true);
    setShowForm(true);
  };

  const resetForm = () => {
    setFormData({
      name: '',
      leagueName: '',
      country: '',
      startDate: '',
      endDate: '',
      isActive: false,
    });
    setSelectedSeason(null);
    setIsEditing(false);
    setShowForm(false);
  };

  const applyFilters = () => {
    fetchSeasons().then();
  };

  const resetFilters = () => {
    setFilter({});
    fetchSeasons().then();
  };

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'name', label: 'Name' },
    { key: 'leagueName', label: 'League' },
    { key: 'country', label: 'Country' },
    {
      key: 'startDate',
      label: 'Start Date',
      render: (season: Season) =>
        season.startDate
          ? new Date(season.startDate).toLocaleDateString()
          : '-',
    },
    {
      key: 'endDate',
      label: 'End Date',
      render: (season: Season) =>
        season.endDate ? new Date(season.endDate).toLocaleDateString() : '-',
    },
    {
      key: 'isActive',
      label: 'Active',
      render: (season: Season) => (
        <span
          className={`rounded-full px-2 py-1 text-xs ${season.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}
        >
          {season.isActive ? 'Yes' : 'No'}
        </span>
      ),
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
          Season Management
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
            {showForm ? 'Cancel' : 'Add New Season'}
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

      {/* Filters */}
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: 0.1 }}
        className="rounded-xl border border-gray-700 bg-gray-800/50 p-6 shadow-lg backdrop-blur-sm"
      >
        <h3 className="mb-4 text-lg font-medium text-gray-200">
          Filter Seasons
        </h3>
        <div className="mb-6 grid grid-cols-1 gap-6 md:grid-cols-3">
          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.15 }}
            className="space-y-2"
          >
            <Label htmlFor="filter-league" className="text-gray-300">
              League Name
            </Label>
            <Input
              id="filter-league"
              value={filter.leagueName || ''}
              onChange={(e) => handleFilterChange('leagueName', e.target.value)}
              placeholder="Filter by league"
              className="border-gray-600 bg-gray-700/50 text-gray-200"
            />
          </motion.div>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.2 }}
            className="space-y-2"
          >
            <Label htmlFor="filter-country" className="text-gray-300">
              Country
            </Label>
            <Input
              id="filter-country"
              value={filter.country || ''}
              onChange={(e) => handleFilterChange('country', e.target.value)}
              placeholder="Filter by country"
              className="border-gray-600 bg-gray-700/50 text-gray-200"
            />
          </motion.div>

          <motion.div
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: 0.25 }}
            className="flex items-center space-x-3 pt-6"
          >
            <Switch
              checked={filter.isActive === true}
              onCheckedChange={(checked) =>
                handleFilterChange('isActive', checked)
              }
              id="filter-active"
              className="data-[state=checked]:bg-green-500"
            />
            <Label htmlFor="filter-active" className="text-gray-300">
              Show only active seasons
            </Label>
          </motion.div>
        </div>

        <div className="flex space-x-3">
          <Button
            onClick={applyFilters}
            className="bg-blue-600 transition-all duration-300 hover:bg-blue-500"
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
                d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
              />
            </svg>
            Apply Filters
          </Button>
          <Button
            variant="outline"
            onClick={resetFilters}
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
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
              />
            </svg>
            Reset Filters
          </Button>
        </div>
      </motion.div>

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
              {isEditing ? 'Edit Season' : 'Create New Season'}
            </h3>
            <form
              onSubmit={isEditing ? handleUpdateSeason : handleCreateSeason}
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
                    Season Name *
                  </Label>
                  <Input
                    id="name"
                    name="name"
                    value={formData.name}
                    onChange={handleInputChange}
                    required
                    placeholder="e.g., 2023-2024"
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.15 }}
                  className="space-y-2"
                >
                  <Label htmlFor="leagueName" className="text-gray-300">
                    League Name
                  </Label>
                  <Input
                    id="leagueName"
                    name="leagueName"
                    value={formData.leagueName}
                    onChange={handleInputChange}
                    placeholder="e.g., Premier League"
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
                    placeholder="e.g., England"
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.25 }}
                  className="space-y-2"
                >
                  <Label htmlFor="startDate" className="text-gray-300">
                    Start Date
                  </Label>
                  <Input
                    id="startDate"
                    name="startDate"
                    type="date"
                    value={formData.startDate}
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
                  <Label htmlFor="endDate" className="text-gray-300">
                    End Date
                  </Label>
                  <Input
                    id="endDate"
                    name="endDate"
                    type="date"
                    value={formData.endDate}
                    onChange={handleInputChange}
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.35 }}
                  className="flex items-center space-x-3 pt-6"
                >
                  <Switch
                    checked={formData.isActive === true}
                    onCheckedChange={handleSwitchChange}
                    id="isActive"
                    className="data-[state=checked]:bg-green-500"
                  />
                  <Label htmlFor="isActive" className="text-gray-300">
                    Active Season
                  </Label>
                </motion.div>
              </div>

              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 0.3, delay: 0.4 }}
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
                      Update Season
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
                      Create Season
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
        data={seasons}
        isLoading={isLoading}
        pagination
        onEdit={handleEditClick}
        onDelete={handleDeleteSeason}
      />
    </div>
  );
};

export default SeasonManagement;
