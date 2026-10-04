import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import type { SurveyResponseProgressDto } from '../../types/surveyOperations';

type LiveStatus = 'idle' | 'connecting' | 'connected' | 'reconnecting' | 'unavailable';

interface UseSurveyResponseProgressOptions {
  accessToken: string | null;
  canConnect: boolean;
  surveyAssignmentId: string | null;
  onProgress: (progress: SurveyResponseProgressDto) => void;
  onReconnectRefresh: () => Promise<void>;
}

export function useSurveyResponseProgress({
  accessToken,
  canConnect,
  surveyAssignmentId,
  onProgress,
  onReconnectRefresh
}: UseSurveyResponseProgressOptions): LiveStatus {
  const [status, setStatus] = useState<LiveStatus>('idle');

  useEffect(() => {
    if (!accessToken || !canConnect || !surveyAssignmentId) {
      setStatus('idle');
      return;
    }

    let isMounted = true;
    let connection: signalR.HubConnection | null = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/survey-sessions', {
        accessTokenFactory: () => accessToken
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ResponseProgressUpdated', (progress: SurveyResponseProgressDto) => {
      if (!isMounted || progress.surveyAssignmentId !== surveyAssignmentId) {
        return;
      }

      onProgress(progress);
    });

    connection.onreconnecting(() => {
      if (isMounted) {
        setStatus('reconnecting');
      }
    });

    connection.onreconnected(() => {
      if (!isMounted || !connection) {
        return;
      }

      setStatus('connected');
      void connection
        .invoke('JoinAssignmentGroup', surveyAssignmentId)
        .then(() => onReconnectRefresh())
        .catch(() => {
          if (isMounted) setStatus('unavailable');
        });
    });

    connection.onclose(() => {
      if (isMounted) {
        setStatus('unavailable');
      }
    });

    async function startConnection() {
      if (!connection) {
        return;
      }

      setStatus('connecting');

      try {
        await connection.start();
        await connection.invoke('JoinAssignmentGroup', surveyAssignmentId);

        if (isMounted) {
          setStatus('connected');
        }
      } catch {
        if (isMounted) {
          setStatus('unavailable');
        }
      }
    }

    void startConnection();

    return () => {
      isMounted = false;
      const currentConnection = connection;
      connection = null;

      if (!currentConnection) {
        return;
      }

      currentConnection.off('ResponseProgressUpdated');
      void currentConnection
        .invoke('LeaveAssignmentGroup', surveyAssignmentId)
        .catch(() => undefined)
        .finally(() => {
          void currentConnection.stop();
        });
    };
  }, [accessToken, canConnect, onProgress, onReconnectRefresh, surveyAssignmentId]);

  return status;
}
