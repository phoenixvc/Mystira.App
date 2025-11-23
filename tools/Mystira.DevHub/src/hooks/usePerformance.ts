import { useEffect, useRef } from 'react';

interface PerformanceMetrics {
  componentName: string;
  renderTime: number;
  renderCount: number;
}

/**
 * Hook to monitor component render performance
 * Logs render times and counts in development
 */
export function usePerformance(componentName: string, enabled = process.env.NODE_ENV === 'development') {
  const renderCount = useRef(0);
  const startTime = useRef(performance.now());

  useEffect(() => {
    if (!enabled) return;

    renderCount.current += 1;
    const endTime = performance.now();
    const renderTime = endTime - startTime.current;

    const metrics: PerformanceMetrics = {
      componentName,
      renderTime,
      renderCount: renderCount.current,
    };

    console.log(
      `[Performance] ${componentName} - Render #${renderCount.current} - ${renderTime.toFixed(2)}ms`
    );

    // Log slow renders
    if (renderTime > 16) {
      console.warn(
        `[Performance Warning] ${componentName} took ${renderTime.toFixed(2)}ms (>16ms frame budget)`
      );
    }

    startTime.current = performance.now();
  });

  return renderCount.current;
}

/**
 * Hook to measure async operation duration
 */
export function useAsyncPerformance() {
  const measure = async <T,>(
    operationName: string,
    operation: () => Promise<T>
  ): Promise<T> => {
    const startTime = performance.now();

    try {
      const result = await operation();
      const duration = performance.now() - startTime;

      console.log(`[Async Performance] ${operationName} - ${duration.toFixed(2)}ms`);

      if (duration > 1000) {
        console.warn(
          `[Async Performance Warning] ${operationName} took ${duration.toFixed(2)}ms (>1s)`
        );
      }

      return result;
    } catch (error) {
      const duration = performance.now() - startTime;
      console.error(`[Async Performance] ${operationName} failed after ${duration.toFixed(2)}ms`, error);
      throw error;
    }
  };

  return { measure };
}

/**
 * Hook to detect slow renders and memory leaks
 */
export function useRenderMonitor(componentName: string, threshold = 16) {
  const renderTimes = useRef<number[]>([]);
  const mountTime = useRef(Date.now());

  useEffect(() => {
    const startTime = performance.now();

    return () => {
      const renderTime = performance.now() - startTime;
      renderTimes.current.push(renderTime);

      // Keep only last 100 renders
      if (renderTimes.current.length > 100) {
        renderTimes.current.shift();
      }

      // Calculate average
      const avg = renderTimes.current.reduce((a, b) => a + b, 0) / renderTimes.current.length;

      if (avg > threshold) {
        console.warn(
          `[Render Monitor] ${componentName} average render time: ${avg.toFixed(2)}ms (threshold: ${threshold}ms)`
        );
      }
    };
  });

  useEffect(() => {
    return () => {
      const lifetimeMs = Date.now() - mountTime.current;
      console.log(
        `[Component Lifecycle] ${componentName} unmounted after ${(lifetimeMs / 1000).toFixed(2)}s`
      );
    };
  }, [componentName]);
}
