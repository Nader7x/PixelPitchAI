'use client';

import React, { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import DataTable from '../DataTable';
import stadiumService, {
  Stadium,
  CreateStadiumDto,
  UpdateStadiumDto,
} from '@/Services/StadiumService';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Textarea } from '@/components/ui/textarea';

const StadiumManagement = () => {
  const [stadiums, setStadiums] = useState<Stadium[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [selectedStadium, setSelectedStadium] = useState<Stadium | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [showForm, setShowForm] = useState(false);

  // Form state
  const [formData, setFormData] = useState<CreateStadiumDto | UpdateStadiumDto>(
    {
      name: '',
      capacity: undefined,
      city: '',
      country: '',
      surfaceType: '',
      address: '',
      latitude: undefined,
      longitude: undefined,
      description: '',
      facilities: '',
      builtDate: undefined,
      image: undefined,
    }
  );

  // File input state
  const [imageFile, setImageFile] = useState<File | null>(null);

  // Fetch stadiums on component mount
  useEffect(() => {
    fetchStadiums().then();
  }, []);

  const fetchStadiums = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const fetchedStadiums = await stadiumService.getStadiums();
      setStadiums(Array.isArray(fetchedStadiums) ? fetchedStadiums : []);
    } catch (err) {
      setError('Failed to fetch stadiums');
      console.error(err);
    } finally {
      setIsLoading(false);
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
          image: files[0],
        }));
      }
    } else if (type === 'number') {
      setFormData({
        ...formData,
        [name]: value === '' ? undefined : Number(value),
      });
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
    }
  };

  const handleCreateStadium = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await stadiumService.createStadium(formData as CreateStadiumDto);
      setSuccess('Stadium created successfully');
      resetForm();
      await fetchStadiums();
    } catch (err) {
      setError('Failed to create stadium');
      console.error(err);
    }
  };

  const handleUpdateStadium = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedStadium) return;

    setError(null);
    setSuccess(null);

    try {
      await stadiumService.updateStadium(selectedStadium.id, formData);
      setSuccess('Stadium updated successfully');
      resetForm();
      await fetchStadiums();
    } catch (err) {
      setError('Failed to update stadium');
      console.error(err);
    }
  };

  const handleDeleteStadium = async (stadium: Stadium) => {
    if (!window.confirm(`Are you sure you want to delete ${stadium.name}?`))
      return;

    setError(null);
    setSuccess(null);

    try {
      await stadiumService.deleteStadium(stadium.id);
      setSuccess('Stadium deleted successfully');
      await fetchStadiums();
    } catch (err) {
      setError('Failed to delete stadium');
      console.error(err);
    }
  };

  const handleEditClick = (stadium: Stadium) => {
    setSelectedStadium(stadium);
    setFormData({
      name: stadium.name,
      capacity: stadium.capacity,
      city: stadium.city || '',
      country: stadium.country || '',
      surfaceType: stadium.surfaceType || '',
      address: stadium.address || '',
      latitude: stadium.latitude,
      longitude: stadium.longitude,
      description: stadium.description || '',
      facilities: stadium.facilities || '',
      builtDate: stadium.builtDate || '',
    });
    setIsEditing(true);
    setShowForm(true);
  };

  const resetForm = () => {
    setFormData({
      name: '',
      capacity: undefined,
      city: '',
      country: '',
      surfaceType: '',
      address: '',
      latitude: undefined,
      longitude: undefined,
      description: '',
      facilities: '',
      builtDate: undefined,
      image: undefined,
    });
    setImageFile(null);
    setSelectedStadium(null);
    setIsEditing(false);
    setShowForm(false);
  };

  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'name', label: 'Name' },
    { key: 'capacity', label: 'Capacity' },
    { key: 'city', label: 'City' },
    { key: 'country', label: 'Country' },
    {
      key: 'builtDate',
      label: 'Built Date',
      render: (stadium: Stadium) =>
        stadium.builtDate
          ? new Date(stadium.builtDate).toLocaleDateString()
          : '-',
    },
    {
      key: 'image',
      label: 'Image',
      render: (stadium: Stadium) =>
        stadium.imageUrl ? (
          <img
            src={stadium.imageUrl}
            alt={`${stadium.name}`}
            className="h-10 w-10 object-contain"
          />
        ) : (
          'No image'
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
          Stadium Management
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
            {showForm ? 'Cancel' : 'Add New Stadium'}
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
              {isEditing ? 'Edit Stadium' : 'Create New Stadium'}
            </h3>
            <form
              onSubmit={isEditing ? handleUpdateStadium : handleCreateStadium}
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
                    Stadium Name *
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
                  <Label htmlFor="capacity" className="text-gray-300">
                    Capacity
                  </Label>
                  <Input
                    id="capacity"
                    name="capacity"
                    type="number"
                    value={formData.capacity || ''}
                    onChange={handleInputChange}
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
                  transition={{ duration: 0.3, delay: 0.3 }}
                  className="space-y-2"
                >
                  <Label htmlFor="surfaceType" className="text-gray-300">
                    Surface Type
                  </Label>
                  <Input
                    id="surfaceType"
                    name="surfaceType"
                    value={formData.surfaceType}
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
                  <Label htmlFor="builtDate" className="text-gray-300">
                    Built Date
                  </Label>
                  <Input
                    id="builtDate"
                    name="builtDate"
                    type="date"
                    value={formData.builtDate || ''}
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
                  <Label htmlFor="address" className="text-gray-300">
                    Address
                  </Label>
                  <Input
                    id="address"
                    name="address"
                    value={formData.address}
                    onChange={handleInputChange}
                    className="border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.45 }}
                  className="col-span-2 space-y-2"
                >
                  <Label htmlFor="image" className="text-gray-300">
                    Stadium Image
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

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.5 }}
                  className="col-span-2 space-y-2"
                >
                  <Label htmlFor="description" className="text-gray-300">
                    Description
                  </Label>
                  <Textarea
                    id="description"
                    name="description"
                    value={formData.description}
                    onChange={handleInputChange}
                    rows={3}
                    className="resize-none border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.55 }}
                  className="col-span-2 space-y-2"
                >
                  <Label htmlFor="facilities" className="text-gray-300">
                    Facilities
                  </Label>
                  <Textarea
                    id="facilities"
                    name="facilities"
                    value={formData.facilities}
                    onChange={handleInputChange}
                    rows={3}
                    className="resize-none border-gray-600 bg-gray-700/50 text-gray-200"
                  />
                </motion.div>
              </div>

              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ duration: 0.3, delay: 0.6 }}
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
                      Update Stadium
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
                      Create Stadium
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
        data={stadiums}
        isLoading={isLoading}
        pagination
        onEdit={handleEditClick}
        onDelete={handleDeleteStadium}
      />
    </div>
  );
};

export default StadiumManagement;
