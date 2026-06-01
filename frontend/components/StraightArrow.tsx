'use client';

import { useRef, useEffect, useState } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import { Event } from '@/types/Event';

type Props = {
  event: Event;
  moveBall?: (position: [number, number, number]) => void;
  startY?: number; // Add startY prop
  endY?: number; // Add endY prop
};

const StraightArrow = ({ event, moveBall, startY, endY }: Props) => {
  const lineRef = useRef<THREE.Line>(null);
  const [points, setPoints] = useState<THREE.Vector3[]>([]);
  const progress = useRef(0);

  // Use startY/endY from props, or from event, or fallback to 0.07
  const actualStartY =
    typeof startY === 'number'
      ? startY
      : typeof (event as any).startY === 'number'
        ? (event as any).startY
        : 0.07;
  const actualEndY =
    typeof endY === 'number'
      ? endY
      : typeof (event as any).endY === 'number'
        ? (event as any).endY
        : 0.07;

  const start = new THREE.Vector3(
    event.position[0] - 60,
    actualStartY,
    event.position[1] - 40
  );
  // Use pass_target for passes, shot_target for shots
  const target = event.pass_target ?? event.shot_target;
  const end = target
    ? new THREE.Vector3(target[0] - 60, actualEndY, target[1] - 40)
    : start;

  const direction = new THREE.Vector3().subVectors(end, start).normalize();
  const totalLength = new THREE.Vector3().subVectors(end, start).length();

  useFrame((_, delta) => {
    const speed = totalLength / 0.8; // reach the end in 0.8 seconds
    progress.current += delta * speed;

    if (progress.current < totalLength) {
      const currentPoint = new THREE.Vector3().addVectors(
        start,
        direction.clone().multiplyScalar(progress.current)
      );

      // Update ball position
      if (moveBall) {
        moveBall([currentPoint.x, currentPoint.y, currentPoint.z]);
      }

      // Grow the line by adding the new point
      setPoints([start.clone(), currentPoint.clone()]);
    } else {
      // Snap ball to final position
      if (moveBall) moveBall([end.x, end.y, end.z]);
      setPoints([start.clone(), end.clone()]);
    }
  });

  useEffect(() => {
    if (lineRef.current && points.length >= 2) {
      const geometry = new THREE.BufferGeometry().setFromPoints(points);
      lineRef.current.geometry = geometry;
    }
  }, [points]);

  return (
    <primitive object={new THREE.Line()} ref={lineRef}>
      <bufferGeometry />
      <lineBasicMaterial color="white" linewidth={2} />
    </primitive>
  );
};

export default StraightArrow;
