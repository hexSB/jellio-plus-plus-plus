import { FC, useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { FileText, X, Trash2 } from 'lucide-react';
import { getBaseUrl, getOrCreateDeviceId } from '@/lib/utils';
import axios from 'axios';

interface LogEntry {
  timestamp: string;
  message: string;
  level: 'Info' | 'Warning' | 'Error';
}

interface LogsViewerProps {
  accessToken?: string;
}

export const LogsViewer: FC<LogsViewerProps> = ({ accessToken }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(false);

  const fetchLogs = async () => {
    if (!accessToken) return;
    
    setLoading(true);
    try {
      const headers: Record<string, string> = {};
      const deviceId = getOrCreateDeviceId();
      headers['Authorization'] = `MediaBrowser Token="${accessToken}"`;
      headers['X-Emby-Token'] = accessToken;
      headers['X-Emby-Authorization'] = `MediaBrowser Client="Jellio", Device="Web", DeviceId="${deviceId}", Version="0.0.0", Token="${accessToken}"`;

      const response = await axios.get(`${getBaseUrl()}/logs?limit=100`, {
        headers,
        withCredentials: true,
      });

      setLogs(response.data.logs || []);
    } catch (error) {
      console.error('Failed to fetch logs:', error);
    } finally {
      setLoading(false);
    }
  };

  const clearLogs = async () => {
    if (!accessToken) return;
    
    try {
      const headers: Record<string, string> = {};
      const deviceId = getOrCreateDeviceId();
      headers['Authorization'] = `MediaBrowser Token="${accessToken}"`;
      headers['X-Emby-Token'] = accessToken;
      headers['X-Emby-Authorization'] = `MediaBrowser Client="Jellio", Device="Web", DeviceId="${deviceId}", Version="0.0.0", Token="${accessToken}"`;

      await axios.post(`${getBaseUrl()}/logs/clear`, null, {
        headers,
        withCredentials: true,
      });

      setLogs([]);
    } catch (error) {
      console.error('Failed to clear logs:', error);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchLogs();
    }
  }, [isOpen, accessToken]);

  useEffect(() => {
    if (isOpen && autoRefresh) {
      const interval = setInterval(fetchLogs, 2000); // Refresh every 2 seconds
      return () => clearInterval(interval);
    }
  }, [isOpen, autoRefresh, accessToken]);

  const getLevelColor = (level: string) => {
    switch (level) {
      case 'Error':
        return 'text-red-400';
      case 'Warning':
        return 'text-yellow-400';
      default:
        return 'text-gray-300';
    }
  };

  const formatTimestamp = (timestamp: string) => {
    try {
      const date = new Date(timestamp);
      return date.toLocaleString();
    } catch {
      return timestamp;
    }
  };

  if (!isOpen) {
    return (
      <Button
        type="button"
        variant="outline"
        onClick={() => setIsOpen(true)}
        className="w-full max-w-md"
      >
        <FileText className="mr-2 h-4 w-4" />
        View Logs
      </Button>
    );
  }

  return (
    <div className="w-full max-w-4xl mx-auto border rounded-lg p-4 bg-background">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold">Jellio Logs</h3>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setAutoRefresh(!autoRefresh)}
          >
            {autoRefresh ? 'Stop Auto-Refresh' : 'Start Auto-Refresh'}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={fetchLogs}
            disabled={loading}
          >
            {loading ? 'Loading...' : 'Refresh'}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={clearLogs}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setIsOpen(false)}
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      </div>
      <div className="h-96 w-full border rounded p-4 bg-black text-green-400 font-mono text-sm overflow-y-auto">
        {logs.length === 0 ? (
          <div className="text-center text-gray-500">No logs available</div>
        ) : (
          <div className="space-y-1">
            {logs.map((log, index) => (
              <div key={index} className="flex gap-2">
                <span className="text-gray-500 text-xs whitespace-nowrap">
                  {formatTimestamp(log.timestamp)}
                </span>
                <span className={`${getLevelColor(log.level)} whitespace-nowrap`}>
                  [{log.level}]
                </span>
                <span className="break-words">{log.message}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

