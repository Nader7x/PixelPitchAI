'use client';

import { useRef, useEffect, useMemo } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import { Event } from '@/types/Event';

type Props = {
  event: Event;
  moveBall?: (position: [number, number, number]) => void;
  endY?: number; // Optional: allow explicit override
};

const CURVE_SEGMENTS = 20;

const CurvedArrow = ({ event, moveBall, endY }: Props) => {
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

  const { curve, totalLength, scratchPoint, positions, positionAttr, geometry, lineObject } =
    useMemo(() => {
      const start = new THREE.Vector3(
        event.position[0] - 60,
        startY,
        event.position[1] - 40
      );
      const end = target
        ? new THREE.Vector3(target[0] - 60, targetY, target[1] - 40)
        : start.clone();

      // Control point: halfway between start and end, higher on y axis for the arc
      const mid = new THREE.Vector3().addVectors(start, end).multiplyScalar(0.5);
      mid.y += 10;

      const bezier = new THREE.QuadraticBezierCurve3(start, mid, end);
      const posArray = new Float32Array((CURVE_SEGMENTS + 1) * 3);
      const attr = new THREE.BufferAttribute(posArray, 3);
      const geom = new THREE.BufferGeometry();
      geom.setAttribute('position', attr);
      geom.setDrawRange(0, 0);

      const mat = new THREE.LineBasicMaterial({ color: 'white', linewidth: 2 });
      const line = new THREE.Line(geom, mat);

      return {
        curve: bezier,
        totalLength: bezier.getLength(),
        scratchPoint: new THREE.Vector3(),
        positions: posArray,
        positionAttr: attr,
        geometry: geom,
        lineObject: line,
      };
    }, [event.position, startY, target, targetY]);

  useEffect(() => {
    progress.current = 0;
    return () => {
      geometry.dispose();
      (lineObject.material as THREE.Material).dispose();
    };
  }, [geometry, lineObject]);

  useFrame((_, delta) => {
    if (totalLength <= 0) return;

    const speed = totalLength / 0.8; // reach the end in 0.8 seconds
    progress.current += delta * speed;

    const t = Math.min(progress.current / totalLength, 1);

    // Ball position on curve (zero per-frame allocation)
    curve.getPoint(t, scratchPoint);

    if (moveBall) {
      moveBall([scratchPoint.x, scratchPoint.y, scratchPoint.z]);
    }

    // Update line vertices in-place
    const count = Math.floor(CURVE_SEGMENTS * t) + 1;
    for (let i = 0; i < count; i++) {
      curve.getPoint(i / CURVE_SEGMENTS, scratchPoint);
      const idx = i * 3;
      positions[idx] = scratchPoint.x;
      positions[idx + 1] = scratchPoint.y;
      positions[idx + 2] = scratchPoint.z;
    }
    positionAttr.needsUpdate = true;
    geometry.setDrawRange(0, count >= 2 ? count : 0);
  });

  return <primitive object={lineObject} />;
};

export default CurvedArrow;
