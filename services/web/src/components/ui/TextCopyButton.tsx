import { ClipboardIcon } from "@heroicons/react/24/outline";
import { useEffect, useRef, useState } from "react";

export default function TextCopyButton({
  text,
  title,
}: {
  text: string;
  title?: string;
}) {
  const [copied, setCopied] = useState(false);
  const timerIdRef = useRef<number | null>(null);
  useEffect(() => {
    if (copied) {
      timerIdRef.current = window.setTimeout(() => {
        setCopied(false);
      }, 2000);
      return () => {
        if (timerIdRef.current) window.clearTimeout(timerIdRef.current);
      };
    }
  }, [copied]);

  const handleCopyClick = () => {
    navigator.clipboard.writeText(text);
    setCopied(true);
  };

  return (
    <button type="button" onClick={handleCopyClick} className="ms-2">
      {copied ? (
        <span className="text-blue-500">Copied</span>
      ) : (
        <div className="flex items-center">
          <ClipboardIcon className="h-4 w-4 text-blue-500" />
          {title && <span className="ms-1">{title}</span>}
        </div>
      )}
    </button>
  );
}
