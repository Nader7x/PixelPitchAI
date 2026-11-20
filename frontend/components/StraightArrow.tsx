'use client';

import { useRef, useEffect, useMemo } from 'react';
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

  // Use pass_target for passes, shot_target for shots
  const target = event.pass_target ?? event.shot_target;

  const {
    start,
    end,
    direction,
    totalLength,
    scratchPoint,
    positions,
    positionAttr,
    geometry,
    lineObject,
  } = useMemo(() => {
    const s = new THREE.Vector3(
      event.position[0] - 60,
      actualStartY,
      event.position[1] - 40
    );
    const e = target
      ? new THREE.Vector3(target[0] - 60, actualEndY, target[1] - 40)
      : s.clone();
    const diff = new THREE.Vector3().subVectors(e, s);
    const len = diff.length();
    const dir = len > 0 ? diff.normalize() : new THREE.Vector3();

    const posArray = new Float32Array([s.x, s.y, s.z, s.x, s.y, s.z]);
    const attr = new THREE.BufferAttribute(posArray, 3);
    const geom = new THREE.BufferGeometry();
    geom.setAttribute('position', attr);

    const mat = new THREE.LineBasicMaterial({ color: 'white', linewidth: 2 });
    const line = new THREE.Line(geom, mat);

    return {
      start: s,
      end: e,
      direction: dir,
      totalLength: len,
      scratchPoint: new THREE.Vector3(),
      positions: posArray,
      positionAttr: attr,
      geometry: geom,
      lineObject: line,
    };
  }, [event.position, actualStartY, actualEndY, target]);

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

    if (progress.current < totalLength) {
      scratchPoint
        .copy(start)
        .addScaledVector(direction, progress.current);

      if (moveBall) {
        moveBall([scratchPoint.x, scratchPoint.y, scratchPoint.z]);
      }

      positions[3] = scratchPoint.x;
      positions[4] = scratchPoint.y;
      positions[5] = scratchPoint.z;
    } else {
      if (moveBall) moveBall([end.x, end.y, end.z]);
      positions[3] = end.x;
      positions[4] = end.y;
      positions[5] = end.z;
    }
    positionAttr.needsUpdate = true;
  });

  return <primitive object={lineObject} />;
};

export default StraightArrow;
