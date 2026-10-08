export interface AuditEntryDto {
  id: string;
  occurredAtUtc: string;
  actorUserId: string | null;
  actorDisplayName: string | null;
  action: string;
  module: string;
  entityType: string;
  entityId: string | null;
  description: string;
  metadata: unknown | null;
}

export interface AuditEntriesPageDto {
  items: AuditEntryDto[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface AuditFilters {
  fromUtc?: string;
  toUtc?: string;
  actorUserId?: string;
  module?: string;
  action?: string;
  entityType?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
