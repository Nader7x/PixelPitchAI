'use client';
import { useLoader } from '@react-three/fiber';
import { TextureLoader } from 'three';
import Goal from './Goal';
import Flag from './Flag';
import { FIELD_CONFIG } from '@/lib/fieldConfig';

export default function Field() {
  const texture = useLoader(TextureLoader, '/assets/field.png');
  const corners = [
    [-FIELD_CONFIG.realWidth / 2, 0, -FIELD_CONFIG.realHeight / 2], // Top-left
    [FIELD_CONFIG.realWidth / 2, 0, -FIELD_CONFIG.realHeight / 2], // Top-right
    [-FIELD_CONFIG.realWidth / 2, 0, FIELD_CONFIG.realHeight / 2], // Bottom-left
    [FIELD_CONFIG.realWidth / 2, 0, FIELD_CONFIG.realHeight / 2], // Bottom-right
  ];
  return (
    <group position={[0, 0, 0]}>
      {/* Main field surface */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} receiveShadow>
        <planeGeometry args={[150, 84.375]} />
        <meshStandardMaterial map={texture} />
      </mesh>

      {/* Left Goal */}
      <Goal position={[-60, 1.22, 0]} rotation={[0, Math.PI / 2, 0]} />

      {/* Right Goal */}
      <Goal position={[60, 1.22, 0]} rotation={[0, -Math.PI / 2, 0]} />
      <>
        {/* Add corner flags */}
        {corners.map((pos, i) => (
          <Flag key={`corner-${i}`} position={pos} />
        ))}
      </>
    </group>
  );
}
