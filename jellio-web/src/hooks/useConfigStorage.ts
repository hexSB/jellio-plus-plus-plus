import { useEffect } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import { z } from 'zod';
import type { ConfigFormType } from '@/components/configForm/formSchema';
import { getConfigFromServer } from '@/services/backendService';
import type { Library } from '@/types';

const STORAGE_KEY = 'jelliopp_config';

const storedConfigSchema = z.object({
  libraries: z
    .array(
      z.object({
        key: z.string(),
        name: z.string(),
        type: z.string(),
      }),
    )
    .optional(),
  jellyseerrEnabled: z.boolean().optional(),
  jellyseerrUrl: z.string().optional(),
  jellyseerrApiKey: z.string().optional(),
  defaultTags: z.string().optional(),
  publicBaseUrl: z.string().optional(),
  // Transcoding settings
  enableDirectStreaming: z.boolean().optional(),
  forceTranscodeVideo: z.boolean().optional(),
  forceTranscodeAudio: z.boolean().optional(),
  maxVideoBitrate: z.number().optional(),
});

type StoredConfig = z.infer<typeof storedConfigSchema>;

const stripTrailingSlash = (url: string) => url.replace(/\/+$/, '');

export const useConfigStorage = (
  form: UseFormReturn<ConfigFormType>,
  accessToken?: string,
  availableLibraries?: Library[],
) => {
  // Load config from localStorage and server on mount
  useEffect(() => {
    const loadConfig = async () => {
      try {
        // First try localStorage (faster)
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
          const result = storedConfigSchema.safeParse(JSON.parse(stored));
          if (result.success) {
            const config = result.data;
            if (config.libraries) {
              form.setValue('libraries', config.libraries);
            }
            if (config.jellyseerrEnabled !== undefined) {
              form.setValue('jellyseerrEnabled', config.jellyseerrEnabled);
            }
            if (config.jellyseerrUrl) {
              form.setValue('jellyseerrUrl', config.jellyseerrUrl);
            }
            if (config.jellyseerrApiKey) {
              form.setValue('jellyseerrApiKey', config.jellyseerrApiKey);
            }
            if (config.defaultTags !== undefined) {
              form.setValue('defaultTags', config.defaultTags);
            }
            if (config.publicBaseUrl) {
              form.setValue('publicBaseUrl', config.publicBaseUrl);
            }
            // Transcoding settings
            if (config.enableDirectStreaming !== undefined) {
              form.setValue(
                'enableDirectStreaming',
                config.enableDirectStreaming,
              );
            }
            if (config.forceTranscodeVideo !== undefined) {
              form.setValue('forceTranscodeVideo', config.forceTranscodeVideo);
            }
            if (config.forceTranscodeAudio !== undefined) {
              form.setValue('forceTranscodeAudio', config.forceTranscodeAudio);
            }
            if (config.maxVideoBitrate !== undefined) {
              form.setValue('maxVideoBitrate', config.maxVideoBitrate);
            }
          }
        }

        // Then try server config (may override localStorage)
        if (accessToken) {
          const serverConfig = await getConfigFromServer({
            token: accessToken,
          });
          if (serverConfig.jellyseerrEnabled !== undefined) {
            form.setValue('jellyseerrEnabled', serverConfig.jellyseerrEnabled);
          }
          if (serverConfig.jellyseerrUrl) {
            form.setValue('jellyseerrUrl', serverConfig.jellyseerrUrl);
          }
          if (serverConfig.jellyseerrApiKey) {
            form.setValue('jellyseerrApiKey', serverConfig.jellyseerrApiKey);
          }
          if (serverConfig.defaultTags !== undefined) {
            form.setValue('defaultTags', serverConfig.defaultTags);
          }
          if (serverConfig.publicBaseUrl) {
            form.setValue('publicBaseUrl', serverConfig.publicBaseUrl);
          }
          // Transcoding settings
          if (serverConfig.enableDirectStreaming !== undefined) {
            form.setValue(
              'enableDirectStreaming',
              serverConfig.enableDirectStreaming,
            );
          }
          if (serverConfig.forceTranscodeVideo !== undefined) {
            form.setValue(
              'forceTranscodeVideo',
              serverConfig.forceTranscodeVideo,
            );
          }
          if (serverConfig.forceTranscodeAudio !== undefined) {
            form.setValue(
              'forceTranscodeAudio',
              serverConfig.forceTranscodeAudio,
            );
          }
          if (serverConfig.maxVideoBitrate !== undefined) {
            form.setValue('maxVideoBitrate', serverConfig.maxVideoBitrate);
          }
          if (serverConfig.selectedLibraries && availableLibraries) {
            // Server stores library IDs as 32-char guids without dashes
            const selectedLibraries = serverConfig.selectedLibraries
              .map((id) => {
                const formattedId =
                  id.length === 32
                    ? `${id.slice(0, 8)}-${id.slice(8, 12)}-${id.slice(12, 16)}-${id.slice(16, 20)}-${id.slice(20, 32)}`
                    : id;
                return availableLibraries.find(
                  (lib) => lib.key === formattedId,
                );
              })
              .filter((lib): lib is Library => lib !== undefined);

            if (selectedLibraries.length > 0) {
              form.setValue('libraries', selectedLibraries);
            }
          }
        }
      } catch (error) {
        console.error('Failed to load config:', error);
      }
    };

    void loadConfig();
  }, [form, accessToken, availableLibraries]);

  // Save config to localStorage
  const saveConfig = () => {
    try {
      const values = form.getValues();
      const config: StoredConfig = {
        libraries: values.libraries,
        jellyseerrEnabled: values.jellyseerrEnabled,
        jellyseerrUrl: stripTrailingSlash(values.jellyseerrUrl ?? ''),
        jellyseerrApiKey: values.jellyseerrApiKey,
        defaultTags: values.defaultTags,
        publicBaseUrl: stripTrailingSlash(values.publicBaseUrl ?? ''),
        // Transcoding settings
        enableDirectStreaming: values.enableDirectStreaming,
        forceTranscodeVideo: values.forceTranscodeVideo,
        forceTranscodeAudio: values.forceTranscodeAudio,
        maxVideoBitrate: values.maxVideoBitrate,
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
