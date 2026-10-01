import { useEffect, useMemo, useState } from 'react';

interface PaginationResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalPages: number;
  totalItems: number;
  firstItem: number;
  lastItem: number;
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
}

interface PaginationControlsProps {
  firstItem: number;
  itemLabel: string;
  lastItem: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  page: number;
  pageSize: number;
  pageSizeOptions?: number[];
  totalItems: number;
  totalPages: number;
}

export function usePagination<T>(items: T[], initialPageSize = 8): PaginationResult<T> {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);
  const totalItems = items.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  useEffect(() => {
    setPage((current) => Math.min(current, totalPages));
  }, [totalPages]);

  useEffect(() => {
    setPage(1);
  }, [items, pageSize]);

  const paginatedItems = useMemo(() => {
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

  return {
    items: paginatedItems,
    page,
    pageSize,
    totalPages,
    totalItems,
    firstItem: totalItems === 0 ? 0 : (page - 1) * pageSize + 1,
    lastItem: Math.min(page * pageSize, totalItems),
    setPage,
    setPageSize
  };
}

export function PaginationControls({
  firstItem,
  itemLabel,
  lastItem,
  onPageChange,
  onPageSizeChange,
  page,
  pageSize,
  pageSizeOptions = [6, 8, 12, 24],
  totalItems,
  totalPages
}: PaginationControlsProps) {
  if (totalItems === 0) {
    return null;
  }

  return (
    <div className="pagination-bar" aria-label={`Paginación de ${itemLabel}`}>
      <p>
        Mostrando <strong>{firstItem}-{lastItem}</strong> de <strong>{totalItems}</strong> {itemLabel}
      </p>
      <div className="pagination-actions">
        <label>
          <span>Por página</span>
          <select
            className="text-input"
            onChange={(event) => onPageSizeChange(Number(event.target.value))}
            value={pageSize}
          >
            {pageSizeOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
        <button
          className="secondary-button"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          type="button"
        >
          Anterior
        </button>
        <span aria-live="polite">
          Página {page} de {totalPages}
        </span>
        <button
          className="secondary-button"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          type="button"
        >
          Siguiente
        </button>
      </div>
    </div>
  );
}
