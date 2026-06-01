'use client';

import { memo } from 'react';

// Simplified background component with reduced CSS complexity
const OptimizedBackground = memo(() => {
  return (
    <>
      {/* Simplified gradient background */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
            linear-gradient(135deg, 
              #0a0a0a 0%, 
              #0f1420 20%, 
              #141b2a 40%, 
              #1a2238 60%, 
              #0a0a0a 100%
            )
          `,
          zIndex: 1,
        }}
      />

      {/* Simple vignette effect */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `
            radial-gradient(ellipse 80% 60% at 50% 40%, 
              transparent 0%, 
              rgba(0, 0, 0, 0.4) 70%, 
              rgba(0, 0, 0, 0.8) 100%
            )
          `,
          pointerEvents: 'none',
          zIndex: 1,
        }}
      />
    </>
  );
});

OptimizedBackground.displayName = 'OptimizedBackground';

export default OptimizedBackground;
