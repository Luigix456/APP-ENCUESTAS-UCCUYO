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
  showPageSize?: boolean;
  showSummary?: boolean;
}

type PageToken = number | 'ellipsis-left' | 'ellipsis-right';

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
  totalPages,
  showPageSize = true,
  showSummary = true
}: PaginationControlsProps) {
  if (totalItems === 0 || totalPages <= 1 && !showSummary && !showPageSize) {
    return null;
  }

  const pageTokens = buildPageTokens(page, totalPages);

  return (
    <nav className="pagination-bar" aria-label={`Paginación de ${itemLabel}`}>
      {(showSummary || showPageSize) ? (
        <div className="pagination-meta">
          {showSummary ? (
            <p>
              Mostrando <strong>{firstItem}-{lastItem}</strong> de <strong>{totalItems}</strong> {itemLabel}
            </p>
          ) : null}
          {showPageSize ? (
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
          ) : null}
        </div>
      ) : null}

      <div className="pagination-pages" aria-live="polite">
        <button
          aria-label="Página anterior"
          className="pagination-page-button pagination-page-button--arrow"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          type="button"
        >
          «
        </button>

        {pageTokens.map((token) => {
          if (typeof token !== 'number') {
            return <span aria-hidden="true" className="pagination-ellipsis" key={token}>…</span>;
          }

          const isCurrent = token === page;
          return (
            <button
              aria-current={isCurrent ? 'page' : undefined}
              aria-label={`Ir a la página ${token}`}
              className={`pagination-page-button ${isCurrent ? 'pagination-page-button--active' : ''}`}
              key={token}
              onClick={() => onPageChange(token)}
              type="button"
            >
              {token}
            </button>
          );
        })}

        <button
          aria-label="Página siguiente"
          className="pagination-page-button pagination-page-button--arrow"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          type="button"
        >
          »
        </button>
      </div>
    </nav>
  );
}

function buildPageTokens(current: number, total: number): PageToken[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, index) => index + 1);
  }

  const pages = new Set<number>([1, 2, total - 1, total, current - 1, current, current + 1]);
  const validPages = [...pages]
    .filter((value) => value >= 1 && value <= total)
    .sort((left, right) => left - right);

  const tokens: PageToken[] = [];
  let ellipsisCount = 0;

  validPages.forEach((value, index) => {
    const previous = validPages[index - 1];
    if (previous) {
      const gap = value - previous;
      if (gap === 2) {
        tokens.push(previous + 1);
      } else if (gap > 2) {
        tokens.push(ellipsisCount++ === 0 ? 'ellipsis-left' : 'ellipsis-right');
      }
    }
    tokens.push(value);
  });

  return tokens;
}
