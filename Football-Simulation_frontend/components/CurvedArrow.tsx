'use client';

import { useRef, useEffect, useState } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import { Event } from '@/types/Event';

type Props = {
  event: Event;
  moveBall?: (position: [number, number, number]) => void;
  endY?: number; // Optional: allow explicit override
};

const CurvedArrow = ({ event, moveBall, endY }: Props) => {
  const lineRef = useRef<THREE.Line>(null);
  const [points, setPoints] = useState<THREE.Vector3[]>([]);
  const progress = useRef(0);

  // Use pass_target for passes, shot_target for shots
  const target = event.shot_target ?? event.pass_target;
  // Use custom endY if provided, or event.shot_target_y, or default to 0
  const targetY =
    typeof endY === 'number'
      ? endY
      : typeof (event as any).shot_target_y === 'number'
        ? (event as any).shot_target_y
        : 0;

  const startY =
    typeof (event as any).startY === 'number' ? (event as any).startY : 0.0;

  const start = new THREE.Vector3(
    event.position[0] - 60,
    startY,
    event.position[1] - 40
  );
  const end = target
    ? new THREE.Vector3(target[0] - 60, targetY, target[1] - 40)
    : start;

  // Control point: halfway between start and end, higher on y axis for the arc
  const mid = new THREE.Vector3().addVectors(start, end).multiplyScalar(0.5);
  mid.y += 10; // Adjust this value for arc height

  // Quadratic Bezier curve
  const curve = new THREE.QuadraticBezierCurve3(start, mid, end);

  useFrame((_, delta) => {
    const totalLength = curve.getLength();
    const speed = totalLength / 0.8; // reach the end in 0.8 seconds
    progress.current += delta * speed;

    let t = Math.min(progress.current / totalLength, 1);

    // Ball position on curve
    const currentPoint = curve.getPoint(t);

    // Move the global ball
    if (moveBall) {
      moveBall([currentPoint.x, currentPoint.y, currentPoint.z]);
    }

    // Line points from start to current
    const curvePoints = [];
    for (let i = 0; i <= 20 * t; i++) {
      curvePoints.push(curve.getPoint(i / 20));
    }
    setPoints(curvePoints);
  });

  useEffect(() => {
    if (lineRef.current && points.length >= 2) {
      const geometry = new THREE.BufferGeometry().setFromPoints(points);
      lineRef.current.geometry = geometry;
    }
  }, [points]);

  return lineRef.current ? (
    <primitive object={lineRef.current} ref={lineRef}>
      <bufferGeometry />
      <lineBasicMaterial color="white" linewidth={2} />
    </primitive>
  ) : null;
};

export default CurvedArrow;
