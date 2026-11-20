'use client';

import { memo } from 'react';
import dynamic from 'next/dynamic';
import { InterpBuffer } from '@/lib/InterpBuffer';

const OrbitControls = dynamic(
  () =>
    import('@react-three/drei').then((mod) => ({ default: mod.OrbitControls })),
  { ssr: false }
);
const Field = dynamic(() => import('./Field'), { ssr: false });
const EventPlotter = dynamic(() => import('./EventPlotter'), { ssr: false });
const PitchPlayersLayer = dynamic(() => import('./PitchPlayersLayer'), {
  ssr: false,
});

interface Scene3DProps {
  events: any[];
  timer: number;
  interpBufferRef?: React.MutableRefObject<InterpBuffer>;
}

const Scene3D = memo(({ events, timer, interpBufferRef }: Scene3DProps) => {
  return (
    <>
      <ambientLight intensity={0.5} />
      <directionalLight position={[10, 20, 10]} intensity={1} castShadow />
      <OrbitControls enableDamping dampingFactor={0.05} />
      <Field />
      {interpBufferRef ? (
        <PitchPlayersLayer interpBufferRef={interpBufferRef} />
      ) : (
        <EventPlotter events={events} timer={timer} />
      )}
    </>
  );
});

Scene3D.displayName = 'Scene3D';

export default Scene3D;
