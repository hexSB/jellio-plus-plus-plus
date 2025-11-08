import { useEffect } from 'react';
import type { UseFormReturn } from 'react-hook-form';

const STORAGE_KEY = 'jellio_config';

interface StoredConfig {
  jellyseerrEnabled?: boolean;
  jellyseerrUrl?: string;
  jellyseerrApiKey?: string;
  publicBaseUrl?: string;
}

export const useConfigStorage = (form: UseFormReturn<any>) => {
  // Load config from localStorage on mount
  useEffect(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const config: StoredConfig = JSON.parse(stored);
        
        // Update form with stored values
        if (config.jellyseerrEnabled !== undefined) {
          form.setValue('jellyseerrEnabled', config.jellyseerrEnabled);
        }
        if (config.jellyseerrUrl) {
          form.setValue('jellyseerrUrl', config.jellyseerrUrl);
        }
        if (config.jellyseerrApiKey) {
          form.setValue('jellyseerrApiKey', config.jellyseerrApiKey);
        }
        if (config.publicBaseUrl) {
          form.setValue('publicBaseUrl', config.publicBaseUrl);
        }
      }
    } catch (error) {
      console.error('Failed to load config from localStorage:', error);
    }
  }, [form]);

  // Save config to localStorage
  const saveConfig = () => {
    try {
      const values = form.getValues();
      
      // Strip trailing slashes from URLs before saving
      const stripTrailingSlash = (url: string) => url?.replace(/\/+$/, '') || '';
      
      const config: StoredConfig = {
        jellyseerrEnabled: values.jellyseerrEnabled,
        jellyseerrUrl: stripTrailingSlash(values.jellyseerrUrl),
        jellyseerrApiKey: values.jellyseerrApiKey,
        publicBaseUrl: stripTrailingSlash(values.publicBaseUrl),
      };
      
      localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
      return true;
    } catch (error) {
      console.error('Failed to save config to localStorage:', error);
      return false;
    }
  };

  return { saveConfig };
};
