import { apiRequest } from './apiClient';
import type { AuditEntriesPageDto, AuditFilters } from '../types/audit';

export function getAuditEntries(
  accessToken: string,
  onUnauthorized: () => void,
  filters: AuditFilters,
  signal?: AbortSignal
): Promise<AuditEntriesPageDto> {
  const searchParams = new URLSearchParams();

  appendOptionalFilter(searchParams, 'fromUtc', filters.fromUtc);
  appendOptionalFilter(searchParams, 'toUtc', filters.toUtc);
  appendOptionalFilter(searchParams, 'actorUserId', filters.actorUserId);
  appendOptionalFilter(searchParams, 'module', filters.module);
  appendOptionalFilter(searchParams, 'action', filters.action);
  appendOptionalFilter(searchParams, 'entityType', filters.entityType);
  appendOptionalFilter(searchParams, 'search', filters.search);

  searchParams.set('page', String(filters.page ?? 1));
  searchParams.set('pageSize', String(filters.pageSize ?? 25));

  return apiRequest<AuditEntriesPageDto>(`/api/audit?${searchParams.toString()}`, {
    token: accessToken,
    onUnauthorized,
    signal
  });
}

function appendOptionalFilter(searchParams: URLSearchParams, key: string, value?: string) {
  if (value?.trim()) {
    searchParams.set(key, value.trim());
  }
}
