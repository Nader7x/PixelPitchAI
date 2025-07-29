'use client';

import { memo } from 'react';
import dynamic from 'next/dynamic';

// Dynamic imports for individual 3D components
const OrbitControls = dynamic(
  () =>
    import('@react-three/drei').then((mod) => ({ default: mod.OrbitControls })),
  { ssr: false }
);
const Field = dynamic(() => import('./Field'), { ssr: false });
const EventPlotter = dynamic(() => import('./EventPlotter'), { ssr: false });

interface Scene3DProps {
  events: any[];
  timer: number;
}

// Memoized 3D scene to prevent unnecessary re-renders
const Scene3D = memo(({ events, timer }: Scene3DProps) => {
  return (
    <>
      <ambientLight intensity={0.5} />
      <directionalLight position={[10, 20, 10]} intensity={1} castShadow />
      <OrbitControls enableDamping dampingFactor={0.05} />
      <Field />
      <EventPlotter events={events} timer={timer} />
    </>
  );
});

Scene3D.displayName = 'Scene3D';

export default Scene3D;
