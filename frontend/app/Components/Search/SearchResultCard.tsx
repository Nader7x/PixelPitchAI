'use client';
import React from 'react';
import Image from 'next/image';
import { motion } from 'framer-motion';
import {
  Star,
  User,
  Users,
  Home,
  Calendar,
  Package,
  Trophy,
  MapPin,
  Clock,
} from 'lucide-react';
import { SearchResultItem } from '@/Services/SearchService';

interface SearchResultCardProps {
  result: SearchResultItem;
  index?: number;
  showMetadata?: boolean;
  showDescription?: boolean;
  className?: string;
  onClick?: () => void;
}

export default function SearchResultCard({
  result,
  index = 0,
  showMetadata = true,
  showDescription = true,
  className = '',
  onClick,
}: SearchResultCardProps) {
  const getTypeIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'player':
        return <User className="h-5 w-5" />;
      case 'coach':
        return <Users className="h-5 w-5" />;
      case 'team':
        return <Trophy className="h-5 w-5" />;
      case 'stadium':
        return <Home className="h-5 w-5" />;
      case 'match':
        return <Calendar className="h-5 w-5" />;
      default:
        return <Package className="h-5 w-5" />;
    }
  };

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'player':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'coach':
        return 'bg-green-100 text-green-800 border-green-200';
      case 'team':
        return 'bg-purple-100 text-purple-800 border-purple-200';
      case 'stadium':
        return 'bg-orange-100 text-orange-800 border-orange-200';
      case 'match':
        return 'bg-indigo-100 text-indigo-800 border-indigo-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const formatMetadata = (type: string, additionalData: any) => {
    switch (type.toLowerCase()) {
      case 'player':
        return [
          additionalData.position && `Position: ${additionalData.position}`,
          additionalData.age && `Age: ${additionalData.age}`,
          additionalData.team && `Team: ${additionalData.team}`,
          additionalData.nationality && `${additionalData.nationality}`,
        ].filter(Boolean);

      case 'coach':
        return [
          additionalData.experience && `${additionalData.experience} years exp`,
          additionalData.trophies && `${additionalData.trophies} trophies`,
          additionalData.nationality && additionalData.nationality,
          additionalData.currentTeam &&
            `Current: ${additionalData.currentTeam}`,
        ].filter(Boolean);

      case 'team':
        return [
          additionalData.league && additionalData.league,
          additionalData.founded && `Founded: ${additionalData.founded}`,
          additionalData.stadium && `Stadium: ${additionalData.stadium}`,
          additionalData.country && additionalData.country,
        ].filter(Boolean);

      case 'stadium':
        return [
          additionalData.capacity &&
            `Capacity: ${additionalData.capacity.toLocaleString()}`,
          additionalData.location && additionalData.location,
          additionalData.surface && `Surface: ${additionalData.surface}`,
          additionalData.opened && `Opened: ${additionalData.opened}`,
        ].filter(Boolean);

      case 'match':
        return [
          additionalData.competition && additionalData.competition,
          additionalData.date && `Date: ${additionalData.date}`,
          additionalData.venue && `Venue: ${additionalData.venue}`,
          additionalData.status && `Status: ${additionalData.status}`,
        ].filter(Boolean);

      default:
        return [
          additionalData.category && `Category: ${additionalData.category}`,
          additionalData.price && `Price: $${additionalData.price}`,
          additionalData.brand && `Brand: ${additionalData.brand}`,
          additionalData.rating && `Rating: ${additionalData.rating}/5`,
        ].filter(Boolean);
    }
  };

  const metadata = formatMetadata(result.type, result.additionalData);
  const cardContent = (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.05 }}
      className={`relative overflow-hidden rounded-xl border border-gray-200/50 bg-white/80 shadow-lg backdrop-blur-sm ${className}`}
      onClick={onClick}
    >
      <div className="relative p-6">
        <div className="flex items-start space-x-4">
          {/* Thumbnail/Image */}
          <div className="flex-shrink-0">
            {result.thumbnailUrl ? (
              <div className="relative h-16 w-16 overflow-hidden rounded-lg">
                <Image
                  src={result.thumbnailUrl}
                  alt={result.name}
                  fill
                  className="object-cover"
                />
              </div>
            ) : (
              <div className="flex h-16 w-16 items-center justify-center rounded-lg bg-gradient-to-br from-gray-100 to-gray-200 text-gray-600">
                {getTypeIcon(result.type)}
              </div>
            )}
          </div>

          {/* Content */}
          <div className="min-w-0 flex-1">
            {/* Header with type badge */}
            <div className="mb-2 flex items-center justify-between">
              <div className="flex items-center space-x-2">
                <span
                  className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${getTypeColor(
                    result.type
                  )}`}
                >
                  {getTypeIcon(result.type)}
                  <span className="ml-1 capitalize">{result.type}</span>
                </span>
              </div>
            </div>

            {/* Title */}
            <h3 className="mb-2 line-clamp-1 text-lg font-semibold text-gray-900">
              {result.name}
            </h3>

            {/* Description */}
            {showDescription && result.description && (
              <p className="mb-3 line-clamp-2 text-sm leading-relaxed text-gray-600">
                {result.description}
              </p>
            )}

            {/* Metadata */}
            {showMetadata && metadata.length > 0 && (
              <div className="space-y-1">
                {metadata.slice(0, 3).map((item, idx) => (
                  <div
                    key={idx}
                    className="flex items-center text-xs text-gray-500"
                  >
                    <div className="mr-2 h-1 w-1 rounded-full bg-gray-300"></div>
                    <span>{item}</span>
                  </div>
                ))}
              </div>
            )}

            {/* Additional metadata for specific types */}
            {result.additionalData.rating && (
              <div className="mt-2 flex items-center space-x-1">
                <Star className="h-4 w-4 fill-current text-yellow-500" />
                <span className="text-sm font-medium text-gray-700">
                  {result.additionalData.rating}
                </span>
              </div>
            )}

            {/* Location for stadium/team */}
            {(result.type === 'Stadium' || result.type === 'Team') &&
              result.additionalData.location && (
                <div className="mt-2 flex items-center space-x-1 text-xs text-gray-500">
                  <MapPin className="h-3 w-3" />
                  <span>{result.additionalData.location}</span>
                </div>
              )}

            {/* Match specific information */}
            {result.type === 'Match' && result.additionalData.kickoffTime && (
              <div className="mt-2 flex items-center space-x-1 text-xs text-gray-500">
                <Clock className="h-3 w-3" />
                <span>{result.additionalData.kickoffTime}</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </motion.div>
  );

  // Always return the card content without Link wrapper
  return cardContent;
}
