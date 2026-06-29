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
        <Label className="text-base">Transcoding Settings</Label>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={handleReset}
          className="text-xs"
        >
          Reset to Defaults
        </Button>
      </div>

      <FormField
        control={form.control}
        name="enableDirectStreaming"
        render={({ field }) => (
          <FormItem className="flex items-center justify-between py-2">
            <div className="flex-1">
              <FormLabel>Enable Direct Streaming</FormLabel>
              <FormDescription>
                Preserves original video quality when possible. Recommended for
                most users. When enabled, Jellyfin will copy compatible video
                codecs (AV1, HEVC, H.264) directly without re-encoding.
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
              <FormLabel>Force Video Transcoding</FormLabel>
              <FormDescription>
                Use for content that won't play directly. Overrides direct
                streaming and forces all video to be transcoded to H.264. Enable
                this if you experience playback issues with certain video files.
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
              <FormLabel>Force Audio Transcoding</FormLabel>
              <FormDescription>
                Use for content with audio issues. Transcodes all audio to AAC.
                Enable this if you experience audio playback problems or if your
                client doesn't support the source audio codec.
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
              <FormLabel>Max Video Bitrate: {field.value} Mbps</FormLabel>
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
              Higher values preserve more quality but require more bandwidth.
              Default is 120 Mbps. Reduce this if you have slow network
              connections.
            </FormDescription>
            <FormMessage />
          </FormItem>
        )}
      />
    </div>
  );
};
