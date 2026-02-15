import { useEffect, useState, useRef } from "react";
import { useDotNet } from "@/utils/useDotNet";
import { cn } from "@/lib/utils";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";

const MAX_LOGS = 100;

type LogLevel =
  | "trace"
  | "debug"
  | "information"
  | "warning"
  | "error"
  | "critical";

type LogItem = {
  timestamp: string;
  level: LogLevel;
  categoryName: string;
  message: string;
};

const levelColors: Record<LogLevel, string> = {
  trace: "text-gray-500",
  debug: "text-emerald-500",
  information: "text-sky-500",
  warning: "text-amber-500",
  error: "text-red-500",
  critical: "text-red-500",
};

export default function LogDisplayer() {
  const dotNetObj = useDotNet("LogDisplayer", "LogDisplayerController");
  const [logs, setLogs] = useState<LogItem[]>([]);
  const containerRef = useRef<HTMLDivElement>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!dotNetObj) {
      return;
    }

    const addLogItem = (logItem: LogItem) => {
      setLogs((prevItems) => {
        const updated = [...prevItems, logItem];
        return updated.length > MAX_LOGS ? updated.slice(-MAX_LOGS) : updated;
      });
    };

    dotNetObj.registerComponent({
      addLogItem,
    });
  }, [dotNetObj]);

  useEffect(() => {
    const container = containerRef.current;

    if (!container) return;

    const threshold = 100; // px from bottom considered "near bottom"
    const isNearBottom =
      container.scrollHeight - container.scrollTop - container.clientHeight <
      threshold;

    if (isNearBottom) {
      bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    }
  }, [logs]);

  return (
    <div
      className="bg-muted/30 h-full overflow-auto p-2 font-mono text-sm"
      ref={containerRef}
    >
      {logs.map((log, index) => (
        <div
          key={`${log.timestamp}-${index}`}
          className="hover:bg-muted/50 flex gap-3 rounded px-2 leading-6"
        >
          <span className="text-muted-foreground">{log.timestamp}</span>
          <span
            className={cn("min-w-20 font-semibold", levelColors[log.level])}
          >
            <Tooltip delayDuration={100} disableHoverableContent>
              <TooltipTrigger className="uppercase">{log.level}</TooltipTrigger>
              <TooltipContent>
                <p>{log.categoryName}</p>
              </TooltipContent>
            </Tooltip>
          </span>
          <span>{log.message}</span>
        </div>
      ))}
      <div ref={bottomRef} />
    </div>
  );
}
