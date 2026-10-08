import { apiRequest } from './apiClient';
import type { CareerParticipationDashboardDto } from '../types/dashboard';

export function getCareerParticipationDashboard(
  careerId: string,
  academicCycleId: string,
  accessToken: string,
  onUnauthorized: () => void,
  signal?: AbortSignal
): Promise<CareerParticipationDashboardDto> {
  const searchParams = new URLSearchParams({
    academicCycleId
  });

  return apiRequest<CareerParticipationDashboardDto>(
    `/api/dashboard/careers/${encodeURIComponent(careerId)}?${searchParams.toString()}`,
    {
      token: accessToken,
      onUnauthorized,
      signal
    }
  );
}
