import type { FC } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import type { ConfigFormType } from '@/components/configForm/formSchema.tsx';
import { Button } from '@/components/ui/button.tsx';
import { Checkbox } from '@/components/ui/checkbox.tsx';
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form.tsx';
import { Label } from '@/components/ui/label.tsx';

interface Props {
  form: UseFormReturn<ConfigFormType>;
}

const DEFAULT_VALUES = {
  enableDirectStreaming: true,
  forceTranscodeVideo: false,
  forceTranscodeAudio: false,
  maxVideoBitrate: 120,
};

export const TranscodingFieldset: FC<Props> = ({ form }) => {
  const enableDirectStreaming = form.watch('enableDirectStreaming');
  const forceTranscodeVideo = form.watch('forceTranscodeVideo');
  const forceTranscodeAudio = form.watch('forceTranscodeAudio');

  const playbackMode =
    forceTranscodeVideo || forceTranscodeAudio
      ? 'Forced Jellyfin transcoding'
      : enableDirectStreaming
        ? 'Adaptive: direct/remux first, Jellyfin fallback'
        : 'Jellyfin compatibility transcode';

  const playbackModeDescription =
    forceTranscodeVideo || forceTranscodeAudio
      ? 'Jellyfin will re-encode the selected media tracks even when direct streaming might work.'
      : enableDirectStreaming
        ? 'Jellyfin will copy/remux compatible tracks first, then transcode unsupported tracks when needed.'
        : 'Jellyfin will request H.264/AAC output instead of preserving original codecs.';

  const handleReset = () => {
    form.setValue(
      'enableDirectStreaming',
      DEFAULT_VALUES.enableDirectStreaming,
    );
    form.setValue('forceTranscodeVideo', DEFAULT_VALUES.forceTranscodeVideo);
    form.setValue('forceTranscodeAudio', DEFAULT_VALUES.forceTranscodeAudio);
    form.setValue('maxVideoBitrate', DEFAULT_VALUES.maxVideoBitrate);
  };

  return (
    <div className="rounded-lg border p-2 space-y-2">
      <div className="flex items-center justify-between">
        <Label className="text-base">Playback Strategy</Label>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={handleReset}
          className="text-xs"
        >
          Reset to Recommended
        </Button>
      </div>

      <div className="rounded-md border bg-muted/40 px-3 py-2 text-sm">
        <p className="font-medium">{playbackMode}</p>
        <p className="text-muted-foreground">{playbackModeDescription}</p>
      </div>

      <FormField
        control={form.control}
        name="enableDirectStreaming"
        render={({ field }) => (
          <FormItem className="flex items-center justify-between py-2">
            <div className="flex-1">
              <FormLabel>
                Prefer direct stream/remux with Jellyfin fallback
              </FormLabel>
              <FormDescription>
                This is not direct-only. Jellyfin copies compatible HEVC/H.264
                video and Opus/EAC3/AAC audio when possible, then transcodes
                unsupported tracks if playback needs it.
              </FormDescription>
            </div>
            <FormControl>
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={form.control}
        name="forceTranscodeVideo"
        render={({ field }) => (
          <FormItem className="flex items-center justify-between py-2">
            <div className="flex-1">
              <FormLabel>Always transcode video in Jellyfin</FormLabel>
              <FormDescription>
                Forces Jellyfin to re-encode video to H.264. Use this as a
                compatibility fallback, not when testing direct streaming
                quality.
              </FormDescription>
            </div>
            <FormControl>
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={form.control}
        name="forceTranscodeAudio"
        render={({ field }) => (
          <FormItem className="flex items-center justify-between py-2">
            <div className="flex-1">
              <FormLabel>Always transcode audio in Jellyfin</FormLabel>
              <FormDescription>
                Forces Jellyfin to re-encode audio to AAC. Use this if your
                device has audio issues with the original track.
              </FormDescription>
            </div>
            <FormControl>
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={form.control}
        name="maxVideoBitrate"
        render={({ field }) => (
          <FormItem className="py-2">
            <div className="flex items-center justify-between">
              <FormLabel>
                Jellyfin video bitrate ceiling: {field.value} Mbps
              </FormLabel>
            </div>
            <FormControl>
              <input
                type="range"
                min="10"
                max="200"
                step="10"
                className="w-full"
                value={field.value}
                onChange={(e) => field.onChange(parseInt(e.target.value))}
              />
            </FormControl>
            <FormDescription>
              A low ceiling can trigger Jellyfin transcoding even when direct
              streaming is preferred. Use a high value for quality testing, then
              lower it only if the network buffers.
            </FormDescription>
            <FormMessage />
          </FormItem>
        )}
      />
    </div>
  );
};
