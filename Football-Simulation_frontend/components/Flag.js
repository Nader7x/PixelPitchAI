'use client';
import React from 'react';
import { DoubleSide } from 'three';

export default function Flag({ position = [0, 0, 0] }) {
  return (
    <group position={position}>
      {/* Flag pole */}
      <mesh>
        <cylinderGeometry args={[0.1, 0.1, 5, 16]} />
        <meshStandardMaterial color="#cccccc" metalness={0.2} />
      </mesh>

      {/* Flag cloth */}
      <mesh position={[0, 2.5, 0.3]} rotation={[0, -Math.PI / 4, 0]}>
        <planeGeometry args={[1, 0.8]} />
        <meshStandardMaterial color="red" side={DoubleSide} />
      </mesh>
    </group>
  );
}
