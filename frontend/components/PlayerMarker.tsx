'use client';

import { Text } from '@react-three/drei';

type Props = {
  position: [number, number, number];
  color?: string;
  player?: string;
};

const PlayerMarker = ({ position, color = 'blue', player }: Props) => (
  <group position={position}>
    {/* Cylinder base */}
    <mesh position={[0, 0.6, 0]}>
      <cylinderGeometry args={[0.2, 0.2, 1.6, 12]} />
      <meshStandardMaterial color="white" />
    </mesh>
    {/* Sphere on top of cylinder */}
    <mesh position={[0, 2.6, 0]}>
      <sphereGeometry args={[1.2, 16, 16]} />
      <meshStandardMaterial color={color} />
    </mesh>
    {/* Player name above the sphere */}
    {player && (
      <Text
        fontSize={2}
        color="white"
        anchorX="center"
        anchorY="bottom"
        position={[0, 4.2, 0]}
      >
        {player}
      </Text>
    )}
  </group>
);

export default PlayerMarker;
