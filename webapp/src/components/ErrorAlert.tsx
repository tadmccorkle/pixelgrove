import { useState, useEffect } from "react";
import { AlertCircle, X } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

interface ErrorAlertProps {
  message: string | null;
}

export function ErrorAlert({ message }: ErrorAlertProps) {
  const [isDismissed, setIsDismissed] = useState(false);

  useEffect(() => {
    setIsDismissed(false);
  }, [message]);

  if (!message) {
    return null;
  }

  if (isDismissed) {
    return (
      <button
        type="button"
        onClick={() => setIsDismissed(false)}
        className="fixed bottom-4 right-4 p-2 bg-card text-destructive rounded-full shadow-lg hover:opacity-80 transition-opacity cursor-pointer"
        aria-label="Show error details"
      >
        <AlertCircle className="size-5" />
      </button>
    );
  }

  return (
    <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 w-full max-w-md px-4">
      <Alert variant="destructive" className="relative shadow-lg">
        <AlertCircle className="size-4" />
        <AlertTitle>Error</AlertTitle>
        <AlertDescription>{message}</AlertDescription>
        <button
          type="button"
          onClick={() => setIsDismissed(true)}
          className="absolute top-3 right-3 text-destructive hover:opacity-70 transition-opacity cursor-pointer"
          aria-label="Dismiss error"
        >
          <X className="size-4" />
        </button>
      </Alert>
    </div>
  );
}
