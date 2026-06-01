'use client';

import { Canvas, useFrame, useLoader } from '@react-three/fiber';
import { TextureLoader } from 'three';
import React, { useRef } from 'react';
import * as THREE from 'three';

const RotatingLogo = ({ logoUrl }: { logoUrl: string }) => {
  const texture = useLoader(TextureLoader, logoUrl);
  const frontRef = useRef<THREE.Mesh>(null!);
  const backRef = useRef<THREE.Mesh>(null!);

  useFrame(() => {
    if (frontRef.current) frontRef.current.rotation.y += 0.01;
    if (backRef.current) backRef.current.rotation.y += 0.01;
  });

  return (
    <>
      <mesh ref={frontRef} scale={2}>
        <planeGeometry args={[2, 2]} />
        <meshBasicMaterial map={texture} transparent side={THREE.FrontSide} />
      </mesh>

      <mesh ref={backRef} scale={2} rotation={[0, Math.PI, 0]}>
        <planeGeometry args={[2, 2]} />
        <meshBasicMaterial map={texture} transparent side={THREE.FrontSide} />
      </mesh>
    </>
  );
};

export const LogoBackground = ({ logoUrl }: { logoUrl: string }) => (
  <div className="rotating-background absolute inset-0 z-0">
    <Canvas
      camera={{ position: [0, 0, 6] }}
      onCreated={({ gl }) => {
        gl.domElement.style.pointerEvents = 'none'; // ✅ This makes your buttons clickable again
      }}
    >
      <ambientLight intensity={1} />
      <RotatingLogo logoUrl={logoUrl} />
    </Canvas>
  </div>
);
