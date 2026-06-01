import React from 'react';
import { Button } from './button';
import { ChevronLeft, ChevronRight, MoreHorizontal } from 'lucide-react';

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
  showPageSizeSelector?: boolean;
  pageSizeOptions?: number[];
}

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  showPageSizeSelector = true,
  pageSizeOptions = [10, 25, 50, 100],
}) => {
  // Debug logging for pagination component
  React.useEffect(() => {
    console.log('🎯 PAGINATION COMPONENT PROPS:', {
      currentPage,
      totalPages,
      pageSize,
      totalCount,
      hasOnPageChange: typeof onPageChange === 'function',
      isPaginationEnabled: totalPages > 1,
      timestamp: new Date().toLocaleTimeString(),
    });
  }, [currentPage, totalPages, pageSize, totalCount, onPageChange]);

  const handlePageClick = (page: number) => {
    console.log('🎯 PAGINATION: Page button clicked:', page);
    console.log('🎯 PAGINATION: Calling onPageChange with:', page);
    onPageChange(page);
  };

  const getVisiblePages = () => {
    if (totalPages <= 1) {
      return totalPages === 1 ? [1] : [];
    }

    const delta = 2;
    const range = [];
    const rangeWithDots = [];

    for (
      let i = Math.max(2, currentPage - delta);
      i <= Math.min(totalPages - 1, currentPage + delta);
      i++
    ) {
      range.push(i);
    }

    if (currentPage - delta > 2) {
      rangeWithDots.push(1, '...');
    } else {
      rangeWithDots.push(1);
    }

    rangeWithDots.push(...range);

    if (currentPage + delta < totalPages - 1) {
      rangeWithDots.push('...', totalPages);
    } else {
      if (totalPages > 1) {
        rangeWithDots.push(totalPages);
      }
    }

    return rangeWithDots.filter(
      (item, index, arr) => arr.indexOf(item) === index
    );
  };

  const visiblePages = totalPages > 1 ? getVisiblePages() : [];

  const startItem = (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalCount);

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      {/* Results info */}
      <div className="text-sm text-gray-400">
        Showing {startItem} to {endItem} of {totalCount} results
      </div>

      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-4">
        {/* Page size selector */}
        {showPageSizeSelector && onPageSizeChange && (
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-400">Rows per page:</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="rounded-md border border-gray-600 bg-gray-700 px-2 py-1 text-sm text-gray-200 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none"
            >
              {pageSizeOptions.map((option) => (
                <option key={option} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </div>
        )}

        {/* Pagination controls */}
        {totalPages > 1 && (
          <div className="flex items-center gap-1">
            {' '}
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageClick(currentPage - 1)}
              disabled={currentPage <= 1}
              className="border-gray-600 text-gray-300 hover:bg-gray-700"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            {visiblePages.map((page, index) => (
              <React.Fragment key={index}>
                {page === '...' ? (
                  <Button
                    variant="ghost"
                    size="sm"
                    disabled
                    className="text-gray-400"
                  >
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                ) : (
                  <Button
                    variant={page === currentPage ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => handlePageClick(page as number)}
                    className={
                      page === currentPage
                        ? 'bg-blue-600 text-white hover:bg-blue-700'
                        : 'border-gray-600 text-gray-300 hover:bg-gray-700'
                    }
                  >
                    {page}
                  </Button>
                )}
              </React.Fragment>
            ))}
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageClick(currentPage + 1)}
              disabled={currentPage >= totalPages}
              className="border-gray-600 text-gray-300 hover:bg-gray-700"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        )}
      </div>
    </div>
  );
};

export default Pagination;
