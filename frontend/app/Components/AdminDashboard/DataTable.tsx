'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { motion } from 'framer-motion';

interface DataTableProps {
  data: any[];
  columns: {
    key: string;
    label: string;
    render?: (item: any) => React.ReactNode;
  }[];
  onEdit: (item: any) => void;
  onDelete: (item: any) => void;
  idField?: string;
  isLoading?: boolean;
  pagination?: boolean;
}

const DataTable: React.FC<DataTableProps> = ({
  data,
  columns,
  onEdit,
  onDelete,
  idField = 'id',
  isLoading = false,
  pagination = false,
}) => {
  // State for hover effects
  const [hoveredRow, setHoveredRow] = useState<number | null>(null);

  // Ensure data is an array before attempting to render
  const safeData = Array.isArray(data) ? data : [];

  if (isLoading) {
    return (
      <div className="flex h-40 items-center justify-center">
        <div className="h-12 w-12 animate-spin rounded-full border-t-2 border-b-2 border-green-500"></div>
      </div>
    );
  }

  if (!safeData || safeData.length === 0) {
    return (
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 0.5 }}
        className="my-4 rounded-xl border border-gray-600 bg-gray-700/30 p-8 text-center backdrop-blur-sm"
      >
        <svg
          className="mx-auto mb-4 h-16 w-16 text-gray-400"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={1.5}
            d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4"
          />
        </svg>
        <p className="text-lg text-gray-300">No data available</p>
      </motion.div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      className="overflow-x-auto rounded-lg border border-gray-700 bg-gray-800/70 shadow-lg backdrop-blur-sm"
    >
      <table className="min-w-full divide-y divide-gray-700">
        <thead className="bg-gray-700/50">
          <tr key="header-row">
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className="px-6 py-4 text-left text-xs font-semibold tracking-wider text-gray-300 uppercase"
              >
                {column.label}
              </th>
            ))}
            <th
              scope="col"
              className="px-6 py-4 text-right text-xs font-semibold tracking-wider text-gray-300 uppercase"
            >
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-700/50">
          {safeData.map((item, index) => (
            <motion.tr
              key={item[idField]}
              initial={{ opacity: 0, y: 5 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{
                duration: 0.3,
                delay: index * 0.05,
                ease: 'easeOut',
              }}
              onMouseEnter={() => setHoveredRow(index)}
              onMouseLeave={() => setHoveredRow(null)}
              className={`${hoveredRow === index ? 'bg-gray-700/50' : 'bg-gray-800/30'} transition-all duration-150`}
            >
              {columns.map((column) => (
                <td
                  key={`${item[idField]}-${column.key}`}
                  className="px-6 py-4 whitespace-nowrap text-gray-200"
                >
                  {column.render ? column.render(item) : item[column.key]}
                </td>
              ))}
              <td className="space-x-2 px-6 py-4 text-right whitespace-nowrap">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onEdit(item)}
                  className="border-green-700 text-green-400 transition-all duration-300 hover:bg-green-700/20"
                >
                  <svg
                    className="mr-1 h-4 w-4"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={1.5}
                      d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                    />
                  </svg>
                  Edit
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => onDelete(item)}
                  className="bg-red-900/40 text-red-300 transition-all duration-300 hover:bg-red-800/60"
                >
                  <svg
                    className="mr-1 h-4 w-4"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={1.5}
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                  Delete
                </Button>
              </td>
            </motion.tr>
          ))}
        </tbody>
      </table>
    </motion.div>
  );
};

export default DataTable;
