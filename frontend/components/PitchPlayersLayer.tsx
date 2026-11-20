'use client';

import { useEffect, useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import { InterpBuffer } from '@/lib/InterpBuffer';

interface PitchPlayersLayerProps {
  interpBufferRef: React.MutableRefObject<InterpBuffer>;
}

const TEAM_SIZE = 11;

/**
 * 60 FPS 22-player + 3D ball renderer driven directly by `InterpBuffer` inside `useFrame()`.
 * Performs zero React `setState` calls per animation frame.
 */
export default function PitchPlayersLayer({
  interpBufferRef,
}: PitchPlayersLayerProps) {
  const homeMeshRef = useRef<THREE.InstancedMesh>(null);
  const awayMeshRef = useRef<THREE.InstancedMesh>(null);
  const ballRef = useRef<THREE.Mesh>(null);
  const possessionRingRef = useRef<THREE.Mesh>(null);

  // Reusable scratch objects to avoid per-frame allocations
  const dummy = useMemo(() => new THREE.Object3D(), []);
  const homeOutfieldColor = useMemo(() => new THREE.Color('#2563EB'), []);
  const homeGkColor = useMemo(() => new THREE.Color('#0EA5E9'), []);
  const awayOutfieldColor = useMemo(() => new THREE.Color('#DC2626'), []);
  const awayGkColor = useMemo(() => new THREE.Color('#F97316'), []);

  // Initialize instance colors once (slot 1 & slot 12 are GKs)
  useEffect(() => {
    if (homeMeshRef.current) {
      for (let i = 0; i < TEAM_SIZE; i++) {
        homeMeshRef.current.setColorAt(
          i,
          i === 0 ? homeGkColor : homeOutfieldColor
        );
      }
      if (homeMeshRef.current.instanceColor) {
        homeMeshRef.current.instanceColor.needsUpdate = true;
      }
    }
    if (awayMeshRef.current) {
      for (let i = 0; i < TEAM_SIZE; i++) {
        awayMeshRef.current.setColorAt(
          i,
          i === 0 ? awayGkColor : awayOutfieldColor
        );
      }
      if (awayMeshRef.current.instanceColor) {
        awayMeshRef.current.instanceColor.needsUpdate = true;
      }
    }
  }, [awayGkColor, awayOutfieldColor, homeGkColor, homeOutfieldColor]);

  useFrame(() => {
    const state = interpBufferRef.current.sample(performance.now());
    let carrierX = 0;
    let carrierZ = 0;
    let hasCarrier = false;

    if (homeMeshRef.current) {
      for (let i = 0; i < TEAM_SIZE; i++) {
        const p = state.players[i];
        if (!p || !p.active || p.isSentOff) {
          dummy.position.set(0, -10, 0);
          dummy.scale.set(0, 0, 0);
        } else {
          const wx = p.x - 60;
          const wz = p.y - 40;
          dummy.position.set(wx, 0.9, wz);
          dummy.rotation.set(0, -p.facingRad, 0);
          dummy.scale.set(1, 1, 1);
          if (p.hasBall) {
            carrierX = wx;
            carrierZ = wz;
            hasCarrier = true;
          }
        }
        dummy.updateMatrix();
        homeMeshRef.current.setMatrixAt(i, dummy.matrix);
      }
      homeMeshRef.current.instanceMatrix.needsUpdate = true;
    }

    if (awayMeshRef.current) {
      for (let i = 0; i < TEAM_SIZE; i++) {
        const p = state.players[TEAM_SIZE + i];
        if (!p || !p.active || p.isSentOff) {
          dummy.position.set(0, -10, 0);
          dummy.scale.set(0, 0, 0);
        } else {
          const wx = p.x - 60;
          const wz = p.y - 40;
          dummy.position.set(wx, 0.9, wz);
          dummy.rotation.set(0, -p.facingRad, 0);
          dummy.scale.set(1, 1, 1);
          if (p.hasBall) {
            carrierX = wx;
            carrierZ = wz;
            hasCarrier = true;
          }
        }
        dummy.updateMatrix();
        awayMeshRef.current.setMatrixAt(i, dummy.matrix);
      }
      awayMeshRef.current.instanceMatrix.needsUpdate = true;
    }

    if (ballRef.current) {
      ballRef.current.position.set(
        state.ballX - 60,
        Math.max(0.22, state.ballZ),
        state.ballY - 40
      );
    }

    if (possessionRingRef.current) {
      if (hasCarrier) {
        possessionRingRef.current.visible = true;
        possessionRingRef.current.position.set(carrierX, 0.08, carrierZ);
      } else {
        possessionRingRef.current.visible = false;
      }
    }
  });

  return (
    <group>
      {/* Home 11 players (InstancedMesh) */}
      <instancedMesh
        ref={homeMeshRef}
        args={[undefined, undefined, TEAM_SIZE]}
        castShadow
      >
        <capsuleGeometry args={[0.45, 0.9, 6, 12]} />
        <meshStandardMaterial roughness={0.45} metalness={0.1} />
      </instancedMesh>

      {/* Away 11 players (InstancedMesh) */}
      <instancedMesh
        ref={awayMeshRef}
        args={[undefined, undefined, TEAM_SIZE]}
        castShadow
      >
        <capsuleGeometry args={[0.45, 0.9, 6, 12]} />
        <meshStandardMaterial roughness={0.45} metalness={0.1} />
      </instancedMesh>

      {/* 3D Ball with parabolic arc height */}
      <mesh ref={ballRef} castShadow position={[0, 0.22, 0]}>
        <sphereGeometry args={[0.25, 16, 16]} />
        <meshStandardMaterial color="#ffffff" roughness={0.3} />
      </mesh>

      {/* Ball-carrier possession aura */}
      <mesh
        ref={possessionRingRef}
        rotation={[-Math.PI / 2, 0, 0]}
        visible={false}
      >
        <ringGeometry args={[0.65, 0.95, 24]} />
        <meshBasicMaterial color="#FACC15" transparent opacity={0.85} />
      </mesh>
    </group>
  );
}
