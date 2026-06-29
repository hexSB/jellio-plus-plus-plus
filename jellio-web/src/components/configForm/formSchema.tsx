import { z } from 'zod';

export const formSchema = z.object({
  serverName: z.string(),
  libraries: z.array(
    z.object({
      key: z.string(),
      name: z.string(),
      type: z.string(),
    }),
  ),
  jellyseerrEnabled: z.boolean().default(false),
  jellyseerrUrl: z.string().url().or(z.literal('')).default(''),
  jellyseerrApiKey: z.string().default(''),
  publicBaseUrl: z.string().url().or(z.literal('')).default(''),
  // Transcoding settings
  enableDirectStreaming: z.boolean().default(true),
  forceTranscodeVideo: z.boolean().default(false),
  forceTranscodeAudio: z.boolean().default(false),
  maxVideoBitrate: z.number().min(10).max(200).default(120),
});

export type ConfigFormType = z.input<typeof formSchema>;
