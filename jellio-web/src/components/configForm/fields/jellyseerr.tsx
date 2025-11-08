import type { FC } from 'react';
import { UseFormReturn } from 'react-hook-form';
import { Input } from '@/components/ui/input.tsx';
import { Checkbox } from '@/components/ui/checkbox.tsx';
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form.tsx';

interface Props { form: UseFormReturn<any>; }

export const JellyseerrFieldset: FC<Props> = ({ form }) => {
  const enabled = form.watch('jellyseerrEnabled');
  return (
    <fieldset className="rounded-lg border p-2">
      <legend className="px-1 text-base font-medium">Jellyseerr integration</legend>
      <FormField
        control={form.control}
        name="jellyseerrEnabled"
        render={({ field }) => (
          <FormItem className="flex items-center justify-between py-2">
            <div>
              <FormLabel>Enable Jellyseerr</FormLabel>
              <FormDescription>
                Show a "Request via Jellyseerr" option when no stream is available.
              </FormDescription>
            </div>
            <FormControl>
              <Checkbox checked={field.value} onCheckedChange={field.onChange} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      {enabled && (
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          <FormField
            control={form.control}
            name="jellyseerrUrl"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Jellyseerr Base URL</FormLabel>
                <FormControl>
                  <Input placeholder="https://jellyseerr.example.com" {...field} />
                </FormControl>
                <FormDescription className="text-xs">
                  <strong>Use your public/Cloudflare Tunnel URL</strong> (accessible from anywhere).
                  Do NOT include a trailing slash. Example: <code className="text-xs bg-muted px-1 rounded">https://jellyseerr.example.com</code>
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="jellyseerrApiKey"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Jellyseerr API Key</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="paste API key" {...field} />
                </FormControl>
                <FormDescription>Required for automatic API requests.</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="publicBaseUrl"
            render={({ field }) => (
              <FormItem className="md:col-span-2">
                <FormLabel>Public Base URL (optional)</FormLabel>
                <FormControl>
                  <Input placeholder="https://jellyfin.example.com" {...field} />
                </FormControl>
                <FormDescription className="text-xs">
                  <strong>Use your Cloudflare Tunnel or public URL</strong> for Jellyfin if different from where you're accessing this page.
                  Do NOT include a trailing slash. Example: <code className="text-xs bg-muted px-1 rounded">https://jellyfin.example.com</code>
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>
      )}
    </fieldset>
  );
};
